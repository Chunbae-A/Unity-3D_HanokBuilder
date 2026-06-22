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
    "R_Yuyotaek1":         ("유여택 지붕 01",   "지붕"),
    "R_Yuyotaek2":         ("유여택 지붕 02",   "지붕"),
    "R_Yuyotaek3":         ("유여택 지붕 03",   "지붕"),
    "R_Yuyotaek4":         ("유여택 지붕 04",   "지붕"),
    "R_Yuyotaek5":         ("유여택 지붕 05",   "지붕"),
    "R_Yuyotaek6":         ("유여택 지붕 06",   "지붕"),
    "R_Yuyotaek7":         ("유여택 지붕 07",   "지붕"),
    "R_Yuyotaek8":         ("유여택 지붕 08",   "지붕"),
    "R_Yuyotaek9":         ("유여택 지붕 09",   "지붕"),
    "R_Yuyotaek10":        ("유여택 지붕 10",   "지붕"),
    "R_Yuyotaek11":        ("유여택 지붕 11",   "지붕"),
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
    "W_Pink01":            ("분홍 벽 01",       "벽체"),
    "W_Pink02":            ("분홍 벽 02",       "벽체"),
    "W_Pink03":            ("분홍 벽 03",       "벽체"),
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


_PROPS_MAP: dict[str, tuple[str, str]] = {
    # ── 악기 ─────────────────────────────────────────────────────────────────
    "CM_1-1-Gun Drum":      ("군고 1-1",        "악기/음악"),
    "CM_1-2-Gun Drum":      ("군고 1-2",        "악기/음악"),
    "CM_Big Zither":        ("가야금(대)",       "악기/음악"),
    "CM_Chord":             ("줄/현",           "악기/음악"),
    "CM_Dragon Drum 14":    ("용고 14",         "악기/음악"),
    "CM_Drum15":            ("북 15",           "악기/음악"),
    "CM_Gayageum":          ("가야금",          "악기/음악"),
    "CM_GyobangDrum":       ("교방 북",         "악기/음악"),
    "CM_GyobangDrum2":      ("교방 북 2",       "악기/음악"),
    "CM_JejuShamanicJanggu":("제주 무속 장구",  "악기/음악"),
    "CM_Korean harp":       ("공후",            "악기/음악"),
    "CM_Moojang drum":      ("무장 북",         "악기/음악"),
    "CM_SajangDrum":        ("사장 북",         "악기/음악"),
    "CM_TangFlute":         ("당적",            "악기/음악"),
    "CM_Tewak":             ("테왁",            "악기/음악"),
    "CM_Wooden Beating":    ("목타",            "악기/음악"),
    "CM_Yogo":              ("요고",            "악기/음악"),
    # ── 의복/복식 ────────────────────────────────────────────────────────────
    "CM_SM_AnkleBands_Male":    ("행전(남)",         "의복/복식"),
    "CM_SM_BanggatHat":         ("방갓",             "의복/복식"),
    "CM_SM_ChestCover_Female":  ("가슴가리개(여)",   "의복/복식"),
    "CM_SM_ClothExcellency":    ("관료 의복",        "의복/복식"),
    "CM_SM_ClothExcellency_Bot":("관료 의복 하의",   "의복/복식"),
    "CM_SM_ClothExcellency_Hat":("관료 의복 모자",   "의복/복식"),
    "CM_SM_ClothExcellency_Tobacco":("관료 담뱃대",  "의복/복식"),
    "CM_SM_ClothExcellency_Top":("관료 의복 상의",   "의복/복식"),
    "CM_SM_ClothWhite":         ("흰색 민복",        "의복/복식"),
    "CM_SM_ClothWhite_Bot":     ("흰색 민복 하의",   "의복/복식"),
    "CM_SM_ClothWhite_Hat":     ("흰색 민복 모자",   "의복/복식"),
    "CM_SM_ClothWhite_Top":     ("흰색 민복 상의",   "의복/복식"),
    "CM_SM_ClothYellow":        ("황토 민복",        "의복/복식"),
    "CM_SM_ClothYellow_Bot":    ("황토 민복 하의",   "의복/복식"),
    "CM_SM_ClothYellow_Hat":    ("황토 민복 모자",   "의복/복식"),
    "CM_SM_ClothYellow_Top":    ("황토 민복 상의",   "의복/복식"),
    "CM_SM_FrontSkirt_Female":  ("앞치마(여)",       "의복/복식"),
    "CM_SM_Headband_Male":      ("머리띠(남)",       "의복/복식"),
    "CM_SM_Jeogori_Female":     ("저고리(여)",       "의복/복식"),
    "CM_SM_Jeogori_Male":       ("저고리(남)",       "의복/복식"),
    "CM_SM_MerchantCloth_Female":("상인 의복(여)",   "의복/복식"),
    "CM_SM_MerchantCloth_Male": ("상인 의복(남)",    "의복/복식"),
    "CM_SM_Pants_Female":       ("바지(여)",         "의복/복식"),
    "CM_SM_Pants_Male":         ("바지(남)",         "의복/복식"),
    "CM_SM_Red_Uniform":        ("붉은 제복",        "의복/복식"),
    "CM_SM_Shoes_Female":       ("신발(여)",         "의복/복식"),
    "CM_SM_Shoes_Male":         ("신발(남)",         "의복/복식"),
    "CM_SM_Skirt_Female":       ("치마(여)",         "의복/복식"),
    "CM_SM_StrawHat_Male":      ("삿갓(남)",         "의복/복식"),
    "CM_SM_StrawShoes_01":      ("짚신 1",           "의복/복식"),
    "CM_SM_StrawShoes_02":      ("짚신 2",           "의복/복식"),
    "CM_SM_TraditionalSocks_Female":("버선(여)",     "의복/복식"),
    "CM_SM_TraditionalSocks_male":  ("버선(남)",     "의복/복식"),
    "CM_SM_UjangRaincoat":      ("우장",             "의복/복식"),
    "CM_TraditionalHat":        ("전통 모자",        "의복/복식"),
    "CM_Woodenshoes_01":        ("나막신 1",         "의복/복식"),
    "CM_Woodenshoes_02":        ("나막신 2",         "의복/복식"),
    "CM_Wristlet_01":           ("팔찌 1",           "의복/복식"),
    # ── 농기구 ───────────────────────────────────────────────────────────────
    "CM_Adze_01":              ("자귀 1",           "농기구"),
    "CM_Adze_02":              ("자귀 2",           "농기구"),
    "CM_Adze_03":              ("자귀 3",           "농기구"),
    "CM_Hammer_01":            ("망치 1",           "농기구"),
    "CM_Hammer_02":            ("망치 2",           "농기구"),
    "CM_Hammer_03":            ("망치 3",           "농기구"),
    "CM_Hammer_04":            ("망치 4",           "농기구"),
    "CM_Iron":                 ("다리미",           "농기구"),
    "CM_Mill":                 ("맷돌",             "농기구"),
    "CM_Plane_01":             ("대패 1",           "농기구"),
    "CM_Plane_02":             ("대패 2",           "농기구"),
    "CM_Plane_03":             ("대패 3",           "농기구"),
    "CM_Plane_04":             ("대패 4",           "농기구"),
    "CM_RipSaw_02":            ("톱 2",             "농기구"),
    "CM_Sickle":               ("낫",               "농기구"),
    "CM_Spin_Drill":           ("드릴",             "농기구"),
    "CM_SM_Flail":             ("도리깨",           "농기구"),
    "CM_SM_GomulaeRake":       ("고무래",           "농기구"),
    "CM_SM_JaenggiPlow":       ("쟁기",             "농기구"),
    "CM_SM_JeolguThresher":    ("절구",             "농기구"),
    "CM_SM_JonggalaeShovel":   ("종갈래 삽",        "농기구"),
    "CM_SM_KoreanHandPlow":    ("손쟁기",           "농기구"),
    "CM_SM_MeHammer":          ("메",               "농기구"),
    "CM_SM_MildaeMop":         ("밀대",             "농기구"),
    "CM_SM_Mortar":            ("절구통",           "농기구"),
    "CM_SM_Pestle":            ("절구공이",         "농기구"),
    "CM_SM_Rake":              ("갈퀴",             "농기구"),
    "CM_SM_Sickle":            ("낫",               "농기구"),
    "CM_SM_SorghumBroom":      ("수수빗자루",       "농기구"),
    "CM_SM_SseolaeHarrow":     ("써레",             "농기구"),
    "CM_SM_Thresher":          ("탈곡기",           "농기구"),
    "CM_SM_ThreshingFan":      ("키",               "농기구"),
    "CM_SM_GalaeRope":         ("갈래 밧줄",        "농기구"),
    "CM_SM_GalaeShovel":       ("갈래 삽",          "농기구"),
    "CM_SM_GalaeShovel_NoRope":("갈래 삽(줄 없음)", "농기구"),
    # ── 깃발/현수막 ──────────────────────────────────────────────────────────
    "CM_SM_BaeghogiFlag":      ("백호기",           "깃발/현수막"),
    "CM_SM_CheonglyonggiFlag": ("청룡기",           "깃발/현수막"),
    "CM_SM_DeungsagiFlag":     ("등사기",           "깃발/현수막"),
    "CM_SM_DulegiFlag1":       ("두레기 1",         "깃발/현수막"),
    "CM_SM_DulegiFlag2":       ("두레기 2",         "깃발/현수막"),
    "CM_SM_Flag_Wind_1":       ("풍기 1",           "깃발/현수막"),
    "CM_SM_Flag_Wind_2":       ("풍기 2",           "깃발/현수막"),
    "CM_SM_Flag_Wind_3":       ("풍기 3",           "깃발/현수막"),
    "CM_SM_Flag_Wind_4":       ("풍기 4",           "깃발/현수막"),
    "CM_SM_Flag_Wind_5":       ("풍기 5",           "깃발/현수막"),
    "CM_SM_Flag_Wind_6":       ("풍기 6",           "깃발/현수막"),
    "CM_SM_HyeonmugiFlag":     ("현무기",           "깃발/현수막"),
    "CM_SM_JujaggiFlag":       ("주작기",           "깃발/현수막"),
    "CM_SM_TownFlag1":         ("마을 깃발 1",      "깃발/현수막"),
    "CM_SM_TownFlag2":         ("마을 깃발 2",      "깃발/현수막"),
    "CM_SM_TownFlag3":         ("마을 깃발 3",      "깃발/현수막"),
    "CM_SM_TownFlag4":         ("마을 깃발 4",      "깃발/현수막"),
    "CM_SM_YeonggiFlag1":      ("영기 1",           "깃발/현수막"),
    "CM_SM_YeonggiFlag2":      ("영기 2",           "깃발/현수막"),
    # ── 자연/식물 ────────────────────────────────────────────────────────────
    "CM_SM_Bush":              ("덤불",             "자연/식물"),
    "CM_SM_FineTree":          ("소나무",           "자연/식물"),
    "CM_SM_Grass":             ("풀",               "자연/식물"),
    "CM_SM_WIllowTree":        ("버드나무",         "자연/식물"),
    "CM_SM_Zelkova_01":        ("느티나무 1",       "자연/식물"),
    "CM_SM_Zelkova_02":        ("느티나무 2",       "자연/식물"),
    "CM_Goose_01":             ("거위 1",           "자연/식물"),
    "CM_Goose_02":             ("거위 2",           "자연/식물"),
    # ── 도구/생활용품 ────────────────────────────────────────────────────────
    "CM_Box_01":               ("상자 1",           "도구/생활용품"),
    "CM_Box_02":               ("상자 2",           "도구/생활용품"),
    "CM_Box_03":               ("상자 3",           "도구/생활용품"),
    "CM_Cabinet_02":           ("장롱",             "도구/생활용품"),
    "CM_Cylinder":             ("원통",             "도구/생활용품"),
    "CM_Dressing_Stand":       ("경대",             "도구/생활용품"),
    "CM_Jar":                  ("항아리",           "도구/생활용품"),
    "CM_Kettle":               ("주전자",           "도구/생활용품"),
    "CM_Latch_01":             ("빗장 1",           "도구/생활용품"),
    "CM_Latch_02":             ("빗장 2",           "도구/생활용품"),
    "CM_Lattice_Window":       ("격자창",           "도구/생활용품"),
    "CM_Three_Tier_Box":       ("삼단 함",          "도구/생활용품"),
    "CM_SM_Backpack_Male":     ("등짐(남)",         "도구/생활용품"),
    "CM_SM_Basket":            ("바구니",           "도구/생활용품"),
    "CM_SM_CarrierCushion":    ("짐받이 방석",      "도구/생활용품"),
    "CM_SM_CucurbitBucket":    ("바가지",           "도구/생활용품"),
    "CM_SM_DosjaliMat":        ("도사리 멍석",      "도구/생활용품"),
    "CM_SM_DuteuleCushion":    ("두테를 방석",      "도구/생활용품"),
    "CM_SM_EggBag":            ("달걀 바구니",      "도구/생활용품"),
    "CM_SM_FoodMesh":          ("음식 덮개",        "도구/생활용품"),
    "CM_SM_FrameCarrier":      ("지게",             "도구/생활용품"),
    "CM_SM_Hamper":            ("광주리",           "도구/생활용품"),
    "CM_SM_Haystack":          ("볏짚 더미",        "도구/생활용품"),
    "CM_SM_JolongtaegiBag":    ("졸롱태기",         "도구/생활용품"),
    "CM_SM_MeongseogMat":      ("멍석",             "도구/생활용품"),
    "CM_SM_PanaebagLadle":     ("파나배기 국자",    "도구/생활용품"),
    "CM_SM_PullCar":           ("손수레",           "도구/생활용품"),
    "CM_SM_Rice":              ("쌀",               "도구/생활용품"),
    "CM_SM_RiceBale":          ("볏짚 단",          "도구/생활용품"),
    "CM_SM_RiceSeedlings":     ("모내기 모",        "도구/생활용품"),
    "CM_SM_Rope":              ("밧줄",             "도구/생활용품"),
    "CM_SM_SackOfRice":        ("쌀자루",           "도구/생활용품"),
    "CM_SM_SamtaegiTray":      ("삼태기",           "도구/생활용품"),
    "CM_SM_Scale":             ("저울",             "도구/생활용품"),
    "CM_SM_Scarecrow":         ("허수아비",         "도구/생활용품"),
    "CM_SM_SheafOfRice":       ("벼 단",            "도구/생활용품"),
    "CM_SM_Torch":             ("횃불",             "도구/생활용품"),
    "CM_SM_Tray":              ("쟁반",             "도구/생활용품"),
    "CM_SM_TtwaliPedestal":    ("뚜왈리 받침대",    "도구/생활용품"),
    "CM_SM_TtwalitaeRope":     ("뚜왈리태 밧줄",    "도구/생활용품"),
    "CM_SM_Winnower":          ("키",               "도구/생활용품"),
    "CM_SM_YongduleLadle":     ("용두레 국자",      "도구/생활용품"),
    "CM_SM_YongduleLadle_Base":("용두레 받침",      "도구/생활용품"),
    "CM_SM_YongduleLadle_Ladle":("용두레",          "도구/생활용품"),
    # ── 수레/운반 ────────────────────────────────────────────────────────────
    "CM_SM_BalchaeSaddle":     ("발채 안장",        "수레/운반"),
    "CM_SM_GilmaSaddle":       ("길마 안장",        "수레/운반"),
    # ── 무기/군사 ────────────────────────────────────────────────────────────
    "CM_SM_BulimangMuzzle":    ("부리망",           "무기/총통"),
    "CM_SM_Cannon":            ("대포",             "무기/총통"),
    "CM_SM_CannonParts_001":   ("포차 부품 1",      "무기/총통"),
    "CM_SM_CannonParts_002":   ("포차 부품 2",      "무기/총통"),
    "CM_SM_CannonParts_003":   ("포차 부품 3",      "무기/총통"),
    "CM_SM_LineFan":           ("선풍",             "무기/총통"),
    # ── 도자기/미술 ──────────────────────────────────────────────────────────
    "CM_Porcelain":            ("도자기",           "도자기/미술"),
    "CM_Porcelain_02":         ("도자기 2",         "도자기/미술"),
    "CM_Porcelain_03":         ("도자기 3",         "도자기/미술"),
    "CM_Porcelain_04":         ("도자기 4",         "도자기/미술"),
    "CM_Prorcelain_05":        ("도자기 5",         "도자기/미술"),
    "CM_Porcelain_06":         ("도자기 6",         "도자기/미술"),
    "CM_Porcelain_07":         ("도자기 7",         "도자기/미술"),
    "CM_Porcelain_08":         ("도자기 8",         "도자기/미술"),
    "CM_Porcelain_09":         ("도자기 9",         "도자기/미술"),
    "CM_Porcelain_10":         ("도자기 10",        "도자기/미술"),
    "CM_SM_Amseog_01":         ("암석 1",           "도자기/미술"),
    "CM_SM_Amseog_02":         ("암석 2",           "도자기/미술"),
    # ── 기타 소품 ────────────────────────────────────────────────────────────
    "CM_Prop_10":              ("소품 10",          "소품/기타"),
    "CM_Props_10":             ("소품 세트 10",     "소품/기타"),
    "CM_SM_Cow":               ("소",               "소품/기타"),
}


def classify_props(key: str) -> tuple[str, str]:
    """공간소품 프리팹 키 → (한글명, 서브카테고리)"""
    # 직접 매핑 우선
    if key in _PROPS_MAP:
        return _PROPS_MAP[key]

    # 이미 한글인 경우 (한국어 문자 포함)
    name = key.replace("CM_SM_", "").replace("CM_", "", 1)
    if any(ord(c) > 127 for c in name):
        lower = name.lower()
        if "총통" in name or "군박물관" in name:
            return name, "무기/총통"
        if "석탑" in name or "석조" in name or "좌상" in name or "불상" in name or "광배" in name or "대좌" in name:
            return name, "석탑/불상"
        if "두레기" in name or "두레놀이" in name or "깃발" in name:
            return name, "깃발/현수막"
        return name, "소품/기타"

    # 영어 fallback — 패턴 매칭
    lower = name.lower()
    if "flag" in lower:
        return name, "깃발/현수막"
    if "drum" in lower or "zither" in lower or "harp" in lower or "flute" in lower:
        return name, "악기/음악"
    if any(w in lower for w in ("cloth", "hat", "shoes", "pants", "skirt", "sock", "coat", "uniform")):
        return name, "의복/복식"
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
