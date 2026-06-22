"""
CM_ Prefab Variant GUID 복구 스크립트
건축물완성형/Prefabs/*.prefab 의 깨진 base GUID를
현재 FBX .meta GUID로 교체한다.

실행:
    python Tools/fix_cm_prefab_guids.py
"""
import re
import sys
from pathlib import Path

ROOT       = Path(__file__).parent.parent
HANOK      = ROOT / "Assets" / "HanokBuilder" / "Resources" / "HanokAssets"
PREFAB_DIRS = [
    HANOK / "건축물완성형" / "Prefabs",
    HANOK / "건축물부품형" / "Prefabs",
    HANOK / "공간소품"    / "Prefabs",
    HANOK / "디지털휴먼"  / "Prefabs",
]
FBX_SEARCH_DIRS = [
    HANOK / "건축물완성형",
    HANOK / "건축물부품형",
    HANOK / "공간소품",
    HANOK / "디지털휴먼",
]

GUID_RE = re.compile(r'guid: ([0-9a-f]{32})', re.IGNORECASE)


def build_fbx_guid_map():
    """FBX 파일명(소문자, 확장자 없음) → GUID 딕셔너리"""
    fbx_map = {}
    for search_dir in FBX_SEARCH_DIRS:
        if not search_dir.exists():
            continue
        for meta in search_dir.rglob("*.fbx.meta"):
            content = meta.read_text(encoding="utf-8", errors="ignore")
            m = GUID_RE.search(content)
            if m:
                key = meta.stem.replace(".fbx", "").lower()  # SM_Bijangcheong
                fbx_map[key] = m.group(1)
    return fbx_map


def fix_prefabs(fbx_map):
    fixed = skipped = missing = 0

    for prefab_dir in PREFAB_DIRS:
        if not prefab_dir.exists():
            continue

        for prefab_path in prefab_dir.glob("*.prefab"):
            # CM_SM_Bijangcheong → SM_Bijangcheong
            base_name = prefab_path.stem
            if base_name.upper().startswith("CM_"):
                fbx_key = base_name[3:].lower()   # 앞 "CM_" 제거
            else:
                fbx_key = base_name.lower()

            if fbx_key not in fbx_map:
                print(f"  FBX 없음: {prefab_path.name} (찾는 키: {fbx_key})")
                missing += 1
                continue

            new_guid = fbx_map[fbx_key]
            content  = prefab_path.read_text(encoding="utf-8", errors="ignore")

            # 이 프리팹이 참조하는 현재 (깨진) base GUID 추출
            # m_SourcePrefab 라인 근처의 guid 가 base GUID
            src_match = re.search(r'm_SourcePrefab.*?guid: ([0-9a-f]{32})', content, re.DOTALL)
            if not src_match:
                skipped += 1
                continue

            old_guid = src_match.group(1)
            if old_guid == new_guid:
                skipped += 1
                continue

            new_content = content.replace(old_guid, new_guid)
            prefab_path.write_text(new_content, encoding="utf-8")
            print(f"  고침: {prefab_path.name}  {old_guid[:8]}… → {new_guid[:8]}…")
            fixed += 1

    return fixed, skipped, missing


def main():
    print("FBX GUID 맵 구축 중...")
    fbx_map = build_fbx_guid_map()
    print(f"  FBX 파일 {len(fbx_map)}개 인식")

    print("\nCM_ Prefab GUID 교체 중...")
    fixed, skipped, missing = fix_prefabs(fbx_map)

    print(f"""
======================================
 완료
======================================
  수정: {fixed}개
  스킵(이미 정상): {skipped}개
  FBX 없음: {missing}개

※ Unity 에디터가 열려있으면 닫고 실행하세요.
  실행 후 Unity를 다시 열면 프리팹이 정상으로 복구됩니다.
""")


if __name__ == "__main__":
    main()
