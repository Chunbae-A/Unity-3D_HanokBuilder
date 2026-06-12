using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — 오른쪽 패널: AI 프롬프트 입력 + AI 에셋 추천 (partial)
/// </summary>
public partial class HanokUIManager
{
    void BuildEditPanel(Transform content)
    {
        // ── AI 프롬프트 입력 바 ────────────────────────────
        BuildAIInputBar(content);

        // ── AI 에셋 추천 ──────────────────────────────────
        Spacer(content, 8);
        BuildAIRecommendationSection(content);

        Spacer(content, 12);

        // 이름 카드
        var nameCard = RowBox(content, "NameCard", 52, BG_PANEL);
        var nameCardImg = nameCard.GetComponent<Image>();
        var ncOutline = nameCard.gameObject.AddComponent<Outline>();
        ncOutline.effectColor = BORDER; ncOutline.effectDistance = new Vector2(1,-1);

        // 왼쪽 네이비 액센트 바
        var accentBar = new GameObject("Accent");
        accentBar.transform.SetParent(nameCard, false);
        var abRT = accentBar.AddComponent<RectTransform>();
        abRT.anchorMin = new Vector2(0,0); abRT.anchorMax = new Vector2(0,1);
        abRT.offsetMin = Vector2.zero; abRT.offsetMax = new Vector2(4, 0);
        accentBar.AddComponent<Image>().color = NAVY;

        var nt = new GameObject("NameT"); nt.transform.SetParent(nameCard, false);
        var ntRT = nt.AddComponent<RectTransform>();
        ntRT.anchorMin = Vector2.zero; ntRT.anchorMax = Vector2.one;
        ntRT.offsetMin = new Vector2(12, 4); ntRT.offsetMax = new Vector2(-8, -4);
        infoNameText = nt.AddComponent<TextMeshProUGUI>();
        infoNameText.text = "부재를 선택하세요";
        infoNameText.fontSize = 12;
        infoNameText.fontStyle = FontStyles.Bold;
        infoNameText.color = TEXT_HINT;
        infoNameText.alignment = TextAlignmentOptions.Left;
        infoNameText.overflowMode = TextOverflowModes.Ellipsis;
        infoNameText.enableWordWrapping = false;
        KorFont(infoNameText);

        Spacer(content, 8);
        Divider(content);

        // ── 문화 정보 테이블 ──────────────────────────────
        Spacer(content, 8);
        InfoSectionLabel(content, "문화해설");

        _infoUsage    = InfoRow(content, "용도", "—");
        _infoPeriod   = InfoRow(content, "시대", "—");
        _infoMaterial = InfoRow(content, "재질", "—");
        _infoSource   = InfoRow(content, "출처", "—");

        Spacer(content, 4);
        Divider(content);

        // ══ B. Transform 편집 ═════════════════════════════

        Spacer(content, 8);
        InfoSectionLabel(content, "배치 편집");

        // 위치
        Spacer(content, 4);
        SubLabel(content, "위 치");
        posX = AxisInput(content, "X", COL_X);
        posY = AxisInput(content, "Y", COL_Y);
        posZ = AxisInput(content, "Z", COL_Z);
        posX.onEndEdit.AddListener(_ => ApplyPos());
        posY.onEndEdit.AddListener(_ => ApplyPos());
        posZ.onEndEdit.AddListener(_ => ApplyPos());

        // 회전
        Spacer(content, 6);
        SubLabel(content, "회 전");
        rotX = AxisInput(content, "X", COL_X);
        rotY = AxisInput(content, "Y", COL_Y);
        rotZ = AxisInput(content, "Z", COL_Z);
        rotX.onEndEdit.AddListener(_ => ApplyRot());
        rotY.onEndEdit.AddListener(_ => ApplyRot());
        rotZ.onEndEdit.AddListener(_ => ApplyRot());
        Spacer(content, 4);
        ActionRow(content, 28,
            ("-90", () => QuickRot(-90f), BTN_GHOST, TEXT_MAIN),
            ("+90", () => QuickRot( 90f), BTN_GHOST, TEXT_MAIN),
            ("초기화", ResetRot,           BTN_GHOST, TEXT_MAIN));

        // 크기
        Spacer(content, 6);
        SubLabel(content, "크 기");
        scaleF = AxisInput(content, "S", GOLD);
        scaleF.onEndEdit.AddListener(_ => ApplyScale());
        Spacer(content, 4);
        ActionRow(content, 28,
            (" 0.5× ", () => SetScale(12f),  BTN_GHOST, TEXT_MAIN),
            (" 1× ",   () => SetScale(23f),  NAVY,      Color.white),
            (" 2× ",   () => SetScale(46f),  BTN_GHOST, TEXT_MAIN),
            (" 3× ",   () => SetScale(70f),  BTN_GHOST, TEXT_MAIN));

        // ── 액션 버튼 ──────────────────────────────────────
        Spacer(content, 12);
        Divider(content);
        Spacer(content, 8);
        ActionRow(content, 40,
            ("복  제", Duplicate,      BTN_SEC,    Color.white),
            ("삭  제", DeleteSelected, BTN_DANGER, Color.white));
        Spacer(content, 6);
        ActionRow(content, 30,
            ("선택 해제", ClearSelection, BTN_GHOST, TEXT_MAIN));
        Spacer(content, 24);
    }

    // ── 섹션 라벨 ─────────────────────────────────────────
    void InfoSectionLabel(Transform parent, string kor)
    {
        var go = new GameObject("SecLbl");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 26; le.flexibleWidth = 1;
        go.AddComponent<Image>().color = BG_ROOT;
        var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0); tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = kor; t.fontSize = 10; t.fontStyle = FontStyles.Bold;
        t.color = NAVY; t.alignment = TextAlignmentOptions.Left;
        KorFont(t);
    }
}
