import os
import sys
import json
import time
import re
import logging
import zipfile
import threading
import requests
from pathlib import Path
from urllib.parse import urlparse
from bs4 import BeautifulSoup
from tqdm import tqdm
from concurrent.futures import ThreadPoolExecutor, as_completed

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

DOWNLOAD_DIR     = _SCRIPT_DIR / "downloaded_fbx"
PROGRESS_FILE    = _SCRIPT_DIR / "download_progress.json"
DRIVE_FOLDER_ID  = "1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9"
CREDENTIALS_FILE = str(_SCRIPT_DIR / "credentials.json")
TOKEN_FILE       = str(_SCRIPT_DIR / "token.json")

WORKERS                = 5    # 동시 처리 에셋 수
DELAY_BETWEEN_REQUESTS = 0.5  # 스레드당 요청 간격(s)
MAX_RETRIES            = 3

EXCLUDE_KEYWORD = "언리얼엔진"

# y_view_icon2.png = 유니티 패키지, y_view_icon3.png = FBX
DOWNLOAD_ICONS = {"y_view_icon2.png", "y_view_icon3.png"}
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

# progress.json 동시 쓰기 방지
_progress_lock = threading.Lock()


# ── 헬퍼 ──────────────────────────────────────────────────────
def to_absolute_url(href: str) -> str:
    return href if href.startswith("http") else f"{BASE_URL}{href}"


def filename_from_url(url: str) -> str:
    return Path(urlparse(url).path).name


# ── HTTP 세션 ─────────────────────────────────────────────────
def make_session() -> requests.Session:
    session = requests.Session()
    session.headers.update({
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
    session.get(f"{BASE_URL}/", timeout=15)
    return session


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
                time.sleep(2 * attempt)
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

        soup = BeautifulSoup(resp.text, "html.parser")
        assets = get_asset_links_from_list(soup, bo_table)

        if not assets:
            log.info(f"  {page}페이지에 에셋 없음 — 수집 종료")
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
    """유니티 패키지(icon2) + FBX(icon3) URL 반환"""
    url = f"{BBS_URL}?bo_table={asset['bo_table']}&wr_id={asset['wr_id']}"
    resp = safe_get(session, url, referer=asset["list_url"])
    if not resp:
        return []

    soup = BeautifulSoup(resp.text, "html.parser")
    urls = []
    for btn in soup.select("li.downloadBtn a[download]"):
        href = btn.get("href", "")
        if not href:
            continue
        img = btn.find("img")
        icon_name = img.get("src", "").split("/")[-1] if img else ""
        if icon_name in DOWNLOAD_ICONS:
            urls.append(to_absolute_url(href))
    return urls


# ── 파일 다운로드 ─────────────────────────────────────────────
def download_file(session: requests.Session, url: str, dest_path: Path) -> bool:
    for attempt in range(1, MAX_RETRIES + 1):
        try:
            resp = session.get(url, stream=True, timeout=(15, None),
                               headers={"Referer": BASE_URL})
            resp.raise_for_status()
            total = int(resp.headers.get("Content-Length", 0))
            dest_path.parent.mkdir(parents=True, exist_ok=True)

            with open(dest_path, "wb") as f, tqdm(
                total=total, unit="B", unit_scale=True,
                desc=dest_path.name[:40], leave=False, dynamic_ncols=True,
            ) as bar:
                for chunk in resp.iter_content(chunk_size=65536):
                    if chunk:
                        f.write(chunk)
                        bar.update(len(chunk))

            size = dest_path.stat().st_size
            if size == 0:
                dest_path.unlink()
                raise ValueError("파일 크기 0")

            log.info(f"  다운로드 완료: {dest_path.name} ({size/1024/1024:.1f} MB)")
            return True

        except Exception as e:
            log.warning(f"  다운로드 실패 ({attempt}/{MAX_RETRIES}): {e}")
            if dest_path.exists():
                dest_path.unlink()
            if attempt < MAX_RETRIES:
                time.sleep(2 * attempt)

    return False


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
def get_drive_service():
    from google.oauth2.credentials import Credentials
    from google_auth_oauthlib.flow import InstalledAppFlow
    from google.auth.transport.requests import Request
    from googleapiclient.discovery import build

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
        with open(TOKEN_FILE, "w") as token:
            token.write(creds.to_json())
    return build("drive", "v3", credentials=creds)


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


def upload_to_drive(service, local_path: Path, parent_id: str) -> bool:
    from googleapiclient.http import MediaFileUpload

    query = f"name='{local_path.name}' and '{parent_id}' in parents and trashed=false"
    if service.files().list(q=query, fields="files(id)").execute().get("files"):
        log.info(f"  이미 업로드됨: {local_path.name}")
        return True
    try:
        media = MediaFileUpload(str(local_path), resumable=True)
        meta = {"name": local_path.name, "parents": [parent_id]}
        service.files().create(body=meta, media_body=media, fields="id").execute()
        log.info(f"  Drive 업로드 완료: {local_path.name}")
        return True
    except Exception as e:
        log.error(f"  Drive 업로드 실패: {local_path.name} — {e}")
        return False


def process_file(local_path: Path, drive_service, drive_folder_id: str) -> bool:
    """zip이면 압축 해제 후 Drive 업로드, 그 외 직접 업로드. 성공 시 로컬 삭제."""
    if local_path.suffix.lower() == ".zip":
        try:
            with zipfile.ZipFile(local_path) as zf:
                members = [m for m in zf.infolist() if not m.filename.endswith("/")]
                log.info(f"  zip 압축 해제: {local_path.name} ({len(members)}개 파일)")
                all_ok = True
                for member in members:
                    extracted = local_path.parent / Path(member.filename).name
                    with zf.open(member) as src, open(extracted, "wb") as dst:
                        dst.write(src.read())
                    if drive_service and drive_folder_id:
                        ok = upload_to_drive(drive_service, extracted, drive_folder_id)
                        extracted.unlink()
                        if not ok:
                            all_ok = False
            local_path.unlink()
            return all_ok
        except Exception as e:
            log.error(f"  zip 처리 실패: {local_path.name} — {e}")
            return False
    else:
        if drive_service and drive_folder_id:
            ok = upload_to_drive(drive_service, local_path, drive_folder_id)
            if ok:
                local_path.unlink()
            return ok
        return True


# ── 에셋 단위 처리 (스레드 워커) ─────────────────────────────
def process_asset(asset: dict, cat_dir: Path, drive_folder_id: str,
                  progress: dict, already_done: set) -> str:
    """반환값: 'success' | 'fail' | 'skip'"""
    key = f"{asset['bo_table']}/{asset['wr_id']}"
    category_name = CATEGORIES[asset["bo_table"]]

    session = make_session()  # 스레드마다 독립 세션

    # 스레드마다 독립 Drive service → 다운로드 즉시 업로드해 로컬 임시 파일 최소화
    drive_service = None
    if os.path.exists(CREDENTIALS_FILE):
        try:
            drive_service = get_drive_service()
        except Exception:
            pass

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
    failed_reasons = []

    for file_url in download_urls:
        filename = filename_from_url(file_url)
        dest = cat_dir / filename

        if not dest.exists():
            time.sleep(DELAY_BETWEEN_REQUESTS)
            ok = download_file(session, file_url, dest)
            if not ok:
                failed_reasons.append(f"다운로드 실패: {filename}")
                all_ok = False
                continue

        ok = process_file(dest, drive_service, drive_folder_id)
        if not ok:
            failed_reasons.append(f"업로드 실패: {filename}")
            all_ok = False

    with _progress_lock:
        if all_ok:
            progress["downloaded"].append(key)
            already_done.add(key)
        else:
            if key not in progress["failed"]:
                progress["failed"].append(key)
            for r in failed_reasons:
                log.error(f"  [{category_name}] {asset['title']} — {r}")
        save_progress(progress)

    return "success" if all_ok else "fail"


# ── 메인 실행 ─────────────────────────────────────────────────
def main(test_mode=False, test_limit=3):
    log.info("=" * 60)
    log.info("문화재관광부 메타버스데이터랩 FBX 다운로더 시작")
    log.info(f"다운로드 폴더: {DOWNLOAD_DIR.resolve()}")
    log.info(f"병렬 워커: {WORKERS}개 / 요청 딜레이: {DELAY_BETWEEN_REQUESTS}s")
    log.info(f"Google Drive 폴더 ID: {DRIVE_FOLDER_ID}")
    if test_mode:
        log.info(f"[테스트 모드] buildings 카테고리 {test_limit}개만 처리")
    log.info("=" * 60)

    DOWNLOAD_DIR.mkdir(exist_ok=True)
    progress = load_progress()
    already_done = set(progress["downloaded"])

    drive_service = None
    if os.path.exists(CREDENTIALS_FILE):
        try:
            drive_service = get_drive_service()
            log.info("Google Drive 인증 성공")
        except Exception as e:
            log.warning(f"Google Drive 인증 실패: {e} — 로컬 저장만 진행")
    else:
        log.warning("credentials.json 없음 → Drive 업로드 비활성화")

    # 목록 수집용 단일 세션 (페이지네이션은 순차 처리)
    list_session = make_session()
    log.info("HTTP 세션 준비 완료")

    stats = {cat: {"success": 0, "fail": 0, "skip": 0} for cat in CATEGORIES}
    categories_to_process = ["buildings"] if test_mode else list(CATEGORIES.keys())
    drive_folder_cache: dict[str, str] = {}

    for bo_table in categories_to_process:
        category_name = CATEGORIES[bo_table]
        log.info(f"\n{'='*50}")
        log.info(f"카테고리: {category_name} (bo_table={bo_table})")

        if drive_service and category_name not in drive_folder_cache:
            try:
                drive_folder_cache[category_name] = get_or_create_folder(
                    drive_service, category_name, DRIVE_FOLDER_ID
                )
                log.info(f"Drive 폴더 준비: {category_name} (id={drive_folder_cache[category_name]})")
            except Exception as e:
                log.warning(f"Drive 폴더 생성 실패: {e}")

        drive_folder_id = drive_folder_cache.get(category_name)

        assets = collect_all_assets(list_session, bo_table)
        log.info(f"수집된 에셋: {len(assets)}개")

        if test_mode:
            assets = assets[:test_limit]
            log.info(f"[테스트] {len(assets)}개만 처리")

        cat_dir = DOWNLOAD_DIR / category_name
        cat_dir.mkdir(exist_ok=True)

        # 이미 완료된 에셋 건너뜀
        pending = []
        for asset in assets:
            key = f"{bo_table}/{asset['wr_id']}"
            if key in already_done:
                stats[bo_table]["skip"] += 1
            else:
                pending.append(asset)

        log.info(f"처리 대상: {len(pending)}개 (건너뜀: {stats[bo_table]['skip']}개)")

        with ThreadPoolExecutor(max_workers=WORKERS) as executor:
            futures = {
                executor.submit(
                    process_asset, asset, cat_dir, drive_folder_id,
                    progress, already_done
                ): asset
                for asset in pending
            }
            for future in as_completed(futures):
                asset = futures[future]
                try:
                    result = future.result()
                    stats[bo_table][result if result in stats[bo_table] else "fail"] += 1
                    log.info(
                        f"  [{category_name}] {asset['title']} → {result} "
                        f"(성공 {stats[bo_table]['success']} / 실패 {stats[bo_table]['fail']})"
                    )
                except Exception as e:
                    log.error(f"  처리 예외: {asset['title']} — {e}")
                    stats[bo_table]["fail"] += 1

    log.info("\n" + "=" * 60)
    log.info("다운로드 완료 보고")
    log.info("=" * 60)
    total_success = total_fail = total_skip = 0
    for bo_table in categories_to_process:
        s = stats[bo_table]
        log.info(f"  {CATEGORIES[bo_table]}: 성공 {s['success']} / 실패 {s['fail']} / 건너뜀 {s['skip']}")
        total_success += s["success"]
        total_fail    += s["fail"]
        total_skip    += s["skip"]
    log.info(f"\n  전체 성공: {total_success} / 실패: {total_fail} / 건너뜀: {total_skip}")
    return total_success, total_fail


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="문화재관광부 FBX 다운로더")
    parser.add_argument("--test", action="store_true", help="buildings 카테고리 3개만 테스트")
    parser.add_argument("--test-limit", type=int, default=3)
    args = parser.parse_args()
    main(test_mode=args.test, test_limit=args.test_limit)
