import os
import sys
import json
import time
import re
import logging
import threading
import requests
from pathlib import Path
from urllib.parse import urlparse
from bs4 import BeautifulSoup
from tqdm import tqdm
from concurrent.futures import ThreadPoolExecutor, as_completed
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

# ── CONFIG ────────────────────────────────────────────────────
BASE_URL = "https://www.culture.go.kr/datametaverse"
BBS_URL  = f"{BASE_URL}/bbs/board.php"

CATEGORIES = {
    # "modeling":   "전통문양3D도음",  # 너무 많아서 제외
    "buildings":    "건축물완성형",
    "building":     "건축물부품형",
    "digitalhuman": "디지털휴먼",
    "object":       "공간소품",
    # "materials":  "스마트머터리얼",  # 제외
}

_SCRIPT_DIR = Path(__file__).parent

PROGRESS_FILE    = _SCRIPT_DIR / "download_progress.json"
DRIVE_FOLDER_ID  = "1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9"
CREDENTIALS_FILE = str(_SCRIPT_DIR / "credentials.json")
TOKEN_FILE       = str(_SCRIPT_DIR / "token.json")

WORKERS                = 5
DELAY_BETWEEN_REQUESTS = 0.3   # 스레드당 요청 간격(s)
MAX_RETRIES            = 3
STREAM_CHUNK           = 32 * 1024 * 1024  # Drive 청크 32MB (PUT 요청 수 최소화)

EXCLUDE_KEYWORD = "언리얼엔진"
DOWNLOAD_ICONS  = {"y_view_icon2.png", "y_view_icon3.png"}

# lxml이 없으면 html.parser fallback
try:
    import lxml  # noqa: F401
    HTML_PARSER = "lxml"
except ImportError:
    HTML_PARSER = "html.parser"
# ─────────────────────────────────────────────────────────────

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler(str(_SCRIPT_DIR / "downloader.log"), encoding="utf-8"),
    ],
)
log = logging.getLogger(__name__)

_progress_lock = threading.Lock()

# ── Thread-local 캐시: 세션·Drive 재사용 ──────────────────────
_tl = threading.local()


def _tl_session() -> requests.Session:
    """다운로드용 세션 — 스레드당 1회 생성 후 재사용."""
    if not hasattr(_tl, "dl_session"):
        s = requests.Session()
        adapter = HTTPAdapter(
            pool_connections=4,
            pool_maxsize=10,
            max_retries=Retry(total=0),  # 재시도는 safe_get에서 직접 처리
        )
        s.mount("https://", adapter)
        s.mount("http://", adapter)
        s.headers.update({
            "User-Agent": (
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                "AppleWebKit/537.36 (KHTML, like Gecko) "
                "Chrome/120.0.0.0 Safari/537.36"
            ),
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            "Accept-Language": "ko-KR,ko;q=0.9",
            "Connection": "keep-alive",
            "Upgrade-Insecure-Requests": "1",
        })
        s.get(f"{BASE_URL}/", timeout=15)   # 쿠키 획득
        _tl.dl_session = s
    return _tl.dl_session


def _tl_upload_session() -> requests.Session:
    """Drive 업로드 전용 세션 — TCP 연결 유지로 청크 PUT 재사용."""
    if not hasattr(_tl, "up_session"):
        s = requests.Session()
        s.mount("https://", HTTPAdapter(pool_connections=2, pool_maxsize=2,
                                        max_retries=Retry(total=0)))
        _tl.up_session = s
    return _tl.up_session


def _tl_token() -> str:
    """Drive 액세스 토큰 — 만료 시만 갱신."""
    creds = getattr(_tl, "creds", None)
    if creds is None or not creds.valid:
        _tl.creds = get_drive_credentials()
    return _tl.creds.token


# ── 헬퍼 ──────────────────────────────────────────────────────
def to_absolute_url(href: str) -> str:
    return href if href.startswith("http") else f"{BASE_URL}{href}"


def filename_from_url(url: str) -> str:
    return Path(urlparse(url).path).name


def safe_get(session: requests.Session, url: str, referer: str = None) -> requests.Response | None:
    headers = {"Referer": referer} if referer else {}
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            resp = session.get(url, headers=headers, timeout=20)
            resp.raise_for_status()
            resp.encoding = "utf-8"
            return resp
        except Exception as e:
            log.warning(f"  요청 실패 ({attempt}/{MAX_RETRIES}): {url} — {e}")
            if attempt < MAX_RETRIES:
                time.sleep(2 ** attempt)   # exponential backoff
    return None


# ── 목록 페이지 파싱 ──────────────────────────────────────────
def get_total_pages(soup: BeautifulSoup) -> int:
    pg_end = soup.select_one("a.pg_end")
    if pg_end:
        m = re.search(r"page=(\d+)", pg_end.get("href", ""))
        if m:
            return int(m.group(1))
    pages = [
        int(m.group(1))
        for a in soup.select("a.pg_page")
        if (m := re.search(r"page=(\d+)", a.get("href", "")))
    ]
    return max(pages) if pages else 1


def get_asset_links_from_list(soup: BeautifulSoup, bo_table: str) -> list[dict]:
    assets = []
    for item in soup.select("li.y_sub_list1_img.itemBox"):
        a_tag = item.select_one("a[href]")
        title_tag = item.select_one("h5.downloadTitle span")
        if not a_tag:
            continue
        title = title_tag.get_text(strip=True) if title_tag else ""
        href = a_tag.get("href", "")
        m = re.search(r"wr_id=(\d+)", href)
        wr_id = m.group(1) if m else None

        if EXCLUDE_KEYWORD in title:
            log.info(f"  [건너뜀] {title} (언리얼엔진 전용)")
            continue

        if wr_id:
            assets.append({
                "bo_table": bo_table,
                "wr_id": wr_id,
                "title": title,
                "list_url": to_absolute_url(href),
            })
    return assets


def collect_all_assets(session: requests.Session, bo_table: str) -> list[dict]:
    category_name = CATEGORIES[bo_table]
    all_assets = []
    page = 1

    while True:
        url = f"{BBS_URL}?bo_table={bo_table}&page={page}"
        log.info(f"[{category_name}] 목록 {page}페이지 수집")

        resp = safe_get(session, url, referer=f"{BASE_URL}/")
        if not resp:
            log.error(f"  페이지 {page} 로드 실패, 수집 중단")
            break

        soup = BeautifulSoup(resp.text, HTML_PARSER)
        assets = get_asset_links_from_list(soup, bo_table)

        if not assets:
            break

        all_assets.extend(assets)
        log.info(f"  {page}페이지 {len(assets)}개 수집 (누적: {len(all_assets)}개)")

        if page >= get_total_pages(soup):
            break

        page += 1
        time.sleep(DELAY_BETWEEN_REQUESTS)

    return all_assets


# ── 상세 페이지 파싱 ──────────────────────────────────────────
def get_download_urls(session: requests.Session, asset: dict) -> list[str]:
    url = f"{BBS_URL}?bo_table={asset['bo_table']}&wr_id={asset['wr_id']}"
    resp = safe_get(session, url, referer=asset["list_url"])
    if not resp:
        return []

    soup = BeautifulSoup(resp.text, HTML_PARSER)
    urls = []
    for btn in soup.select("li.downloadBtn a[download]"):
        href = btn.get("href", "")
        if not href:
            continue
        img = btn.find("img")
        icon_name = img.get("src", "").split("/")[-1] if img else ""
        if icon_name in DOWNLOAD_ICONS:
            urls.append(to_absolute_url(href))

    # FBX 우선: FBX가 있으면 unitypackage 제외
    fbx_urls = [u for u in urls if filename_from_url(u).lower().endswith(".fbx")]
    if fbx_urls:
        log.info(f"  FBX 우선 선택 ({len(fbx_urls)}개) — unitypackage 제외")
        return fbx_urls
    return urls


# ── 진행 상황 저장/로드 ───────────────────────────────────────
def load_progress() -> dict:
    default = {"downloaded": [], "failed": [], "skipped": []}
    if PROGRESS_FILE.exists():
        try:
            with open(PROGRESS_FILE, encoding="utf-8") as f:
                data = json.load(f)
            for k, v in default.items():
                data.setdefault(k, v)
            return data
        except (json.JSONDecodeError, OSError) as e:
            log.warning(f"progress 파일 손상, 초기화: {e}")
    return default


def save_progress(progress: dict):
    tmp = PROGRESS_FILE.with_suffix(".tmp")
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(progress, f, ensure_ascii=False, indent=2)
    tmp.replace(PROGRESS_FILE)


# ── Google Drive ──────────────────────────────────────────────
def get_drive_credentials():
    from google.oauth2.credentials import Credentials
    from google_auth_oauthlib.flow import InstalledAppFlow
    from google.auth.transport.requests import Request

    SCOPES = ["https://www.googleapis.com/auth/drive"]
    creds = None
    if os.path.exists(TOKEN_FILE):
        creds = Credentials.from_authorized_user_file(TOKEN_FILE, SCOPES)
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            flow = InstalledAppFlow.from_client_secrets_file(CREDENTIALS_FILE, SCOPES)
            creds = flow.run_local_server(port=0)
        with open(TOKEN_FILE, "w") as f:
            f.write(creds.to_json())
    return creds


def get_drive_service():
    from googleapiclient.discovery import build
    return build("drive", "v3", credentials=get_drive_credentials())


def get_or_create_folder(service, name: str, parent_id: str) -> str:
    query = (
        f"name='{name}' and mimeType='application/vnd.google-apps.folder' "
        f"and '{parent_id}' in parents and trashed=false"
    )
    items = service.files().list(q=query, fields="files(id)").execute().get("files", [])
    if items:
        return items[0]["id"]
    meta = {"name": name, "mimeType": "application/vnd.google-apps.folder", "parents": [parent_id]}
    return service.files().create(body=meta, fields="id").execute()["id"]


def list_drive_folder(service, folder_id: str) -> set[str]:
    """Drive 폴더 내 파일명 전체를 한 번에 조회 — 에셋마다 API 호출하는 대신 set으로 O(1) 조회."""
    names: set[str] = set()
    params: dict = {
        "q": f"'{folder_id}' in parents and trashed=false",
        "fields": "nextPageToken, files(name)",
        "pageSize": 1000,
    }
    while True:
        result = service.files().list(**params).execute()
        names.update(f["name"] for f in result.get("files", []))
        next_token = result.get("nextPageToken")
        if not next_token:
            break
        params["pageToken"] = next_token
    return names


# ── Drive 스트리밍 업로드 (로컬 저장 없음) ────────────────────
def stream_to_drive(download_url: str, filename: str, parent_id: str,
                    existing_files: set[str]) -> bool:
    """다운로드 스트림을 로컬 저장 없이 Drive resumable upload로 직접 전송.

    Content-Range 규칙:
      - 중간 청크: bytes start-end/{total} or bytes start-end/*  (total 미확인 시)
      - 마지막 청크: bytes start-end/{실제 총 바이트} 로 항상 확정값 사용
      -> total_size=0 일 때 PUT에 "*" 를 쓰면 Drive 400 오류 발생하므로
         마지막 청크에서는 반드시 uploaded+len(data) 를 total로 지정.
    """
    if filename in existing_files:
        log.info(f"  이미 Drive에 있음: {filename}")
        return True

    dl_session = _tl_session()
    up_session = _tl_upload_session()

    for attempt in range(1, MAX_RETRIES + 1):
        try:
            token = _tl_token()

            # 파일 크기: HEAD 시도 → 실패하거나 0이면 GET 헤더에서 재확인
            total_size = 0
            try:
                head = dl_session.head(download_url, timeout=15,
                                       headers={"Referer": BASE_URL})
                total_size = int(head.headers.get("Content-Length", 0))
            except Exception:
                pass

            # Drive resumable upload 초기화
            # X-Upload-Content-Length 미전송: HEAD/GET Content-Length가 실제 파일 크기와
            # 다를 수 있어 사전 등록 시 Drive가 최종 청크를 거부하는 문제 방지
            init_headers = {
                "Authorization": f"Bearer {token}",
                "Content-Type": "application/json; charset=UTF-8",
                "X-Upload-Content-Type": "application/octet-stream",
            }

            init = up_session.post(
                "https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable",
                headers=init_headers,
                json={"name": filename, "parents": [parent_id]},
                timeout=30,
            )
            init.raise_for_status()
            upload_url = init.headers["Location"]

            # 다운로드 스트림 시작; GET 헤더에서도 size 재확인
            # 읽기 타임아웃 300s: None이면 슬립 후 네트워크 끊김 시 수 시간 hang 발생
            dl = dl_session.get(download_url, stream=True, timeout=(15, 300),
                                headers={"Referer": BASE_URL})
            dl.raise_for_status()
            if not total_size:
                total_size = int(dl.headers.get("Content-Length", 0))

            uploaded = 0
            buf = bytearray()

            with tqdm(total=total_size or None, unit="B", unit_scale=True,
                      desc=filename[:40], leave=False, dynamic_ncols=True) as bar:

                def _put_chunk(data: bytes, is_last: bool):
                    nonlocal uploaded
                    start = uploaded
                    end   = start + len(data) - 1
                    # 중간 청크: HEAD/GET Content-Length가 실제 크기와 다를 수 있으므로
                    # 항상 "*" 사용 — Drive가 중간에 잘못된 total을 등록하지 않도록
                    # 마지막 청크: 실제 누적 바이트로 total 확정 (Drive 필수)
                    if is_last:
                        total_str = str(start + len(data))
                    else:
                        total_str = "*"

                    resp = up_session.put(
                        upload_url,
                        headers={
                            "Authorization": f"Bearer {token}",
                            "Content-Range": f"bytes {start}-{end}/{total_str}",
                            "Content-Length": str(len(data)),
                        },
                        data=bytes(data),
                        timeout=600,
                    )
                    expected = (200, 201) if is_last else (200, 201, 308)
                    if resp.status_code not in expected:
                        raise Exception(f"Drive PUT {resp.status_code}: {resp.text[:200]}")
                    uploaded += len(data)

                for chunk in dl.iter_content(STREAM_CHUNK):
                    if not chunk:
                        continue
                    buf += chunk
                    bar.update(len(chunk))

                    while len(buf) >= STREAM_CHUNK:
                        _put_chunk(bytes(buf[:STREAM_CHUNK]), is_last=False)
                        del buf[:STREAM_CHUNK]

                if buf:
                    _put_chunk(bytes(buf), is_last=True)

            log.info(f"  Drive 업로드 완료: {filename} ({uploaded/1024**2:.1f} MB)")
            existing_files.add(filename)
            return True

        except Exception as e:
            log.warning(f"  업로드 실패 ({attempt}/{MAX_RETRIES}): {filename} — {e}")
            if attempt < MAX_RETRIES:
                time.sleep(2 ** attempt)

    return False


# ── 에셋 단위 처리 (스레드 워커) ─────────────────────────────
def process_asset(asset: dict, drive_folder_id: str,
                  existing_files: set[str], progress: dict,
                  already_done: set) -> str:
    """반환값: 'success' | 'fail' | 'skip'"""
    key = f"{asset['bo_table']}/{asset['wr_id']}"
    category_name = CATEGORIES[asset["bo_table"]]

    session = _tl_session()   # 스레드 로컬 세션 재사용
    time.sleep(DELAY_BETWEEN_REQUESTS)

    download_urls = get_download_urls(session, asset)
    if not download_urls:
        log.warning(f"  다운로드 링크 없음: {asset['title']}")
        with _progress_lock:
            if key not in progress["skipped"]:
                progress["skipped"].append(key)
            save_progress(progress)
        return "skip"

    all_ok = True
    for file_url in download_urls:
        filename = filename_from_url(file_url)
        ok = stream_to_drive(file_url, filename, drive_folder_id, existing_files)
        if not ok:
            log.error(f"  업로드 실패: {filename}")
            all_ok = False

    with _progress_lock:
        if all_ok:
            progress["downloaded"].append(key)
            already_done.add(key)
        else:
            if key not in progress["failed"]:
                progress["failed"].append(key)
        save_progress(progress)

    return "success" if all_ok else "fail"


# ── 메인 실행 ─────────────────────────────────────────────────
def main(test_mode=False, test_limit=3):
    log.info("=" * 60)
    log.info("문화재관광부 메타버스데이터랩 FBX 다운로더")
    log.info(f"파서: {HTML_PARSER} | 워커: {WORKERS} | 청크: {STREAM_CHUNK//1024//1024}MB")
    log.info(f"Google Drive 폴더: {DRIVE_FOLDER_ID}")
    if test_mode:
        log.info(f"[테스트] buildings {test_limit}개")
    log.info("=" * 60)

    progress = load_progress()
    already_done = set(progress["downloaded"])

    # 폴더 생성용 Drive service (메인 스레드 1회)
    drive_service = None
    if os.path.exists(CREDENTIALS_FILE):
        try:
            drive_service = get_drive_service()
            log.info("Google Drive 인증 성공")
        except Exception as e:
            log.error(f"Google Drive 인증 실패: {e}")
            sys.exit(1)
    else:
        log.error("credentials.json 없음 — Drive 업로드 불가")
        sys.exit(1)

    # 목록 수집용 세션 (메인 스레드)
    list_session = requests.Session()
    list_session.headers.update({
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Accept-Language": "ko-KR,ko;q=0.9",
    })
    list_session.get(f"{BASE_URL}/", timeout=15)

    stats = {cat: {"success": 0, "fail": 0, "skip": 0} for cat in CATEGORIES}
    categories_to_process = ["buildings"] if test_mode else list(CATEGORIES.keys())

    for bo_table in categories_to_process:
        category_name = CATEGORIES[bo_table]
        log.info(f"\n{'='*50}")
        log.info(f"카테고리: {category_name}")

        # Drive 폴더 준비 + 기존 파일 목록 한 번에 조회
        try:
            folder_id = get_or_create_folder(drive_service, category_name, DRIVE_FOLDER_ID)
            existing_files: set[str] = list_drive_folder(drive_service, folder_id)
            log.info(f"Drive 폴더 준비: {category_name} (기존 파일 {len(existing_files)}개)")
        except Exception as e:
            log.error(f"Drive 폴더 준비 실패: {e}")
            continue

        assets = collect_all_assets(list_session, bo_table)
        log.info(f"수집된 에셋: {len(assets)}개")

        if test_mode:
            assets = assets[:test_limit]

        pending = [
            a for a in assets
            if f"{bo_table}/{a['wr_id']}" not in already_done
        ]
        stats[bo_table]["skip"] = len(assets) - len(pending)
        log.info(f"처리 대상: {len(pending)}개 (건너뜀: {stats[bo_table]['skip']}개)")

        with ThreadPoolExecutor(max_workers=WORKERS) as executor:
            futures = {
                executor.submit(
                    process_asset, asset, folder_id,
                    existing_files, progress, already_done
                ): asset
                for asset in pending
            }
            for future in as_completed(futures):
                asset = futures[future]
                try:
                    result = future.result()
                    stats[bo_table][result if result in stats[bo_table] else "fail"] += 1
                    s = stats[bo_table]
                    log.info(
                        f"  [{category_name}] {asset['title']} → {result} "
                        f"(성공 {s['success']} / 실패 {s['fail']})"
                    )
                except Exception as e:
                    log.error(f"  예외: {asset['title']} — {e}")
                    stats[bo_table]["fail"] += 1

    log.info("\n" + "=" * 60)
    log.info("완료")
    log.info("=" * 60)
    total_s = total_f = total_k = 0
    for bo_table in categories_to_process:
        s = stats[bo_table]
        log.info(f"  {CATEGORIES[bo_table]}: 성공 {s['success']} / 실패 {s['fail']} / 건너뜀 {s['skip']}")
        total_s += s["success"]; total_f += s["fail"]; total_k += s["skip"]
    log.info(f"\n  합계: 성공 {total_s} / 실패 {total_f} / 건너뜀 {total_k}")
    return total_s, total_f


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="문화재관광부 FBX 다운로더")
    parser.add_argument("--test", action="store_true", help="buildings 3개 테스트")
    parser.add_argument("--test-limit", type=int, default=3)
    args = parser.parse_args()
    main(test_mode=args.test, test_limit=args.test_limit)
