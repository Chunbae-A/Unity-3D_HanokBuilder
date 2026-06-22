"""
문화 에셋 4개 폴더 → JSON 매니페스트 자동 생성
출력: Assets/HanokBuilder/Resources/HanokManifest/{folder}.json

실행: python Tools/generate_culture_manifests.py
"""
import json
import re
from pathlib import Path

ROOT     = Path(__file__).parent.parent
HANOK    = ROOT / "Assets" / "HanokBuilder" / "Resources" / "HanokAssets"
OUT_DIR  = ROOT / "Assets" / "HanokBuilder" / "Resources" / "HanokManifest"

# ──────────────────────────────────────────────────────────────────────────────
# 건축물부품형  키워드 → (한글명, 서브카테고리)
# ──────────────────────────────────────────────────────────────────────────────
PARTS_KW: dict[str, tuple[str, str]] = {
    # 지붕
    "Ridge":               ("용마루",           "지붕"),
    "RoofStatue":          ("잡상",             "지붕"),
    "RoofBoard":           ("개판/산자",         "지붕"),
    "Roof":                ("지붕",             "지붕"),
    "HipRafter":           ("추녀",             "지붕"),
    "RafterExtension":     ("평고대",           "지붕"),
    "Rafter":              ("서까래",           "지붕"),
    "R_RoofCornerCut":     ("귀서까래(절단)",   "지붕"),
    "R_RoofCorner":        ("귀서까래",         "지붕"),
    "R_RoofOuter":         ("외목도리",         "지붕"),
    "R_Yuyotaek":          ("유여택 지붕",      "지붕"),
    "Byeolju_Roof":        ("별주 지붕",        "지붕"),
    "Byeolju_SmallGateRoof":("별주 소문 지붕", "지붕"),
    "Uhagwan_CenterRoof":  ("우하관 중앙 지붕", "지붕"),
    "Uhagwan_LeftRoof":    ("우하관 좌측 지붕", "지붕"),
    "Uhagwan_RightRoof":   ("우하관 우측 지붕", "지붕"),
    "GH_Parapet":          ("문루 여장",        "지붕"),
    "G_RoofBoard":         ("대문 개판",        "지붕"),
    "B_GateHouse_HipRafter":("문루 추녀",       "지붕"),
    "B_GateHouse_Rafter":  ("문루 서까래",      "지붕"),
    "B_GateHouse_Roof":    ("문루 지붕",        "지붕"),
    # 기둥/보
    "ColumnTopSupport":    ("기둥 상부 받침",   "기둥/보"),
    "Column":              ("기둥",             "기둥/보"),
    "Heartwood":           ("심주",             "기둥/보"),
    "PurlinSupport":       ("도리 받침",        "기둥/보"),
    "Purlin":              ("도리",             "기둥/보"),
    "BeamSupport":         ("보 받침",          "기둥/보"),
    "Crossbeam":           ("대들보",           "기둥/보"),
    "Ceiling":             ("천장",             "기둥/보"),
    "B_GateHouse_Column":  ("문루 기둥",        "기둥/보"),
    "B_GateHouse_Crossbeam":("문루 대들보",     "기둥/보"),
    # 포작/화반
    "ComplexBracket":      ("다포",             "포작/화반"),
    "BracketSupport":      ("포작 받침",        "포작/화반"),
    "Hwaban":              ("화반",             "포작/화반"),
    # 인방/창호
    "FlatLintel":          ("평인방",           "인방/창호"),
    "Lintel":              ("인방",             "인방/창호"),
    "Window_Frame":        ("창문틀",           "인방/창호"),
    "Window":              ("창",               "인방/창호"),
    "SideDoor_Frame":      ("협문틀",           "인방/창호"),
    "SideDoor":            ("협문",             "인방/창호"),
    "DoorStoper":          ("문멈춤쇠",         "인방/창호"),
    "DoorBase":            ("문짝",             "인방/창호"),
    "B_ArchDoor":          ("아치 문",          "인방/창호"),
    "B_GateHouse_Lintel":  ("문루 인방",        "인방/창호"),
    "B_MetalDoor":         ("철문",             "인방/창호"),
    "B_SmallArch":         ("작은 아치",        "인방/창호"),
    "G_MetalDoor":         ("철대문",           "인방/창호"),
    # 기단/계단
    "GateGuardPost_Stair": ("성문 초소 계단",   "기단/계단"),
    "CW_Stair":            ("성곽 계단",        "기단/계단"),
    "CW_Floor":            ("성곽 바닥",        "기단/계단"),
    "BarbicanStair":       ("바비칸 계단",      "기단/계단"),
    "BarbicanFloor":       ("바비칸 바닥",      "기단/계단"),
    "W_Foundation":        ("기초벽",           "기단/계단"),
    "RammedEarth":         ("다짐흙",           "기단/계단"),
    "G_Stair_Newel":       ("계단 난간 기둥",   "기단/계단"),
    "G_Stair":             ("석조 계단",        "기단/계단"),
    "StoneStair":          ("석조 계단",        "기단/계단"),
    "WoodStair":           ("목조 계단",        "기단/계단"),
    "Stair":               ("계단",             "기단/계단"),
    "Pedestal":            ("주춧돌",           "기단/계단"),
    "Floor":               ("바닥",             "기단/계단"),
    # 벽체
    "FortificationArch":   ("성벽 아치",        "벽체"),
    "FortificationWall":   ("성벽",             "벽체"),
    "StraightStronewall":  ("직선 돌담",        "벽체"),
    "BarbicanWall":        ("바비칸 성벽",      "벽체"),
    "CW_Stone":            ("성곽 석재",        "벽체"),
    "CW_Parapet":          ("성곽 여장",        "벽체"),
    "CW":                  ("성곽 벽체",        "벽체"),
    "W_WideBrick":         ("넓은 벽돌",        "벽체"),
    "W_Stone":             ("돌담",             "벽체"),
    "W_White":             ("흰 벽",            "벽체"),
    "W_Wood":              ("목조 벽",          "벽체"),
    "W_Intwhite":          ("내부 흰 벽",       "벽체"),
    "W_Orange":            ("주황 벽",          "벽체"),
    "W_Pink":              ("분홍 벽",          "벽체"),
    "W_Magenta":           ("자홍 벽",          "벽체"),
    "W_Mountain":          ("산 배경",          "벽체"),
    "WI_Wood":             ("목재 인테리어",    "벽체"),
    # 성곽/치성
    "GateGuardPost_Parapet":("성문 초소 여장",  "성곽/치성"),
    "GateGuardPost":       ("성문 초소",        "성곽/치성"),
    "Bastion_Parapet":     ("치성 여장",        "성곽/치성"),
    "Bastion":             ("치성",             "성곽/치성"),
    "Fortification":       ("성곽",             "성곽/치성"),
    "BarbicanRoofboard":   ("바비칸 개판",      "성곽/치성"),
    "BarbicanParapet":     ("바비칸 여장",      "성곽/치성"),
    "BarbicanDoorBase":    ("바비칸 문",        "성곽/치성"),
    "BarbicanArch":        ("바비칸 아치",      "성곽/치성"),
    # 문루/간판
    "R_R_GateModular":     ("성문 모듈",        "문루/간판"),
    "R_Seoricheong":       ("서리청",           "문루/간판"),
    "R_Sinpungru":         ("신풍루",           "문루/간판"),
    "R_Unhangag_Iancheong":("운한각·이안청",   "문루/간판"),
    "R_Sign_BH":           ("현판(BH)",         "문루/간판"),
    "R_Sign_DB":           ("현판(DB)",         "문루/간판"),
    "R_Sign_DH":           ("현판(DH)",         "문루/간판"),
    "R_Sign_EH":           ("현판(EH)",         "문루/간판"),
    "R_Sign_GH":           ("현판(GH)",         "문루/간판"),
    "R_Sign_GJ":           ("현판(GJ)",         "문루/간판"),
    "R_Sign_GO":           ("현판(GO)",         "문루/간판"),
    "R_Sign_GS":           ("현판(GS)",         "문루/간판"),
    "R_Sign_GY":           ("현판(GY)",         "문루/간판"),
    "R_Sign_HC":           ("현판(HC)",         "문루/간판"),
    "R_Sign_JB":           ("현판(JB)",         "문루/간판"),
    "R_Sign_JY":           ("현판(JY)",         "문루/간판"),
    "R_Sign_OR":           ("현판(OR)",         "문루/간판"),
    "R_Sign_SS":           ("현판(SS)",         "문루/간판"),
    "R_Sign_YB":           ("현판(YB)",         "문루/간판"),
    "R_Sign_YH":           ("현판(YH)",         "문루/간판"),
    "R_Sign_YY":           ("현판(YY)",         "문루/간판"),
    "R_Sign":              ("현판",             "문루/간판"),
    "Signboard_Janganmun": ("장안문 현판",      "문루/간판"),
    "Signboard_Paldalmun": ("팔달문 현판",      "문루/간판"),
    "Note_Janganmun":      ("장안문 설명판",    "문루/간판"),
    "Note_Paldalmun":      ("팔달문 설명판",    "문루/간판"),
    "B_GateHouse":         ("문루",             "문루/간판"),
    # 장식/자연
    "Hedgerows_Arch":      ("생울타리 아치",    "장식"),
    "Hedgerows":           ("생울타리",         "장식"),
    "Gargoyle":            ("잡상",             "장식"),
    "S_Circle":            ("원형 구조물",      "장식"),
    "S_Octagonal":         ("팔각 구조물",      "장식"),
    "S_Square":            ("사각 구조물",      "장식"),
}

# ──────────────────────────────────────────────────────────────────────────────
# 건축물완성형  이름 → 한글명
# ──────────────────────────────────────────────────────────────────────────────
COMPLETE_MAP: dict[str, str] = {
    "CM_SM_Bijangcheong":       "비장청",
    "CM_SM_Binhuimun":          "빈희문",
    "CM_SM_Bokgunyeong":        "복군영",
    "CM_SM_Boknaedang":         "복내당",
    "CM_SM_Bongsudang":         "봉수당",
    "CM_SM_Bueok":              "부엌",
    "CM_SM_Dabogmun":           "다복문",
    "CM_SM_Deughanmun":         "득한문",
    "CM_SM_Deugjungjeong":      "득중정",
    "CM_SM_Gaeomun":            "개오문",
    "CM_SM_Geonjangmun":        "건장문",
    "CM_SM_Gicheungheon":       "기천헌",
    "CM_SM_Gonglang":           "공랑",
    "CM_SM_Guyeomun":           "구여문",
    "CM_SM_Gyeonghwamun":       "경화문",
    "CM_SM_Gyeongryugwan":      "경류관",
    "CM_SM_Gyeongseonmun":      "경선문",
    "CM_SM_Heogan":             "홰간",
    "CM_SM_Hyangchunmun":       "향춘문",
    "CM_SM_Janganmun":          "장안문",
    "CM_SM_Jangbogmun":         "장복문",
    "CM_SM_Jeonsacheong":       "전사청",
    "CM_SM_Jibsacheong_1":      "집사청 1",
    "CM_SM_Jibsacheong_2":      "집사청 2",
    "CM_SM_Jibsacheong_Gate_1": "집사청 문 1",
    "CM_SM_Jibsacheong_Gate_2": "집사청 문 2",
    "CM_SM_Jungyagmun":         "중약문",
    "CM_SM_Jungyangmun":        "중양문",
    "CM_SM_Jwaikmun":           "좌익문",
    "CM_SM_LargeGate":          "대문",
    "CM_SM_Malang":             "마랑",
    "CM_SM_Maru":               "마루",
    "CM_SM_Mirohanjeong":       "미로한정",
    "CM_SM_Naeposa":            "내포사",
    "CM_SM_Naesamun":           "내삼문",
    "CM_SM_Namgunyeong":        "남군영",
    "CM_SM_Namnakhyeon":        "남락헌",
    "CM_SM_Nanlomun":           "난로문",
    "CM_SM_Nusanggo":           "누상고",
    "CM_SM_Oejeongriamun":      "외정리아문",
    "CM_SM_Oijeongriso":        "외정리소",
    "CM_SM_Oisamun":            "외삼문",
    "CM_SM_Ondol":              "온돌방",
    "CM_SM_Paldalmun":          "팔달문",
    "CM_SM_Punghuadang":        "풍화당",
    "CM_SM_RoyalWell":          "어정",
    "CM_SM_Samsumun":           "삼수문",
    "CM_SM_Seoricheong":        "서리청",
    "CM_SM_Sinpungru":          "신풍루",
    "CM_SM_SmallGate":          "소문",
    "CM_SM_Uhwagan":            "우화관",
    "CM_SM_Unhwagak_Iancheong": "운한각 이안청",
    "CM_SM_Yeonhwimun":         "연휘문",
    "CM_SM_Yubogmun":           "유복문",
    "CM_SM_Yuyeomun":           "유여문",
    "CM_SM_Yuyeotaek":          "유여택",
    "CM_1 Gwandeokjeong":       "관덕정",
    "CM_10_Yeongjuhyupdang":    "영주협당",
    "CM_12 Gyullimdang":        "귤림당",
    "CM_14 Mang-gyeongru":      "망경루",
    "CM_15 Yeonhuigak":         "연희각",
    "CM_20 Wooryeondang":       "우련당",
    "CM_23_Chungdaemun":        "충대문",
    "CM_27 Honghwagak":         "홍화각",
    "CM_5 Oedaemun":            "외대문",
    "CM_Agricultural_Equipment_Exhibition_Hall": "농기구 전시관",
    "CM_Anchae":                "안채",
    "CM_Changwon_Maru":         "창원 마루",
    "CM_Folk_Education_Hall":   "민속교육관",
    "CM_Gristmill":             "물레방아",
    "CM_house":                 "한옥",
    "CM_house2 ":               "한옥 2",
    "CM_Hyogyeongmun(back gate)": "효경문(후문)",
    "CM_Left":                  "좌측 건물",
    "CM_Left_freeze":           "좌측 건물(고정)",
    "CM_Left_Modular":          "좌측 모듈",
    "CM_Main_Gate":             "정문",
    "CM_Middle_Gate":           "중문",
    "CM_Multipurpose_Hall":     "다목적 홀",
    "CM_Octagonal_Pavilion":    "팔각정",
    "CM_Pavilion":              "정자",
    "CM_Right":                 "우측 건물",
    "CM_Right_Modular":         "우측 모듈",
    "CM_Sarangchae":            "사랑채",
    "CM_Small_Gate":            "협문",
    "CM_SM_Byeolchu":           "별주",
}

# ──────────────────────────────────────────────────────────────────────────────
# 디지털휴먼  이름 → (한글명, 서브카테고리)
# ──────────────────────────────────────────────────────────────────────────────
HUMAN_MAP: dict[str, tuple[str, str]] = {
    "CM_SM_Merchant_FeMale": ("여성 상인",        "상인"),
    "CM_SM_Merchant_Male":   ("남성 상인",        "상인"),
    "CM_woman_phong":        ("여성 캐릭터",      "메타휴먼"),
    "CM_메타휴먼_남_01":     ("메타휴먼 남성 1",  "메타휴먼"),
    "CM_메타휴먼_남_02":     ("메타휴먼 남성 2",  "메타휴먼"),
    "CM_메타휴먼_남_03":     ("메타휴먼 남성 3",  "메타휴먼"),
    "CM_메타휴먼_남_04":     ("메타휴먼 남성 4",  "메타휴먼"),
    "CM_곤방_전동작":        ("곤방 무예",        "무예"),
    "CM_교전_전동작":        ("교전 무예",        "무예"),
    "CM_권법_전동작":        ("권법 무예",        "무예"),
    "CM_낭선_전동작":        ("낭선 무예",        "무예"),
    "CM_당파_전동작":        ("당파 무예",        "무예"),
    "CM_등패_전동작":        ("등패 무예",        "무예"),
    "CM_쌍검_전동작":        ("쌍검 무예",        "무예"),
    "CM_쌍수도_전동작":      ("쌍수도 무예",      "무예"),
    "CM_월도_전동작":        ("월도 무예",        "무예"),
    "CM_장창_전동작":        ("장창 무예",        "무예"),
    "CM_협도_전동작":        ("협도 무예",        "무예"),
}


def classify_parts(key: str) -> tuple[str, str]:
    """부품형 영문 키 → (한글명, 서브카테고리)"""
    base = re.sub(r"^CM_SM_", "", key)
    base = re.sub(r"_\d+$", "", base)

    # 정확 매칭 (긴 키 먼저)
    for kw in sorted(PARTS_KW, key=len, reverse=True):
        if base == kw or base.startswith(kw + "_"):
            korean, sub = PARTS_KW[kw]
            # 숫자 번호가 있으면 뒤에 붙임
            num_match = re.search(r"(\d+)$", re.sub(r"^CM_SM_", "", key))
            suffix = f" {int(num_match.group(1)):02d}" if num_match else ""
            return f"{korean}{suffix}", sub

    return base, "기타"


def classify_props(key: str) -> tuple[str, str]:
    """공간소품 프리팹 키 → (한글명, 서브카테고리)"""
    # 이미 한글 이름이 있는 경우
    name = key.replace("CM_", "", 1).replace("CM_SM_", "")

    lower = name.lower()
    if "총통" in name or "군박물관" in name:
        return name, "무기/총통"
    if "석탑" in name or "석조" in name or "좌상" in name or "불상" in name or "광배" in name or "대좌" in name:
        return name, "석탑/불상"
    if "cloth" in lower or "민복" in name or "hat" in lower or "fan" in lower or "banggathat" in lower:
        return name, "의복/복식"
    if "flag" in lower or "두레기" in name or "두레놀이" in name:
        return name, "깃발/현수막"
    if "drum" in lower:
        return name, "악기/음악"
    return name, "소품/기타"


def scan_folder(folder: Path, category: str) -> list[dict]:
    prefabs_dir = folder / "Prefabs"
    if not prefabs_dir.exists():
        return []

    assets = []
    for p in sorted(prefabs_dir.glob("*.prefab")):
        key = p.stem
        res_path = f"HanokAssets/{folder.name}/Prefabs/{key}"

        if category == "건축물부품형":
            display, sub = classify_parts(key)
        elif category == "건축물완성형":
            display = COMPLETE_MAP.get(key, key.replace("CM_SM_", "").replace("CM_", ""))
            sub = ""
        elif category == "디지털휴먼":
            if key in HUMAN_MAP:
                display, sub = HUMAN_MAP[key]
            else:
                display = key.replace("CM_", "", 1)
                sub = "기타"
        else:  # 공간소품
            display, sub = classify_props(key)

        assets.append({
            "key":     key,
            "display": display,
            "sub":     sub,
            "path":    res_path,
        })

    return assets


def main():
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    folders = [
        ("건축물완성형", "건축물완성형"),
        ("건축물부품형", "건축물부품형"),
        ("공간소품",     "공간소품"),
        ("디지털휴먼",   "디지털휴먼"),
    ]

    for folder_name, category in folders:
        folder = HANOK / folder_name
        if not folder.exists():
            print(f"  없음: {folder}")
            continue

        assets = scan_folder(folder, category)
        out = {"category": category, "assets": assets}

        out_path = OUT_DIR / f"{folder_name}.json"
        with open(out_path, "w", encoding="utf-8") as f:
            json.dump(out, f, ensure_ascii=False, indent=2)

        subs = sorted({a["sub"] for a in assets if a["sub"]})
        print(f"[{category}] {len(assets)}개 → {out_path.name}")
        print(f"  서브카테고리: {', '.join(subs) if subs else '없음'}")

    print("\n완료! Unity에서 HanokBuilder > Tools > Reload Culture Manifests 실행하세요.")


if __name__ == "__main__":
    main()
