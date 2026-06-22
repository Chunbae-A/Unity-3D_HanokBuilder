"""
HanokAssets 정리 스크립트
drive_asset_sync.py 실행 후 이 스크립트를 실행하세요.

실행 방법:
    cd <프로젝트 루트>
    python Tools/cleanup_hanokassets.py
"""
import os
import shutil
from pathlib import Path

ROOT         = Path(__file__).parent.parent
HANOK_ASSETS = ROOT / "Assets" / "HanokBuilder" / "Resources" / "HanokAssets"

JUNK_DIRS  = {"Library", "Packages", "ProjectSettings"}
JUNK_EXTS  = {".unitypackage", ".spp"}

def step1_remove_nested_unity_folders():
    print("\n[1/3] 중첩 Unity 프로젝트 폴더 삭제...")
    found = [d for d in HANOK_ASSETS.rglob("*") if d.is_dir() and d.name in JUNK_DIRS]
    if not found:
        print("  없음")
        return
    for d in found:
        print(f"  삭제: {d}")
        shutil.rmtree(d, ignore_errors=True)

def step2_remove_junk_files():
    print("\n[2/3] 불필요한 소스 파일 삭제 (.unitypackage, .spp)...")
    found = [f for f in HANOK_ASSETS.rglob("*") if f.is_file() and f.suffix.lower() in JUNK_EXTS]
    if not found:
        print("  없음")
        return
    for f in found:
        size_mb = round(f.stat().st_size / 1024**2, 1)
        print(f"  삭제: {f.name} ({size_mb}MB)")
        f.unlink()

def step3_dedup_textures():
    print("\n[3/3] 중복 텍스처 제거...")
    dedup = Path(__file__).parent / "dedup_textures.py"
    import runpy
    runpy.run_path(str(dedup), run_name="__main__")

def print_unity_steps():
    print("""
======================================
 정리 완료! 이제 Unity 에디터에서:
======================================

  [4] HanokBuilder > Tools > Extract & Upgrade FBX Materials
      FBX 머티리얼 추출 + SharedTextures 연결

  [5] Window > Rendering > Render Pipeline Converter
      Material Upgrade 체크 후 Initialize and Convert
      (Standard → URP 변환, 흰색 메쉬 해결)

  [6] HanokBuilder > Tools > Import Culture Assets
      FBX → Prefab + AssetInfo SO 일괄 생성
""")

if __name__ == "__main__":
    print("======================================")
    print(" HanokAssets 정리 시작")
    print("======================================")
    step1_remove_nested_unity_folders()
    step2_remove_junk_files()
    step3_dedup_textures()
    print_unity_steps()
