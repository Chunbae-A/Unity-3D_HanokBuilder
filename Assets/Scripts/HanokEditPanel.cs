using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — 오른쪽 문화해설 카드 + Transform 편집 (partial)
/// </summary>
public partial class HanokUIManager
{
    // 문화 정보 필드 텍스트 참조
    TMP_Text _infoUsage, _infoPeriod, _infoMaterial, _infoSource;

    void BuildEditPanel(Transform content)
    {
        // ══ A. 선택 부재 정보 카드 ════════════════════════

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
            (" 0.5× ", () => SetScale(0.5f), BTN_GHOST, TEXT_MAIN),
            (" 1× ",   () => SetScale(1f),   NAVY,      Color.white),
            (" 2× ",   () => SetScale(2f),   BTN_GHOST, TEXT_MAIN),
            (" 3× ",   () => SetScale(3f),   BTN_GHOST, TEXT_MAIN));

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

    void SubLabel(Transform parent, string kor)
    {
        var go = new GameObject("Sub");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 20; le.flexibleWidth = 1;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = kor; t.fontSize = 9; t.fontStyle = FontStyles.Bold;
        t.color = TEXT_SUB; t.alignment = TextAlignmentOptions.Left;
        // 마진을 위한 RectTransform 오프셋
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        KorFont(t);
    }

    // ── 문화 정보 행 (라벨 + 값) ──────────────────────────
    TMP_Text InfoRow(Transform parent, string labelKor, string defaultVal)
    {
        var row = new GameObject("InfoRow_" + labelKor);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 24; le.flexibleWidth = 1;
        row.AddComponent<Image>().color = Color.clear;

        // 라벨 (좌측 고정)
        var lgo = new GameObject("Lbl"); lgo.transform.SetParent(row.transform, false);
        var lRT = lgo.AddComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(0.3f, 1);
        lRT.offsetMin = new Vector2(12, 0); lRT.offsetMax = Vector2.zero;
        var lt = lgo.AddComponent<TextMeshProUGUI>();
        lt.text = labelKor; lt.fontSize = 10;
        lt.color = TEXT_SUB; lt.alignment = TextAlignmentOptions.Left;
        KorFont(lt);

        // 값 (우측)
        var vgo = new GameObject("Val"); vgo.transform.SetParent(row.transform, false);
        var vRT = vgo.AddComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0.3f, 0); vRT.anchorMax = new Vector2(1, 1);
        vRT.offsetMin = new Vector2(4, 0); vRT.offsetMax = new Vector2(-12, 0);
        var vt = vgo.AddComponent<TextMeshProUGUI>();
        vt.text = defaultVal; vt.fontSize = 10;
        vt.color = TEXT_MAIN; vt.alignment = TextAlignmentOptions.Left;
        vt.overflowMode = TextOverflowModes.Ellipsis;
        vt.enableWordWrapping = false;
        KorFont(vt);

        return vt;
    }

    // ── 축 입력 행 ────────────────────────────────────────
    TMP_InputField AxisInput(Transform parent, string axisLabel, Color axisCol)
    {
        var row = new GameObject("Row_" + axisLabel);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 26; le.flexibleWidth = 1;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;

        var lgo = new GameObject("Lbl"); lgo.transform.SetParent(row.transform, false);
        lgo.AddComponent<LayoutElement>().preferredWidth = 22;
        var lImg = lgo.AddComponent<Image>();
        lImg.color = new Color(axisCol.r, axisCol.g, axisCol.b, 0.15f);
        var lt = new GameObject("T"); lt.transform.SetParent(lgo.transform, false);
        var ltRT = lt.AddComponent<RectTransform>();
        ltRT.anchorMin = Vector2.zero; ltRT.anchorMax = Vector2.one;
        ltRT.offsetMin = ltRT.offsetMax = Vector2.zero;
        var ltT = lt.AddComponent<TextMeshProUGUI>();
        ltT.text = axisLabel; ltT.fontSize = 10; ltT.fontStyle = FontStyles.Bold;
        ltT.color = axisCol; ltT.alignment = TextAlignmentOptions.Center;
        LatFont(ltT);

        var f = MakeInputField(row.transform);
        f.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        return f;
    }

    // ── 액션 버튼 행 ──────────────────────────────────────
    void ActionRow(Transform parent, float height,
        params (string lbl, System.Action action, Color bg, Color fg)[] btns)
    {
        var row = new GameObject("ActRow");
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height; le.flexibleWidth = 1;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = true;

        foreach (var (lbl, action, bg, fg) in btns)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(row.transform, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>(); img.color = bg;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = BORDER; outline.effectDistance = new Vector2(1,-1);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cs = btn.colors;
            cs.highlightedColor = new Color(
                Mathf.Min(bg.r+.06f,1), Mathf.Min(bg.g+.06f,1), Mathf.Min(bg.b+.06f,1));
            cs.pressedColor = bg * 0.85f;
            btn.colors = cs;
            btn.onClick.AddListener(() => action?.Invoke());

            var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
            var tRT = tgo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
            var t = tgo.AddComponent<TextMeshProUGUI>();
            t.text = lbl; t.fontSize = 11;
            t.alignment = TextAlignmentOptions.Center; t.color = fg;
            if (HasKorean(lbl)) KorFont(t); else LatFont(t);
        }
    }
}
