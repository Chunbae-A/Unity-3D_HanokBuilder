"""
HanokBuilder 에셋 파이프라인 — Drive 동기화부터 Unity 임포트 전까지 후처리를 일괄 실행.

Phase 1  (Drive 동기화 완료 후 실행):
  1. DriveAssetSync          — Drive → Assets/HanokBuilder/Resources/HanokAssets
  2. cleanup_hanokassets     — 불필요 파일·중첩 Unity 프로젝트 제거 + 중복 텍스처 통합
  3. move_korean_fbm_textures — 한글/.fbm 텍스처 → SharedTextures 이동

Phase 2  (Unity 에디터에서 Import Culture Assets 완료 후 실행):
  4. generate_culture_manifests — Prefabs 스캔 → HanokManifest/*.json 생성
  5. fix_cm_prefab_guids        — CM_ Prefab GUID 복구

사용법:
  python Tools/pipeline.py              # Phase 1만 실행 (Drive 동기화 포함)
  python Tools/pipeline.py --skip-sync  # Drive 동기화 건너뜀, 나머지 Phase 1 실행
  python Tools/pipeline.py --phase2     # Phase 2만 실행 (Unity Import 완료 후)
  python Tools/pipeline.py --all        # Phase 1 + Phase 2 연속 실행

자세한 가이드: Tools/GUIDE.md
"""

import argparse
import runpy
import sys
from pathlib import Path

TOOLS = Path(__file__).parent

UNITY_PROMPT = """
╔═══════════════════════════════════════════════════════════════╗
║       Phase 1 완료 — Unity 에디터에서 3단계를 실행하세요       ║
╚═══════════════════════════════════════════════════════════════╝

  [1] HanokBuilder > Tools > Extract & Upgrade FBX Materials
      FBX 머티리얼 추출 + SharedTextures 연결

  [2] Window > Rendering > Render Pipeline Converter
      Material Upgrade 체크 후 Initialize and Convert
      (Standard → URP 변환, 흰색 메시 문제 해결)

  [3] HanokBuilder > Tools > Import Culture Assets
      FBX → Prefab + AssetInfo ScriptableObject 일괄 생성

Unity 작업 완료 후 Phase 2 실행:
  python Tools/pipeline.py --phase2
"""


def _run(script: Path, label: str):
    """sys.argv를 격리한 채 단일 스크립트를 __main__으로 실행."""
    print(f"\n{'=' * 60}")
    print(f"  {label}")
    print(f"{'=' * 60}")
    old_argv = sys.argv[:]
    sys.argv = [str(script)]
    try:
        runpy.run_path(str(script), run_name="__main__")
    finally:
        sys.argv = old_argv


def run_sync():
    _run(TOOLS / "DriveAssetSync" / "drive_asset_sync.py",
         "Step 1 — Google Drive → HanokAssets 동기화")


def run_cleanup():
    _run(TOOLS / "cleanup_hanokassets.py",
         "Step 2 — 불필요 파일 제거 + 중복 텍스처 통합")


def run_move_korean():
    _run(TOOLS / "move_korean_fbm_textures.py",
         "Step 3 — 한글 .fbm 텍스처 → SharedTextures 이동")


def run_manifests():
    _run(TOOLS / "generate_culture_manifests.py",
         "Step 4 — Prefab 스캔 → HanokManifest/*.json 생성")


def run_fix_guids():
    _run(TOOLS / "fix_cm_prefab_guids.py",
         "Step 5 — CM_ Prefab GUID 복구")


def main():
    parser = argparse.ArgumentParser(
        description="HanokBuilder 에셋 파이프라인",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "--phase2", action="store_true",
        help="Phase 2만 실행 (매니페스트 생성 + GUID 복구)"
    )
    group.add_argument(
        "--all", action="store_true",
        help="Phase 1 + Phase 2 연속 실행"
    )
    parser.add_argument(
        "--skip-sync", action="store_true",
        help="Drive 동기화(Step 1)를 건너뜀"
    )
    args = parser.parse_args()

    if args.phase2:
        run_manifests()
        run_fix_guids()
        return

    # Phase 1
    if not args.skip_sync:
        run_sync()
    run_cleanup()
    run_move_korean()

    if args.all:
        run_manifests()
        run_fix_guids()
    else:
        print(UNITY_PROMPT)


if __name__ == "__main__":
    main()
