import os
import sys
import json
import time
import re
import logging
import zipfile
import requests
from pathlib import Path
from urllib.parse import urlparse
from bs4 import BeautifulSoup
from tqdm import tqdm

# ── CONFIG ────────────────────────────────────────────────────
BASE_URL = "https://www.culture.go.kr/datametaverse"
BBS_URL  = f"{BASE_URL}/bbs/board.php"

CATEGORIES = {
    "modeling":     "전통문양3D도음",
    "buildings":    "건축물완성형",
    "building":     "건축물부품형",
    "digitalhuman": "디지털휴먼",
    "object":       "공간소품",
    # "materials":  "스마트머터리얼",  # 제외
}

# 스크립트 파일 위치 기준 — 어느 디렉토리에서 실행해도 항상 동일한 위치 사용
_SCRIPT_DIR = Path(__file__).parent

DOWNLOAD_DIR     = _SCRIPT_DIR / "downloaded_fbx"
PROGRESS_FILE    = _SCRIPT_DIR / "download_progress.json"
DRIVE_FOLDER_ID  = "1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9"
CREDENTIALS_FILE = str(_SCRIPT_DIR / "credentials.json")
TOKEN_FILE       = str(_SCRIPT_DIR / "token.json")

DELAY_BETWEEN_REQUESTS = 1.5
MAX_RETRIES            = 3

EXCLUDE_KEYWORD = "언리얼엔진"

# 다운로드 대상 아이콘
# y_view_icon2.png = 유니티 패키지 (.unitypackage)
# y_view_icon3.png = FBX (건축물: .fbx, 전통문양: _set.zip)
DOWNLOAD_ICONS = {"y_view_icon2.png", "y_view_icon3.png"}
# ────────────────────────────────────────────────────────────

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler(str(_SCRIPT_DIR / "downloader.log"), encoding="utf-8"),
    ],
)
log = logging.getLogger(__name__)


# ── 헬퍼 ──────────────────────────────────────────────────────
def to_absolute_url(href: str) -> str:
    return href if href.startswith("http") else f"{BASE_URL}{href}"


def filename_from_url(url: str) -> str:
    """URL에서 쿼리스트링을 제거한 순수 파일명 반환"""
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
            # [Fix 1] 사이트가 charset 없이 응답할 때 requests가 ISO-8859-1로 오추론하는 것 방지
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
        log.info(f"[{category_name}] 목록 {page}페이지 수집: {url}")

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
        log.info(f"  {page}페이지에서 {len(assets)}개 수집 (누적: {len(all_assets)}개)")

        if page >= get_total_pages(soup):
            break

        page += 1
        time.sleep(DELAY_BETWEEN_REQUESTS)

    return all_assets


# ── 상세 페이지 파싱 ──────────────────────────────────────────
def get_download_urls(session: requests.Session, asset: dict) -> list[str]:
    """유니티 패키지(icon2) + FBX(icon3) 다운로드 URL 반환
    - y_view_icon2.png: .unitypackage
    - y_view_icon3.png: .fbx 또는 _set.zip (전통문양)
    """
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
            # [Fix 2] timeout 튜플: 연결 15s, 읽기 무제한 (수 GB 파일 중간에 끊기지 않게)
            resp = session.get(url, stream=True, timeout=(15, None),
                               headers={"Referer": BASE_URL})
            resp.raise_for_status()
            total = int(resp.headers.get("Content-Length", 0))
            dest_path.parent.mkdir(parents=True, exist_ok=True)

            with open(dest_path, "wb") as f, tqdm(
                total=total, unit="B", unit_scale=True,
                desc=dest_path.name, leave=False
            ) as bar:
                for chunk in resp.iter_content(chunk_size=65536):
                    if chunk:
                        f.write(chunk)
                        bar.update(len(chunk))

            size = dest_path.stat().st_size
            if size == 0:
                dest_path.unlink()
                raise ValueError("다운로드된 파일 크기가 0")

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
            # 기존 파일에 skipped 키 없을 경우 보완
            for k, v in default.items():
                data.setdefault(k, v)
            return data
        except (json.JSONDecodeError, OSError) as e:
            log.warning(f"progress 파일 손상, 초기화: {e}")
    return default


def save_progress(progress: dict):
    # [Fix 4] 원자적 쓰기: 임시 파일에 먼저 쓰고 rename
    tmp = PROGRESS_FILE.with_suffix(".tmp")
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(progress, f, ensure_ascii=False, indent=2)
    tmp.replace(PROGRESS_FILE)


# ── Google Drive 업로드 ───────────────────────────────────────
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
    results = service.files().list(q=query, fields="files(id,name)").execute()
    items = results.get("files", [])
    if items:
        return items[0]["id"]

    metadata = {
        "name": name,
        "mimeType": "application/vnd.google-apps.folder",
        "parents": [parent_id],
    }
    folder = service.files().create(body=metadata, fields="id").execute()
    return folder["id"]


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
    """다운로드된 파일 처리:
    - zip이면 압축 해제 후 내부 파일을 Drive 업로드
    - 그 외엔 그대로 Drive 업로드
    - Drive 업로드 성공 시 로컬 파일 삭제 (로컬 디스크 절약)
    - Drive 없으면 로컬에만 보관
    """
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


# ── 메인 실행 ─────────────────────────────────────────────────
def main(test_mode=False, test_limit=3):
    log.info("=" * 60)
    log.info("문화재관광부 메타버스데이터랩 FBX 다운로더 시작")
    log.info(f"다운로드 폴더: {DOWNLOAD_DIR.resolve()}")
    log.info(f"Google Drive 폴더 ID: {DRIVE_FOLDER_ID}")
    if test_mode:
        log.info(f"[테스트 모드] buildings 카테고리 {test_limit}개만 처리")
    log.info("=" * 60)

    DOWNLOAD_DIR.mkdir(exist_ok=True)
    progress = load_progress()
    # [Fix 5] failed 항목은 재시도 대상 → already_done에서 제외
    already_done = set(progress["downloaded"])

    drive_service = None
    if os.path.exists(CREDENTIALS_FILE):
        try:
            drive_service = get_drive_service()
            log.info("Google Drive 인증 성공")
        except Exception as e:
            log.warning(f"Google Drive 인증 실패: {e}")
            log.warning("다운로드는 진행하지만 Drive 업로드는 건너뜀")
    else:
        log.warning("credentials.json 없음 → Drive 업로드 비활성화")

    session = make_session()
    log.info("HTTP 세션 준비 완료 (메인 페이지 쿠키 설정됨)")

    stats = {cat: {"success": 0, "fail": 0, "skip": 0} for cat in CATEGORIES}
    all_failed = []

    categories_to_process = ["buildings"] if test_mode else list(CATEGORIES.keys())

    # Drive 폴더 ID 캐시 (카테고리당 1회 API 호출)
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

        assets = collect_all_assets(session, bo_table)
        log.info(f"수집된 에셋: {len(assets)}개")

        if test_mode:
            assets = assets[:test_limit]
            log.info(f"[테스트] {len(assets)}개만 처리")

        cat_dir = DOWNLOAD_DIR / category_name
        cat_dir.mkdir(exist_ok=True)

        for asset in assets:
            key = f"{bo_table}/{asset['wr_id']}"
            if key in already_done:
                log.info(f"  [건너뜀] {asset['title']}")
                stats[bo_table]["skip"] += 1
                continue

            log.info(f"\n  [{category_name}] {asset['title']} (wr_id={asset['wr_id']})")
            time.sleep(DELAY_BETWEEN_REQUESTS)

            download_urls = get_download_urls(session, asset)

            if not download_urls:
                log.warning(f"  다운로드 링크 없음: {asset['title']}")
                if key not in progress["skipped"]:
                    progress["skipped"].append(key)
                stats[bo_table]["skip"] += 1
                save_progress(progress)
                time.sleep(DELAY_BETWEEN_REQUESTS)
                continue

            all_ok = True
            for file_url in download_urls:
                filename = filename_from_url(file_url)
                dest = cat_dir / filename

                if not dest.exists():
                    time.sleep(DELAY_BETWEEN_REQUESTS)
                    ok = download_file(session, file_url, dest)
                    if not ok:
                        log.error(f"  다운로드 최종 실패: {filename}")
                        all_failed.append({"key": key, "title": asset["title"],
                                           "reason": f"다운로드 실패: {file_url}"})
                        all_ok = False
                        continue

                ok = process_file(dest, drive_service, drive_folder_id)
                if not ok:
                    all_ok = False

            if all_ok:
                progress["downloaded"].append(key)
                already_done.add(key)
                stats[bo_table]["success"] += 1
            else:
                if key not in progress["failed"]:
                    progress["failed"].append(key)
                stats[bo_table]["fail"] += 1

            save_progress(progress)

    log.info("\n" + "=" * 60)
    log.info("다운로드 완료 보고")
    log.info("=" * 60)

    total_success = total_fail = total_skip = 0
    for bo_table, s in stats.items():
        if bo_table not in categories_to_process:
            continue
        log.info(f"  {CATEGORIES[bo_table]}: 성공 {s['success']} / 실패 {s['fail']} / 건너뜀 {s['skip']}")
        total_success += s["success"]
        total_fail    += s["fail"]
        total_skip    += s["skip"]

    log.info(f"\n  전체 성공: {total_success}개")
    log.info(f"  전체 실패: {total_fail}개")
    log.info(f"  전체 건너뜀: {total_skip}개")

    if all_failed:
        log.info("\n실패 에셋 목록:")
        for item in all_failed:
            log.info(f"  - [{item['key']}] {item['title']}: {item['reason']}")

    return total_success, total_fail


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="문화재관광부 FBX 다운로더")
    parser.add_argument("--test", action="store_true", help="buildings 카테고리 3개만 테스트")
    parser.add_argument("--test-limit", type=int, default=3, help="테스트 시 처리할 에셋 수")
    args = parser.parse_args()

    main(test_mode=args.test, test_limit=args.test_limit)
