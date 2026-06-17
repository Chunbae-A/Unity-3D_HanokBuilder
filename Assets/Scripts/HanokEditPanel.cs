using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// HanokUIManager — 오른쪽 배치 편집 패널 (partial)
/// </summary>
public partial class HanokUIManager
{
    float _positionStep = 0.1f;
    float _rotationStep = 5f;
    GameObject _activeStepDropdown;

    void BuildEditPanel(Transform content)
    {
        // ══ A. 선택 부재 정보 카드 ════════════════════════

        Spacer(content, 16);

        // 이름 카드
        var nameCard = RowBox(content, "NameCard", 52, BG_CARD);
        var nameCardImg = nameCard.GetComponent<Image>();
        nameCardImg.sprite = RoundedRectSprite(8f);
        nameCardImg.type = Image.Type.Sliced;
        nameCardImg.material = GlassMaterial();
        AddInnerGlow(nameCard.gameObject, 8f);

        var cap = new GameObject("Caption"); cap.transform.SetParent(nameCard, false);
        var capRT = cap.AddComponent<RectTransform>();
        capRT.anchorMin = new Vector2(0, 0.56f); capRT.anchorMax = Vector2.one;
        capRT.offsetMin = new Vector2(14, 0); capRT.offsetMax = new Vector2(-8, -4);
        var capText = cap.AddComponent<TextMeshProUGUI>();
        capText.text = "현재 선택";
        capText.fontSize = 9;
        capText.color = TEXT_SUB;
        capText.alignment = TextAlignmentOptions.Left;
        KorFont(capText);

        var nt = new GameObject("NameT"); nt.transform.SetParent(nameCard, false);
        var ntRT = nt.AddComponent<RectTransform>();
        ntRT.anchorMin = Vector2.zero; ntRT.anchorMax = new Vector2(1, 0.58f);
        ntRT.offsetMin = new Vector2(14, 5); ntRT.offsetMax = new Vector2(-8, 0);
        infoNameText = nt.AddComponent<TextMeshProUGUI>();
        infoNameText.text = "부재를 선택하세요";
        infoNameText.fontSize = 12.5f;
        infoNameText.fontStyle = FontStyles.Bold;
        infoNameText.color = TEXT_HINT;
        infoNameText.alignment = TextAlignmentOptions.Left;
        infoNameText.overflowMode = TextOverflowModes.Ellipsis;
        infoNameText.textWrappingMode = TextWrappingModes.NoWrap;
        KorFont(infoNameText);
        AddTextHalo(infoNameText);

        Spacer(content, 10);
        Divider(content);

        // ── 문화 정보 테이블 ──────────────────────────────
        Spacer(content, 10);
        InfoSectionLabel(content, "문화해설");

        _infoUsage    = InfoRow(content, "용도", "—");
        _infoPeriod   = InfoRow(content, "시대", "—");
        _infoMaterial = InfoRow(content, "재질", "—");
        _infoSource   = InfoRow(content, "출처", "—");

        Spacer(content, 8);
        Divider(content);

        // ══ B. Transform 편집 ═════════════════════════════

        Spacer(content, 10);
        InfoSectionLabel(content, "배치 편집");

        // 위치
        Spacer(content, 6);
        SubLabel(content, "위 치");
        posX = AxisInput(content, "X", COL_X);
        posY = AxisInput(content, "Y", COL_Y);
        posZ = AxisInput(content, "Z", COL_Z);
        posX.onEndEdit.AddListener(_ => ApplyPos());
        posY.onEndEdit.AddListener(_ => ApplyPos());
        posZ.onEndEdit.AddListener(_ => ApplyPos());

        // 회전
        Spacer(content, 10);
        SubLabel(content, "회 전");
        rotX = AxisInput(content, "X", COL_X);
        rotY = AxisInput(content, "Y", COL_Y);
        rotZ = AxisInput(content, "Z", COL_Z);
        rotX.onEndEdit.AddListener(_ => ApplyRot());
        rotY.onEndEdit.AddListener(_ => ApplyRot());
        rotZ.onEndEdit.AddListener(_ => ApplyRot());
        Spacer(content, 6);
        ActionRow(content, 28,
            ("-90", () => QuickRot(-90f), BTN_GHOST, TEXT_MAIN),
            ("+90", () => QuickRot( 90f), BTN_GHOST, TEXT_MAIN),
            ("초기화", ResetRot,           BTN_GHOST, TEXT_MAIN));

        // 크기
        Spacer(content, 10);
        SubLabel(content, "크 기");
        scaleF = AxisInput(content, "S", TEXT_SUB);
        scaleF.onEndEdit.AddListener(_ => ApplyScale());
        Spacer(content, 6);
        ActionRow(content, 28,
            (" 0.5× ", () => SetScale(0.5f), BTN_GHOST,  TEXT_MAIN),
            (" 1× ",   () => SetScale(1f),   BTN_ACTIVE, TEXT_ON_ACCENT),
            (" 2× ",   () => SetScale(2f),   BTN_GHOST,  TEXT_MAIN),
            (" 3× ",   () => SetScale(3f),   BTN_GHOST,  TEXT_MAIN));

        // ── 액션 버튼 ──────────────────────────────────────
        Spacer(content, 16);
        Divider(content);
        Spacer(content, 10);
        ActionRow(content, 40,
            ("복  제", Duplicate,      BTN_GHOST, TEXT_MAIN),
            ("삭  제", DeleteSelected, BTN_GHOST, TEXT_MAIN));
        Spacer(content, 10);
        ActionRow(content, 30,
            ("선택 해제", ClearSelection, BTN_GHOST, TEXT_MAIN));
        Spacer(content, 28);
    }

    // ── 섹션 라벨 ─────────────────────────────────────────
    void InfoSectionLabel(Transform parent, string kor)
    {
        var go = new GameObject("SecLbl");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 26; le.flexibleWidth = 1;
        go.AddComponent<Image>().color = Color.clear;
        var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0); tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = kor; t.fontSize = 10; t.fontStyle = FontStyles.Bold;
        t.color = TEXT_MAIN; t.alignment = TextAlignmentOptions.Left;
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
        KorFont(t);
    }

    void MiniHint(Transform parent, string text)
    {
        var row = new GameObject("InfoRow_" + labelKor);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 24; le.flexibleWidth = 1;
        row.AddComponent<Image>().color = BG_CARD;

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
        vt.textWrappingMode = TextWrappingModes.NoWrap;
        KorFont(vt);

        return vt;
    }

    // ── 축 입력 행 ────────────────────────────────────────
    void TransformStepperGroup(Transform parent, string title, bool rotation,
        out TMP_InputField x, out TMP_InputField y, out TMP_InputField z)
    {
        var card = RowBox(parent, "Transform_" + title, 116, Hex("#FBFAF7"));
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = BORDER;
        outline.effectDistance = new Vector2(1, -1);

        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(10, 10, 7, 7);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var header = new GameObject("Header");
        header.transform.SetParent(card, false);
        var headerLE = header.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 28;
        var headerLG = header.AddComponent<HorizontalLayoutGroup>();
        headerLG.spacing = 6;
        headerLG.childForceExpandWidth = false;
        headerLG.childForceExpandHeight = true;
        headerLG.childAlignment = TextAnchor.MiddleCenter;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(header.transform, false);
        titleGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = title + "\n<size=7.5><color=#777777>" +
                         (rotation ? "Rotation / Degrees" : "Position / World") +
                         "</color></size>";
        titleText.richText = true;
        titleText.fontSize = 10.5f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = NAVY;
        titleText.alignment = TextAlignmentOptions.Left;
        KorFont(titleText);

        if (rotation)
        {
            HeaderMiniButton(header.transform, "-90", () => QuickRot(-90f), 30f);
            HeaderMiniButton(header.transform, "+90", () => QuickRot( 90f), 30f);
            HeaderMiniButton(header.transform, "초기화", ResetRot, 44f);
        }

        StepDropdown(header.transform, rotation);

        var row = new GameObject("Axes");
        row.transform.SetParent(card, false);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 66;
        rowLE.flexibleWidth = 1;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        x = AxisStepperColumn(row.transform, "X", COL_X,
            () => StepAxis(rotation, 0, -1), () => StepAxis(rotation, 0, 1));
        y = AxisStepperColumn(row.transform, "Y", COL_Y,
            () => StepAxis(rotation, 1, -1), () => StepAxis(rotation, 1, 1));
        z = AxisStepperColumn(row.transform, "Z", COL_Z,
            () => StepAxis(rotation, 2, -1), () => StepAxis(rotation, 2, 1));
    }

    TMP_InputField AxisStepperColumn(Transform parent, string axisLabel, Color axisCol,
        System.Action minus, System.Action plus)
    {
        var col = new GameObject("Axis_" + axisLabel);
        col.transform.SetParent(parent, false);
        col.AddComponent<RectTransform>();
        var colLE = col.AddComponent<LayoutElement>();
        colLE.flexibleWidth = 1;

        var colImg = col.AddComponent<Image>();
        colImg.color = Hex("#FFFFFF");
        var colOutline = col.AddComponent<Outline>();
        colOutline.effectColor = Hex("#DDD8CF");
        colOutline.effectDistance = new Vector2(1, -1);

        var vlg = col.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var labelGO = new GameObject("Lbl_" + axisLabel);
        labelGO.transform.SetParent(col.transform, false);
        labelGO.AddComponent<LayoutElement>().preferredHeight = 14;
        var labelImg = labelGO.AddComponent<Image>();
        labelImg.color = new Color(axisCol.r, axisCol.g, axisCol.b, 0.10f);

        var labelTextGO = new GameObject("T");
        labelTextGO.transform.SetParent(labelGO.transform, false);
        var labelTextRT = labelTextGO.AddComponent<RectTransform>();
        labelTextRT.anchorMin = Vector2.zero;
        labelTextRT.anchorMax = Vector2.one;
        labelTextRT.offsetMin = labelTextRT.offsetMax = Vector2.zero;
        var labelText = labelTextGO.AddComponent<TextMeshProUGUI>();
        labelText.text = axisLabel;
        labelText.fontSize = 8.8f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = axisCol;
        labelText.alignment = TextAlignmentOptions.Center;
        LatFont(labelText);

        var inputRow = new GameObject("InputRow");
        inputRow.transform.SetParent(col.transform, false);
        inputRow.AddComponent<LayoutElement>().preferredHeight = 34;
        var inputLG = inputRow.AddComponent<HorizontalLayoutGroup>();
        inputLG.spacing = 3;
        inputLG.childForceExpandHeight = true;
        inputLG.childForceExpandWidth = false;

        var field = MakeInputField(inputRow.transform);
        var fieldLE = field.gameObject.AddComponent<LayoutElement>();
        fieldLE.flexibleWidth = 1;
        fieldLE.preferredWidth = 42;
        if (field.targetGraphic is Image fieldImg)
            fieldImg.color = Hex("#F6F3EC");
        field.textComponent.alignment = TextAlignmentOptions.Center;
        if (field.placeholder is TMP_Text ph)
            ph.alignment = TextAlignmentOptions.Center;
        StepButton(inputRow.transform, "-", minus);
        StepButton(inputRow.transform, "+", plus);
        return field;
    }

    void HeaderMiniButton(Transform parent, string label, System.Action action, float width)
    {
        var go = new GameObject("HeadBtn_" + label);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;

        var img = go.AddComponent<Image>();
        img.color = Hex("#EFEAE1");
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Hex("#D8D1C5");
        outline.effectDistance = new Vector2(1, -1);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor = img.color;
        cs.highlightedColor = Hex("#FFFFFF");
        cs.pressedColor = Hex("#DED6C9");
        btn.colors = cs;
        btn.onClick.AddListener(() => action?.Invoke());

        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(2, 0);
        tRT.offsetMax = new Vector2(-2, 0);
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = HasKorean(label) ? 8.3f : 8.8f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = TEXT_MAIN;
        if (HasKorean(label)) KorFont(t); else LatFont(t);
    }

    void StepDropdown(Transform parent, bool rotation)
    {
        var go = new GameObject(rotation ? "RotationStepDropdown" : "PositionStepDropdown");
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredWidth = rotation ? 58 : 68;

        var img = go.AddComponent<Image>();
        img.color = Hex("#FFFFFF");
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Hex("#D8D1C5");
        outline.effectDistance = new Vector2(1, -1);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor = img.color;
        cs.highlightedColor = Hex("#FFFDF8");
        cs.pressedColor = Hex("#E8E1D5");
        btn.colors = cs;

        var labelGO = new GameObject("T");
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(6, 0);
        labelRT.offsetMax = new Vector2(-14, 0);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = StepLabel(rotation);
        label.fontSize = 8.5f;
        label.fontStyle = FontStyles.Bold;
        label.color = TEXT_MAIN;
        label.alignment = TextAlignmentOptions.Center;
        KorFont(label);

        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0);
        arrowRT.anchorMax = Vector2.one;
        arrowRT.offsetMin = new Vector2(-13, 0);
        arrowRT.offsetMax = new Vector2(-3, 0);
        var arrow = arrowGO.AddComponent<TextMeshProUGUI>();
        arrow.text = "v";
        arrow.fontSize = 8f;
        arrow.fontStyle = FontStyles.Bold;
        arrow.color = TEXT_SUB;
        arrow.alignment = TextAlignmentOptions.Center;
        LatFont(arrow);

        btn.onClick.AddListener(() => ToggleStepDropdown(go.transform, label, rotation));
    }

    string StepLabel(bool rotation)
    {
        return rotation
            ? _rotationStep.ToString("0.#") + "도"
            : _positionStep.ToString("0.##") + " 단위";
    }

    void ToggleStepDropdown(Transform anchor, TMP_Text label, bool rotation)
    {
        if (_activeStepDropdown != null)
        {
            bool sameAnchor = _activeStepDropdown.transform.parent == anchor;
            Destroy(_activeStepDropdown);
            _activeStepDropdown = null;
            if (sameAnchor) return;
        }

        var options = rotation
            ? new (string text, float value)[] { ("1도", 1f), ("5도", 5f), ("15도", 15f), ("45도", 45f) }
            : new (string text, float value)[] { ("0.05 단위", 0.05f), ("0.1 단위", 0.1f), ("0.5 단위", 0.5f), ("1 단위", 1f) };

        var menu = new GameObject("StepDropdownMenu");
        menu.transform.SetParent(anchor, false);
        _activeStepDropdown = menu;

        var rt = menu.AddComponent<RectTransform>();
        var menuCanvas = menu.AddComponent<Canvas>();
        menuCanvas.overrideSorting = true;
        menuCanvas.sortingOrder = 50;
        menu.AddComponent<GraphicRaycaster>();

        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 1);
        float height = options.Length * 22f + 6f;
        rt.offsetMin = new Vector2(0, -height - 2f);
        rt.offsetMax = new Vector2(0, -2f);

        var img = menu.AddComponent<Image>();
        img.color = Hex("#FFFFFF");
        var outline = menu.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        outline.effectDistance = new Vector2(1, -1);

        var vlg = menu.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(3, 3, 3, 3);
        vlg.spacing = 2;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        foreach (var option in options)
        {
            var text = option.text;
            var value = option.value;
            StepDropdownOption(menu.transform, text, () =>
            {
                if (rotation) _rotationStep = value;
                else _positionStep = value;

                label.text = StepLabel(rotation);
                if (_activeStepDropdown != null)
                {
                    Destroy(_activeStepDropdown);
                    _activeStepDropdown = null;
                }
            });
        }

        menu.transform.SetAsLastSibling();
    }

    void StepDropdownOption(Transform parent, string label, System.Action action)
    {
        var go = new GameObject("Option_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 20;

        var img = go.AddComponent<Image>();
        img.color = Color.clear;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor = Color.clear;
        cs.highlightedColor = Hex("#F1EBDD");
        cs.pressedColor = Hex("#E4D9C8");
        btn.colors = cs;
        btn.onClick.AddListener(() => action?.Invoke());

        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(4, 0);
        tRT.offsetMax = new Vector2(-4, 0);
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 8.5f;
        t.color = TEXT_MAIN;
        t.alignment = TextAlignmentOptions.Center;
        KorFont(t);
    }

    void StepButton(Transform parent, string label, System.Action action)
    {
        var go = new GameObject("Step_" + label);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 17;

        var img = go.AddComponent<Image>();
        img.color = Hex("#ECE7DE");
        var outline = go.AddComponent<Outline>();
        outline.effectColor = Hex("#D2CCC1");
        outline.effectDistance = new Vector2(1, -1);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.normalColor = img.color;
        cs.highlightedColor = Hex("#FFFFFF");
        cs.pressedColor = Hex("#E7E1D4");
        btn.colors = cs;
        btn.onClick.AddListener(() => action?.Invoke());

        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 10.5f;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = TEXT_MAIN;
        LatFont(t);
    }

    void StepAxis(bool rotation, int axis, int direction)
    {
        if (!selectedObject) return;

        if (rotation)
        {
            var r = selectedObject.transform.eulerAngles;
            r[axis] += _rotationStep * direction;
            selectedObject.transform.eulerAngles = r;
        }
        else
        {
            var p = selectedObject.transform.position;
            p[axis] += _positionStep * direction;
            selectedObject.transform.position = p;
        }

        ForceSyncTransform();
        SyncGizmo();
    }

    TMP_InputField AxisInput(Transform parent, string axisLabel, Color axisCol)
    {
        var row = new GameObject("Row_" + axisLabel);
        row.transform.SetParent(parent, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 26; le.flexibleWidth = 1;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;

        var lgo = new GameObject("Lbl"); lgo.transform.SetParent(row.transform, false);
        lgo.AddComponent<LayoutElement>().preferredWidth = 22;
        var lImg = lgo.AddComponent<Image>();
        lImg.sprite = RoundedRectSprite(8f);
        lImg.type = Image.Type.Sliced;
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

    void RegisterEditPanelTabOrder(params TMP_InputField[] fields)
    {
        _editTabOrder = fields;
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (field == null) continue;

            var nav = field.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = fields[(i - 1 + fields.Length) % fields.Length];
            nav.selectOnRight = fields[(i + 1) % fields.Length];
            nav.selectOnUp = fields[(i - 1 + fields.Length) % fields.Length];
            nav.selectOnDown = fields[(i + 1) % fields.Length];
            field.navigation = nav;
        }
    }

    void HandleEditPanelTabNavigation()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.tabKey.wasPressedThisFrame || _editTabOrder == null)
            return;

        var selectedGO = EventSystem.current?.currentSelectedGameObject;
        var current = selectedGO != null ? selectedGO.GetComponent<TMP_InputField>() : null;
        int currentIndex = System.Array.IndexOf(_editTabOrder, current);
        if (currentIndex < 0) return;

        int direction = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed ? -1 : 1;
        int nextIndex = (currentIndex + direction + _editTabOrder.Length) % _editTabOrder.Length;
        var next = _editTabOrder[nextIndex];
        if (next == null) return;

        current.DeactivateInputField();
        EventSystem.current?.SetSelectedGameObject(next.gameObject);
        next.Select();
        next.ActivateInputField();
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
        hlg.spacing = 6; hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = true;

        foreach (var (lbl, action, bg, fg) in btns)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(row.transform, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = RoundedRectSprite(8f);
            img.type = Image.Type.Sliced;
            img.color = bg;
            if (bg == BTN_ACTIVE)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = GLOW;
                outline.effectDistance = new Vector2(1,-1);
            }
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cs = btn.colors;
            cs.highlightedColor = new Color(bg.r, bg.g, bg.b, Mathf.Min(bg.a + 0.18f, 1f));
            cs.pressedColor     = new Color(bg.r, bg.g, bg.b, Mathf.Min(bg.a + 0.35f, 1f));
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
