"""
.fbm 폴더 텍스처 중복 제거 스크립트
- 동일 파일명 텍스처는 하나만 남기고 삭제
- 고유 텍스처는 SharedTextures 폴더로 통합
- 빈 .fbm 폴더 정리
"""
import os
import shutil
from pathlib import Path
from collections import defaultdict

HANOK_ASSETS = Path(__file__).parent.parent / "Assets" / "HanokBuilder" / "Resources" / "HanokAssets"
SHARED_TEX   = HANOK_ASSETS / "SharedTextures"

TEXTURE_EXTS = {".png", ".jpg", ".jpeg", ".tga", ".bmp", ".exr", ".psd", ".tiff", ".tif"}

def main():
    print(f"HanokAssets: {HANOK_ASSETS}")
    SHARED_TEX.mkdir(exist_ok=True)

    fbm_folders = sorted(HANOK_ASSETS.rglob("*.fbm"))
    print(f".fbm 폴더 수: {len(fbm_folders)}")

    all_files: dict[str, list[Path]] = defaultdict(list)
    for folder in fbm_folders:
        for f in folder.iterdir():
            if f.is_file() and f.suffix.lower() in TEXTURE_EXTS:
                all_files[f.name].append(f)

    total   = sum(len(v) for v in all_files.values())
    unique  = len(all_files)
    dupes   = sum(len(v) - 1 for v in all_files.values() if len(v) > 1)
    print(f"전체 텍스처: {total}개 / 고유 파일명: {unique}개 / 중복: {dupes}개")

    saved_bytes = 0
    moved = deleted = 0

    for name, paths in all_files.items():
        dst = SHARED_TEX / name
        if not dst.exists():
            shutil.move(str(paths[0]), str(dst))
            moved += 1
            rest = paths[1:]
        else:
            rest = paths

        for dup in rest:
            if dup.exists():
                saved_bytes += dup.stat().st_size
                dup.unlink()
                deleted += 1

    empty = 0
    for folder in fbm_folders:
        if folder.exists() and not any(folder.iterdir()):
            folder.rmdir()
            empty += 1

    print(f"\n완료!")
    print(f"  SharedTextures 이동: {moved}개")
    print(f"  중복 삭제: {deleted}개")
    print(f"  빈 .fbm 폴더 삭제: {empty}개")
    print(f"  절약 용량: {saved_bytes/1024**3:.1f}GB")

if __name__ == "__main__":
    main()
