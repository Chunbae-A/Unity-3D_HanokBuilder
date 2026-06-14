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

    // ── 정보 행 (라벨 + 값 텍스트) ───────────────────────
    TMP_Text InfoRow(Transform parent, string label, string value)
    {
        var row = new GameObject("InfoRow_" + label);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>(); le.preferredHeight = 22; le.flexibleWidth = 1;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(12, 12, 2, 2);
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        var lgo = new GameObject("L"); lgo.transform.SetParent(row.transform, false);
        lgo.AddComponent<LayoutElement>().preferredWidth = 38;
        var lt = lgo.AddComponent<TextMeshProUGUI>();
        lt.text = label; lt.fontSize = 9; lt.color = TEXT_HINT;
        lt.alignment = TextAlignmentOptions.Left; KorFont(lt);

        var vgo = new GameObject("V"); vgo.transform.SetParent(row.transform, false);
        var vt = vgo.AddComponent<TextMeshProUGUI>();
        vt.text = value; vt.fontSize = 9; vt.color = TEXT_MAIN;
        vt.alignment = TextAlignmentOptions.Left; KorFont(vt);
        return vt;
    }

    // ── 소제목 ───────────────────────────────────────────
    void SubLabel(Transform parent, string text)
    {
        var go = new GameObject("SubLbl");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>(); le.preferredHeight = 20; le.flexibleWidth = 1;
        var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0); tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = 9; t.color = TEXT_SUB; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Left; KorFont(t);
    }

    // ── 축 입력 필드 (X/Y/Z/S) ───────────────────────────
    TMP_InputField AxisInput(Transform parent, string axis, Color axisColor)
    {
        var row = new GameObject("Axis_" + axis);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>(); le.preferredHeight = 26; le.flexibleWidth = 1;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(12, 12, 2, 2);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        var lgo = new GameObject("AxisLbl"); lgo.transform.SetParent(row.transform, false);
        lgo.AddComponent<LayoutElement>().preferredWidth = 16;
        var lt = lgo.AddComponent<TextMeshProUGUI>();
        lt.text = axis; lt.fontSize = 10; lt.color = axisColor; lt.fontStyle = FontStyles.Bold;
        lt.alignment = TextAlignmentOptions.Center; LatFont(lt);

        var fgo = new GameObject("Field"); fgo.transform.SetParent(row.transform, false);
        fgo.AddComponent<LayoutElement>().flexibleWidth = 1;
        var fImg = fgo.AddComponent<Image>(); fImg.color = BG_INPUT;

        var area = new GameObject("Area"); area.transform.SetParent(fgo.transform, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
        aRT.offsetMin = new Vector2(6, 2); aRT.offsetMax = new Vector2(-6, -2);
        area.AddComponent<RectMask2D>();

        var textGO = new GameObject("Text"); textGO.transform.SetParent(area.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 10; text.color = TEXT_MAIN;
        text.alignment = TextAlignmentOptions.Left; LatFont(text);

        var phGO = new GameObject("Ph"); phGO.transform.SetParent(area.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text = "0"; ph.fontSize = 10; ph.color = TEXT_HINT;
        ph.alignment = TextAlignmentOptions.Left; LatFont(ph);

        var field = fgo.AddComponent<TMP_InputField>();
        field.targetGraphic = fImg;
        field.textViewport = aRT;
        field.textComponent = text;
        field.placeholder = ph;
        field.contentType = TMP_InputField.ContentType.DecimalNumber;
        field.caretColor = NAVY;
        field.selectionColor = new Color(NAVY.r, NAVY.g, NAVY.b, 0.25f);
        return field;
    }

    // ── 버튼 행 (1~3개 버튼) ─────────────────────────────
    void ActionRow(Transform parent, float height,
        params (string label, System.Action onClick, Color bg, Color textCol)[] buttons)
    {
        var row = new GameObject("ActionRow");
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>(); le.preferredHeight = height; le.flexibleWidth = 1;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4; hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        foreach (var (label, onClick, bg, textCol) in buttons)
        {
            var go = new GameObject("Btn"); go.transform.SetParent(row.transform, false);
            var img = go.AddComponent<Image>(); img.color = bg;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var cs = btn.colors;
            cs.highlightedColor = new Color(bg.r * 0.88f, bg.g * 0.88f, bg.b * 0.88f, 1f);
            cs.pressedColor     = new Color(bg.r * 0.75f, bg.g * 0.75f, bg.b * 0.75f, 1f);
            btn.colors = cs;
            var cb = onClick; btn.onClick.AddListener(() => cb?.Invoke());

            var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
            var tRT = tgo.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
            var t = tgo.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 9; t.color = textCol;
            t.alignment = TextAlignmentOptions.Center;
            if (HasKorean(label)) KorFont(t); else LatFont(t);
        }
    }

    // ── 정보 패널 갱신 ───────────────────────────────────
    void RefreshInfoPanel()
    {
        if (infoNameText == null) return;
        if (selectedObject == null)
        {
            infoNameText.text  = "부재를 선택하세요";
            infoNameText.color = TEXT_HINT;
            if (_infoUsage    != null) _infoUsage.text    = "—";
            if (_infoPeriod   != null) _infoPeriod.text   = "—";
            if (_infoMaterial != null) _infoMaterial.text = "—";
            if (_infoSource   != null) _infoSource.text   = "—";
        }
        else
        {
            infoNameText.text  = selectedObject.name;
            infoNameText.color = TEXT_MAIN;
        }
    }

    // ── Transform 입력 동기화 (매 프레임, 비포커스 필드만) ─
    void SyncTransformInputs()
    {
        if (selectedObject == null) return;
        var pos = selectedObject.transform.position;
        var rot = selectedObject.transform.eulerAngles;
        float s = selectedObject.transform.localScale.x;
        if (posX  != null && !posX.isFocused)   posX.text  = pos.x.ToString("F2");
        if (posY  != null && !posY.isFocused)    posY.text  = pos.y.ToString("F2");
        if (posZ  != null && !posZ.isFocused)    posZ.text  = pos.z.ToString("F2");
        if (rotX  != null && !rotX.isFocused)    rotX.text  = rot.x.ToString("F1");
        if (rotY  != null && !rotY.isFocused)    rotY.text  = rot.y.ToString("F1");
        if (rotZ  != null && !rotZ.isFocused)    rotZ.text  = rot.z.ToString("F1");
        if (scaleF != null && !scaleF.isFocused) scaleF.text = s.ToString("F2");
    }

    void ForceSyncTransform()
    {
        if (selectedObject == null) return;
        var pos = selectedObject.transform.position;
        var rot = selectedObject.transform.eulerAngles;
        float s = selectedObject.transform.localScale.x;
        if (posX  != null) posX.text  = pos.x.ToString("F2");
        if (posY  != null) posY.text  = pos.y.ToString("F2");
        if (posZ  != null) posZ.text  = pos.z.ToString("F2");
        if (rotX  != null) rotX.text  = rot.x.ToString("F1");
        if (rotY  != null) rotY.text  = rot.y.ToString("F1");
        if (rotZ  != null) rotZ.text  = rot.z.ToString("F1");
        if (scaleF != null) scaleF.text = s.ToString("F2");
    }

    // ── Transform 적용 ───────────────────────────────────
    void ApplyPos()
    {
        if (selectedObject == null) return;
        float.TryParse(posX?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float x);
        float.TryParse(posY?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float y);
        float.TryParse(posZ?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float z);
        PushUndoMove(selectedObject);
        selectedObject.transform.position = new Vector3(x, y, z);
    }

    void ApplyRot()
    {
        if (selectedObject == null) return;
        float.TryParse(rotX?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float x);
        float.TryParse(rotY?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float y);
        float.TryParse(rotZ?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float z);
        selectedObject.transform.rotation = Quaternion.Euler(x, y, z);
    }

    void ApplyScale()
    {
        if (selectedObject == null) return;
        float.TryParse(scaleF?.text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float s);
        if (s <= 0f) s = 1f;
        selectedObject.transform.localScale = Vector3.one * s;
    }

    void QuickRot(float deg)
    {
        if (selectedObject == null) return;
        selectedObject.transform.Rotate(Vector3.up, deg, Space.World);
        ForceSyncTransform();
    }

    void ResetRot()
    {
        if (selectedObject == null) return;
        selectedObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        ForceSyncTransform();
    }

    void SetScale(float s)
    {
        if (selectedObject == null) return;
        selectedObject.transform.localScale = Vector3.one * s;
        if (scaleF != null) scaleF.text = s.ToString("F2");
    }

    void Duplicate()
    {
        if (selectedObject == null) return;
        var copy = Instantiate(selectedObject,
            selectedObject.transform.position + new Vector3(2f, 0f, 0f),
            selectedObject.transform.rotation);
        copy.name = selectedObject.name;
        copy.transform.localScale = selectedObject.transform.localScale;
        AttachSelectable(copy);
        PushUndoSpawn(copy);
        SelectObject(copy);
    }
}
