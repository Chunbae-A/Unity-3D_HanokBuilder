"""
한글/괄호 포함 .fbm 폴더 텍스처 → SharedTextures 이동
Unity AssetDatabase가 한글 폴더명을 인덱싱 못하는 문제 해결

실행: python Tools/move_korean_fbm_textures.py
"""
import shutil
from pathlib import Path

HANOK       = Path(__file__).parent.parent / "Assets" / "HanokBuilder" / "Resources" / "HanokAssets"
SHARED_TEX  = HANOK / "SharedTextures"
SEARCH_DIRS = [HANOK / "공간소품", HANOK / "건축물완성형", HANOK / "건축물부품형", HANOK / "디지털휴먼"]
TEX_EXTS    = {".png", ".jpg", ".jpeg", ".tga", ".bmp", ".exr", ".tiff", ".tif"}


def has_non_ascii(name: str) -> bool:
    try:
        name.encode("ascii")
        return False
    except UnicodeEncodeError:
        return True


def is_problematic_folder(folder: Path) -> bool:
    """한글 또는 괄호 포함 폴더"""
    name = folder.name
    return has_non_ascii(name) or "(" in name or ")" in name


def move_textures():
    SHARED_TEX.mkdir(exist_ok=True)
    moved = skipped = already = 0

    for search_dir in SEARCH_DIRS:
        if not search_dir.exists():
            continue

        fbm_dirs = [d for d in search_dir.rglob("*.fbm") if d.is_dir()]
        problem_dirs = [d for d in fbm_dirs if is_problematic_folder(d)]

        for fbm in problem_dirs:
            for f in fbm.iterdir():
                if not f.is_file() or f.suffix.lower() not in TEX_EXTS:
                    continue

                dst = SHARED_TEX / f.name
                meta_src = f.with_suffix(f.suffix + ".meta")
                meta_dst = dst.with_suffix(dst.suffix + ".meta")

                if dst.exists():
                    # 이미 있으면 중복 — 삭제
                    f.unlink(missing_ok=True)
                    if meta_src.exists():
                        meta_src.unlink(missing_ok=True)
                    skipped += 1
                    continue

                # 텍스처 + .meta 같이 이동
                shutil.move(str(f), str(dst))
                if meta_src.exists():
                    shutil.move(str(meta_src), str(meta_dst))
                moved += 1

            # 빈 폴더 + .meta 정리
            remaining = list(fbm.iterdir())
            if not remaining:
                fbm_meta = fbm.with_suffix(fbm.suffix + ".meta")
                if fbm_meta.exists():
                    fbm_meta.unlink(missing_ok=True)
                fbm.rmdir()
                print(f"  폴더 삭제: {fbm.name}")
            else:
                non_meta = [x for x in remaining if x.suffix != ".meta"]
                if non_meta:
                    already += len(non_meta)

    print(f"\n완료!")
    print(f"  SharedTextures 이동: {moved}개")
    print(f"  중복 삭제: {skipped}개")
    print(f"  비텍스처 파일 남김: {already}개")


if __name__ == "__main__":
    print("한글 .fbm 폴더 텍스처 → SharedTextures 이동 중...")
    move_textures()
