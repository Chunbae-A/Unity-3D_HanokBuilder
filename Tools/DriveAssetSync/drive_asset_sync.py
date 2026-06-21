import os
import sys
import json
import zipfile
import logging
import io
import argparse
from pathlib import Path
from tqdm import tqdm

# ── CONFIG ──────────────────────────────────────────────────────────────────
_SCRIPT_DIR = Path(__file__).parent

# CultureAssetDownloader의 credentials/token을 공유 사용
CREDENTIALS_FILE = str(_SCRIPT_DIR.parent / "CultureAssetDownloader" / "credentials.json")
TOKEN_FILE       = str(_SCRIPT_DIR.parent / "CultureAssetDownloader" / "token.json")

DRIVE_ROOT_ID = "1J92prWdMR6HYr7WAaeIN-gsvgldHGNh9"

PROJECT_ROOT      = _SCRIPT_DIR.parent.parent
HANOK_ASSETS_ROOT = PROJECT_ROOT / "Assets" / "HanokBuilder" / "Resources" / "HanokAssets"

PROGRESS_FILE = _SCRIPT_DIR / "sync_progress.json"

# Drive 카테고리 폴더명 → 로컬 HanokAssets 하위 폴더명
CATEGORY_MAP = {
    "건축물완성형": "건축물완성형",
    "건축물부품형": "건축물부품형",
    "디지털휴먼":   "디지털휴먼",
    "공간소품":    "공간소품",
}

FOLDER_MIME = "application/vnd.google-apps.folder"
CHUNK_SIZE  = 32 * 1024 * 1024  # 32MB
# ────────────────────────────────────────────────────────────────────────────

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler(str(_SCRIPT_DIR / "sync.log"), encoding="utf-8"),
    ],
)
log = logging.getLogger(__name__)


# ── 인증 ──────────────────────────────────────────────────────────────────
def get_credentials():
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
            if not os.path.exists(CREDENTIALS_FILE):
                log.error(f"credentials.json 없음: {CREDENTIALS_FILE}")
                log.error("Tools/CultureAssetDownloader/credentials.json 가 있어야 합니다.")
                sys.exit(1)
            flow = InstalledAppFlow.from_client_secrets_file(CREDENTIALS_FILE, SCOPES)
            creds = flow.run_local_server(port=0)
        with open(TOKEN_FILE, "w") as f:
            f.write(creds.to_json())

    return creds


def get_drive_service():
    from googleapiclient.discovery import build
    return build("drive", "v3", credentials=get_credentials())


# ── 진행 상황 ──────────────────────────────────────────────────────────────
def load_progress() -> dict:
    if PROGRESS_FILE.exists():
        try:
            with open(PROGRESS_FILE, encoding="utf-8") as f:
                data = json.load(f)
            return {
                "downloaded": set(data.get("downloaded", [])),
                "failed":     set(data.get("failed", [])),
            }
        except Exception as e:
            log.warning(f"Progress 파일 손상, 초기화: {e}")
    return {"downloaded": set(), "failed": set()}


def save_progress(progress: dict):
    tmp = PROGRESS_FILE.with_suffix(".tmp")
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(
            {"downloaded": list(progress["downloaded"]),
             "failed":     list(progress["failed"])},
            f, ensure_ascii=False, indent=2,
        )
    tmp.replace(PROGRESS_FILE)


# ── 다운로드 ───────────────────────────────────────────────────────────────
def download_file(service, file_id: str, dest_path: Path, file_size: int = 0):
    from googleapiclient.http import MediaIoBaseDownload

    dest_path.parent.mkdir(parents=True, exist_ok=True)
    request = service.files().get_media(fileId=file_id)

    with open(dest_path, "wb") as fh:
        downloader = MediaIoBaseDownload(fh, request, chunksize=CHUNK_SIZE)
        prev_bytes = 0
        with tqdm(total=file_size or None, unit="B", unit_scale=True,
                  desc=dest_path.name[:45], leave=False) as bar:
            done = False
            while not done:
                status, done = downloader.next_chunk()
                if status:
                    current = int(status.resumable_progress)
                    bar.update(current - prev_bytes)
                    prev_bytes = current


def extract_zip(zip_path: Path, dest_dir: Path):
    with zipfile.ZipFile(zip_path, "r") as zf:
        # 안전 경로 검증 (zip slip 방지)
        for member in zf.namelist():
            target = (dest_dir / member).resolve()
            if not str(target).startswith(str(dest_dir.resolve())):
                raise ValueError(f"Zip slip 감지: {member}")
        zf.extractall(dest_dir)


# ── 폴더 동기화 (재귀) ─────────────────────────────────────────────────────
def sync_folder(service, drive_folder_id: str, local_dir: Path, progress: dict):
    local_dir.mkdir(parents=True, exist_ok=True)
    page_token = None

    while True:
        resp = service.files().list(
            q=f"'{drive_folder_id}' in parents and trashed=false",
            fields="nextPageToken, files(id, name, mimeType, size)",
            pageSize=1000,
            pageToken=page_token,
        ).execute()

        for item in resp.get("files", []):
            item_name = item["name"]
            item_id   = item["id"]

            if item["mimeType"] == FOLDER_MIME:
                log.info(f"  📁 서브폴더 진입: {item_name}")
                sync_folder(service, item_id, local_dir / item_name, progress)
                continue

            if item_id in progress["downloaded"]:
                log.info(f"  스킵 (이미 완료): {item_name}")
                continue

            dest = local_dir / item_name
            size = int(item.get("size") or 0)

            try:
                log.info(f"  다운로드: {item_name} ({size / 1024 / 1024:.1f} MB)")
                download_file(service, item_id, dest, size)

                if item_name.lower().endswith(".zip"):
                    log.info(f"  ZIP 압축 해제: {item_name}")
                    extract_zip(dest, local_dir)
                    dest.unlink()
                    log.info(f"  ZIP 삭제 완료: {item_name}")

                progress["downloaded"].add(item_id)
                progress["failed"].discard(item_id)
                save_progress(progress)

            except Exception as e:
                log.error(f"  실패: {item_name} — {e}")
                progress["failed"].add(item_id)
                save_progress(progress)

        page_token = resp.get("nextPageToken")
        if not page_token:
            break


# ── 메인 ──────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(description="Google Drive → HanokAssets 동기화")
    parser.add_argument("--category", metavar="NAME",
                        help="특정 카테고리만 동기화 (예: 건축물완성형)")
    parser.add_argument("--reset", action="store_true",
                        help="진행 기록 초기화 후 전체 재다운로드")
    args = parser.parse_args()

    log.info("=" * 60)
    log.info("Drive → HanokAssets 동기화 시작")
    log.info(f"대상 폴더: {HANOK_ASSETS_ROOT}")
    log.info("=" * 60)

    if args.reset and PROGRESS_FILE.exists():
        PROGRESS_FILE.unlink()
        log.info("Progress 초기화 완료")

    progress = load_progress()

    service = get_drive_service()
    log.info("Google Drive 인증 완료")

    # 루트 폴더에서 카테고리 서브폴더 목록 조회
    resp = service.files().list(
        q=f"'{DRIVE_ROOT_ID}' in parents and mimeType='{FOLDER_MIME}' and trashed=false",
        fields="files(id, name)",
        pageSize=100,
    ).execute()

    drive_folders = {item["name"]: item["id"] for item in resp.get("files", [])}
    log.info(f"Drive 루트 카테고리: {list(drive_folders.keys())}")

    categories_to_sync = (
        {args.category: CATEGORY_MAP.get(args.category, args.category)}
        if args.category
        else CATEGORY_MAP
    )

    stats = {}
    for drive_name, local_name in categories_to_sync.items():
        if drive_name not in drive_folders:
            log.warning(f"Drive에서 폴더를 찾을 수 없음: '{drive_name}'")
            continue

        folder_id  = drive_folders[drive_name]
        local_path = HANOK_ASSETS_ROOT / local_name

        log.info(f"\n{'='*50}")
        log.info(f"카테고리: {drive_name}  →  {local_path}")

        before = len(progress["downloaded"])
        sync_folder(service, folder_id, local_path, progress)
        added = len(progress["downloaded"]) - before

        stats[drive_name] = added
        log.info(f"  완료: {added}개 파일 신규 다운로드")

    log.info("\n" + "=" * 60)
    log.info("동기화 완료 요약")
    for cat, count in stats.items():
        log.info(f"  {cat}: {count}개 신규")
    if progress["failed"]:
        log.warning(f"  실패: {len(progress['failed'])}개 — sync.log 확인")
    log.info("=" * 60)
    log.info("다음 단계: Unity 에디터에서 [HanokBuilder > Tools > Import Culture Assets] 실행")


if __name__ == "__main__":
    main()
