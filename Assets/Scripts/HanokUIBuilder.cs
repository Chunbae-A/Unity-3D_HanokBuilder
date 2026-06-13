using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — Canvas·3패널·뷰포트툴바 생성 (partial)
/// </summary>
public partial class HanokUIManager
{
    void BuildUI()
    {
        EnsureEventSystem();

        // Canvas
        var cv = new GameObject("HanokCanvas").AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var cvs = cv.gameObject.AddComponent<CanvasScaler>();
        cvs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cvs.referenceResolution = new Vector2(1280, 720);
        cvs.matchWidthOrHeight  = 0.5f;
        cv.gameObject.AddComponent<GraphicRaycaster>();
        var root = cv.transform;
        _canvasRT = cv.GetComponent<RectTransform>();

        // ── 왼쪽 패널 (280px) ────────────────────────────
        var leftRT = NewRT(root, "Left");
        leftRT.anchorMin = new Vector2(0, 0); leftRT.anchorMax = new Vector2(0, 1);
        leftRT.pivot = new Vector2(0, 0.5f);
        leftRT.offsetMin = Vector2.zero; leftRT.offsetMax = new Vector2(280, 0);
        StylePanel(leftRT.gameObject);
        BuildLeftHeader(leftRT);
        var lScroll = MakeScroll(leftRT, 104);
        assetContent = lScroll.transform.Find("Viewport/Content");
        leftPanelRT = leftRT;

        // ── 오른쪽 패널 (280px, 부재 정보 + Transform 편집) ──
        var rightRT = NewRT(root, "Right");
        rightRT.anchorMin = new Vector2(1, 0); rightRT.anchorMax = new Vector2(1, 1);
        rightRT.pivot = new Vector2(1, 0.5f);
        rightRT.offsetMin = new Vector2(-280, 0); rightRT.offsetMax = Vector2.zero;
        StylePanel(rightRT.gameObject);
        BuildRightHeader(rightRT);
        var rScroll = MakeScroll(rightRT, 56);
        BuildEditPanel(rScroll.transform.Find("Viewport/Content"));

        // ── 가운데 뷰포트 툴바 + 배경 선택 + 스케일 핸들 ──────
        BuildViewportToolbar(root);
        BuildBackgroundSelector(root);
        BuildViewOrientationBadge(root);
        BuildScaleHandle(root);
        BuildCaptureFlash(root);
        BuildToast(root);
        BuildAIPromptWidget(root);
        BuildLeftExpandButton(root);
    }

    // ── 왼쪽 헤더 (제목 + 검색창) ────────────────────────
    void BuildLeftHeader(RectTransform panel)
    {
        // 제목 바
        var hdr = NewRT(panel, "Hdr");
        hdr.anchorMin = new Vector2(0, 1); hdr.anchorMax = new Vector2(1, 1);
        hdr.pivot = new Vector2(0.5f, 1);
        hdr.offsetMin = new Vector2(0, -56); hdr.offsetMax = Vector2.zero;
        hdr.GetComponent<Image>().color = NAVY;

        var title = MakeLabel(hdr, "모듈 라이브러리", 13, Color.white, bold: true);
        var tRT = title.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(16, 0); tRT.offsetMax = new Vector2(-44, 0);
        title.alignment = TextAlignmentOptions.Left;

        // 패널 접기 버튼 — 반투명 원형 배경 + 흰색 화살표(◀)
        var foldRT = NewRT(hdr, "FoldBtn");
        foldRT.anchorMin = new Vector2(1, 0.5f); foldRT.anchorMax = new Vector2(1, 0.5f);
        foldRT.pivot = new Vector2(1, 0.5f);
        foldRT.offsetMin = new Vector2(-40, -14); foldRT.offsetMax = new Vector2(-12, 14);

        var foldImg = foldRT.GetComponent<Image>();
        foldImg.sprite = AICircleSprite();
        foldImg.type = Image.Type.Simple;
        foldImg.color = new Color(1, 1, 1, 0.12f);

        var foldBtn = foldRT.gameObject.AddComponent<Button>();
        foldBtn.targetGraphic = foldImg;
        var fcs = foldBtn.colors;
        fcs.normalColor = new Color(1, 1, 1, 0.12f);
        fcs.highlightedColor = new Color(1, 1, 1, 0.24f);
        fcs.pressedColor = new Color(1, 1, 1, 0.36f);
        foldBtn.colors = fcs;
        foldBtn.onClick.AddListener(() => SetLeftPanelVisible(false));

        // 화살표 아이콘 — 180도 회전 → ◀
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(foldRT, false);
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin = arrowRT.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRT.sizeDelta = new Vector2(11, 11);
        arrowRT.localEulerAngles = new Vector3(0, 0, 180);
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = AITriangleSprite();
        arrowImg.type = Image.Type.Simple;
        arrowImg.color = new Color(1, 1, 1, 0.92f);
        arrowImg.raycastTarget = false;

        // 검색창 — 실제 입력 가능한 TMP_InputField로 구성
        var searchBar = NewRT(panel, "SearchBar");
        searchBar.anchorMin = new Vector2(0, 1); searchBar.anchorMax = new Vector2(1, 1);
        searchBar.pivot = new Vector2(0.5f, 1);
        searchBar.offsetMin = new Vector2(10, -100); searchBar.offsetMax = new Vector2(-10, -60);
        var sbImg = searchBar.GetComponent<Image>();
        sbImg.color = BG_INPUT;
        AddRoundOutline(searchBar, BORDER);

        var area = new GameObject("Area");
        area.transform.SetParent(searchBar, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
        aRT.offsetMin = new Vector2(12, 2); aRT.offsetMax = new Vector2(-8, -2);
        area.AddComponent<RectMask2D>();

        var sTextGO = new GameObject("Text");
        sTextGO.transform.SetParent(area.transform, false);
        var sTextRT = sTextGO.AddComponent<RectTransform>();
        sTextRT.anchorMin = Vector2.zero; sTextRT.anchorMax = Vector2.one;
        sTextRT.offsetMin = sTextRT.offsetMax = Vector2.zero;
        var sText = sTextGO.AddComponent<TextMeshProUGUI>();
        sText.fontSize = 11; sText.color = TEXT_MAIN;
        sText.alignment = TextAlignmentOptions.Left;
        KorFont(sText);

        var sPhGO = new GameObject("Placeholder");
        sPhGO.transform.SetParent(area.transform, false);
        var sPhRT = sPhGO.AddComponent<RectTransform>();
        sPhRT.anchorMin = Vector2.zero; sPhRT.anchorMax = Vector2.one;
        sPhRT.offsetMin = sPhRT.offsetMax = Vector2.zero;
        var sPh = sPhGO.AddComponent<TextMeshProUGUI>();
        sPh.text = "검색..."; sPh.fontSize = 11; sPh.color = TEXT_HINT;
        sPh.alignment = TextAlignmentOptions.Left;
        KorFont(sPh);

        searchInput = searchBar.gameObject.AddComponent<TMP_InputField>();
        searchInput.targetGraphic = sbImg;
        searchInput.textViewport  = aRT;
        searchInput.textComponent = sText;
        searchInput.placeholder   = sPh;
        searchInput.contentType   = TMP_InputField.ContentType.Standard;
        searchInput.caretColor    = NAVY;
        searchInput.selectionColor = new Color(NAVY.r, NAVY.g, NAVY.b, 0.25f);
        searchInput.onValueChanged.AddListener(OnSearchChanged);
    }

    // ── 오른쪽 헤더 ───────────────────────────────────────
    void BuildRightHeader(RectTransform panel)
    {
        var hdr = NewRT(panel, "Hdr");
        hdr.anchorMin = new Vector2(0, 1); hdr.anchorMax = new Vector2(1, 1);
        hdr.pivot = new Vector2(0.5f, 1);
        hdr.offsetMin = new Vector2(0, -56); hdr.offsetMax = Vector2.zero;
        hdr.GetComponent<Image>().color = NAVY;

        var t = MakeLabel(hdr, "부재 정보", 13, Color.white, bold: true);
        var tRT = t.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(16, 0); tRT.offsetMax = new Vector2(-12, 0);
        t.alignment = TextAlignmentOptions.Left;
    }

    // ── 뷰포트 툴바 (가운데 왼쪽 플로팅) ─────────────────
    void BuildViewportToolbar(Transform root)
    {
        var panel = NewRT(root, "Toolbar");
        panel.anchorMin = new Vector2(0, 0.5f); panel.anchorMax = new Vector2(0, 0.5f);
        panel.pivot     = new Vector2(0, 0.5f);
        // 3툴×56 + 3btn×40 + 4div + spacing14 + pad10 = 318px
        panel.offsetMin = new Vector2(288, -159);
        panel.offsetMax = new Vector2(346,  159);
        var panImg = panel.gameObject.GetComponent<Image>();
        panImg.color = new Color(0.10f, 0.12f, 0.16f, 0.90f);
        var panOutline = panel.gameObject.AddComponent<Outline>();
        panOutline.effectColor    = new Color(1f, 1f, 1f, 0.08f);
        panOutline.effectDistance = new Vector2(1, -1);

        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2; vlg.padding = new RectOffset(4, 4, 5, 5);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // ── 도구 버튼 (이동 / 회전 / 크기 / 삭제) ───────
        var tools = new (string lbl, string key, EditTool tool)[]
        {
            ("이동", "1", EditTool.Select),
            ("회전", "2", EditTool.Rotate),
            ("크기", "3", EditTool.Scale),
            ("삭제", "4", EditTool.Delete),
        };

        toolBtns = new Button[tools.Length];
        for (int i = 0; i < tools.Length; i++)
        {
            var (lbl, key, tool) = tools[i];
            bool isDel = (tool == EditTool.Delete);

            var go = new GameObject("Tool_" + tool);
            go.transform.SetParent(panel, false);
            go.AddComponent<LayoutElement>().preferredHeight = 56;

            var btnImg = go.AddComponent<Image>();
            btnImg.color = (i == 0) ? new Color(1f, 1f, 1f, 0.15f) : Color.clear;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var cs = btn.colors;
            cs.normalColor      = btnImg.color;
            cs.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
            cs.pressedColor     = isDel ? new Color(0.80f, 0.15f, 0.12f, 0.90f)
                                        : new Color(0.11f, 0.23f, 0.42f, 0.90f);
            btn.colors = cs;
            btn.onClick.AddListener(() => SetTool(tool));
            toolBtns[i] = btn;

            Color iconCol = (i == 0) ? Color.white
                          : isDel    ? new Color(1f, 0.50f, 0.46f)
                          :            new Color(1f, 1f, 1f, 0.55f);
            var ic = MakeLabel(go.transform, lbl, 12f, iconCol, bold: true);
            var iRT = ic.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.44f); iRT.anchorMax = Vector2.one;
            iRT.offsetMin = iRT.offsetMax = Vector2.zero;
            ic.alignment = TextAlignmentOptions.Center;
            KorFont(ic);

            var hintCol = (i == 0) ? new Color(1f, 1f, 1f, 0.40f) : new Color(1f, 1f, 1f, 0.22f);
            var hint = MakeLabel(go.transform, key, 8f, hintCol);
            var hRT = hint.GetComponent<RectTransform>();
            hRT.anchorMin = Vector2.zero; hRT.anchorMax = new Vector2(1f, 0.46f);
            hRT.offsetMin = hRT.offsetMax = Vector2.zero;
            hint.alignment = TextAlignmentOptions.Center;
            LatFont(hint);
        }

        // ── 구분선 ───────────────────────────────────────
        ToolbarDivider(panel);

        // ── 뷰 초기화 버튼 ───────────────────────────────
        AddToolbarSmallBtn(panel, "초기화", "H",
            new Color(1f, 1f, 1f, 0.50f), Color.clear,
            new Color(1f, 1f, 1f, 0.18f), new Color(1f, 1f, 1f, 0.32f),
            () => Camera.main?.GetComponent<HanokCameraController>()?.ResetView(),
            out _);

        ToolbarDivider(panel);

        // ── 캡처 버튼 ────────────────────────────────────
        AddToolbarSmallBtn(panel, "캡처", "P",
            new Color(0.55f, 0.82f, 1.00f, 0.70f), Color.clear,
            new Color(1f, 1f, 1f, 0.18f), new Color(0.15f, 0.55f, 0.90f, 0.60f),
            () => TriggerCapture(),
            out _);
    }

    void ToolbarDivider(Transform parent)
    {
        var d = new GameObject("Div"); d.transform.SetParent(parent, false);
        d.AddComponent<LayoutElement>().preferredHeight = 1;
        d.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);
    }

    void AddToolbarSmallBtn(Transform parent,
        string label, string keyHint,
        Color labelCol, Color normalBg, Color hoverBg, Color pressBg,
        System.Action onClick, out Image bgImg)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 40;
        bgImg = go.AddComponent<Image>(); bgImg.color = normalBg;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = bgImg;
        var cs = btn.colors;
        cs.normalColor = normalBg; cs.highlightedColor = hoverBg; cs.pressedColor = pressBg;
        btn.colors = cs;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var ic = MakeLabel(go.transform, label, 9.5f, labelCol, bold: true);
        var iRT = ic.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0, 0.44f); iRT.anchorMax = Vector2.one;
        iRT.offsetMin = iRT.offsetMax = Vector2.zero;
        ic.alignment  = TextAlignmentOptions.Center;
        if (HasKorean(label)) KorFont(ic); else LatFont(ic);

        var kh = MakeLabel(go.transform, keyHint, 7.5f, new Color(1f, 1f, 1f, 0.22f));
        var kRT = kh.GetComponent<RectTransform>();
        kRT.anchorMin = Vector2.zero; kRT.anchorMax = new Vector2(1f, 0.46f);
        kRT.offsetMin = kRT.offsetMax = Vector2.zero;
        kh.alignment  = TextAlignmentOptions.Center;
        LatFont(kh);
    }

    // ── 배경 선택기 (뷰포트 상단 중앙) ──────────────────
    void BuildBackgroundSelector(Transform root)
    {
        var bar = NewRT(root, "BgSelector");
        bar.anchorMin = new Vector2(0.5f, 1f);
        bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot     = new Vector2(0.5f, 1f);
        // 화면 가운데 상단에 고정 — 4버튼 행, 높이 68px
        bar.offsetMin = new Vector2(-171, -72);
        bar.offsetMax = new Vector2( 171,  -4);
        bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.96f);
        AddRoundOutline(bar, BORDER);

        var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 3; hlg.padding = new RectOffset(3, 3, 3, 3);
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        // 이름 / 설명 / 분위기 색
        var presets = new (string name, string desc, string atmo)[]
        {
            ("한옥 마당", "청명한 낮",   "#8BBCD4"),
            ("사랑채",   "온화한 빛",   "#C4A050"),
            ("조선 장터", "흐린 하늘",   "#8A9FB0"),
            ("전통 정원", "초록 향기",   "#5CA87A"),
        };

        _bgBtns = new Button[presets.Length];
        for (int i = 0; i < presets.Length; i++)
        {
            int ci = i;
            var (name, desc, atmo) = presets[i];
            Color atmoCol; ColorUtility.TryParseHtmlString(atmo, out atmoCol);

            var go = new GameObject("BG_" + i);
            go.transform.SetParent(bar, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = (i == 0) ? NAVY : BTN_GHOST;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cs = btn.colors;
            cs.highlightedColor = Hex("#D0CCBF"); cs.pressedColor = NAVY;
            btn.colors = cs;
            btn.onClick.AddListener(() => SelectBgPreset(ci));
            _bgBtns[i] = btn;

            // 상단 분위기 색 띠 (항상 고정 표시)
            var strip = new GameObject("Atmo"); strip.transform.SetParent(go.transform, false);
            var sRT = strip.AddComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0.82f); sRT.anchorMax = Vector2.one;
            sRT.offsetMin = sRT.offsetMax = Vector2.zero;
            strip.AddComponent<Image>().color = new Color(atmoCol.r, atmoCol.g, atmoCol.b, 0.85f);

            // 이름 (중간)
            var t = MakeLabel(go.transform, name, 10f,
                (i == 0) ? Color.white : TEXT_MAIN, bold: true);
            var tRT = t.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 0.36f); tRT.anchorMax = new Vector2(1, 0.82f);
            tRT.offsetMin = tRT.offsetMax = Vector2.zero;
            t.alignment = TextAlignmentOptions.Center;
            KorFont(t);

            // 설명 (하단)
            var d = MakeLabel(go.transform, desc, 7.5f,
                (i == 0) ? new Color(1, 1, 1, 0.65f) : TEXT_HINT);
            var dRT = d.GetComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero; dRT.anchorMax = new Vector2(1, 0.40f);
            dRT.offsetMin = new Vector2(2, 2); dRT.offsetMax = new Vector2(-2, 0);
            d.alignment = TextAlignmentOptions.Center;
            KorFont(d);
        }
    }

    void BuildViewOrientationBadge(Transform root)
    {
        var badge = NewRT(root, "ViewBadge");
        badge.anchorMin = new Vector2(1f, 1f);
        badge.anchorMax = new Vector2(1f, 1f);
        badge.pivot     = new Vector2(1f, 1f);
        badge.offsetMin = new Vector2(-348, -36);
        badge.offsetMax = new Vector2(-290,  -6);
        badge.gameObject.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.16f, 0.82f);
        var ol = badge.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.10f); ol.effectDistance = new Vector2(1, -1);
        var tgo = new GameObject("T"); tgo.transform.SetParent(badge, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(5, 2); tRT.offsetMax = new Vector2(-5, -2);
        _viewBadgeText = tgo.AddComponent<TextMeshProUGUI>();
        _viewBadgeText.text = "3D"; _viewBadgeText.fontSize = 9.5f;
        _viewBadgeText.fontStyle = FontStyles.Bold;
        _viewBadgeText.color = new Color(1f, 1f, 1f, 0.65f);
        _viewBadgeText.alignment = TextAlignmentOptions.Center;
        KorFont(_viewBadgeText);
    }

    void BuildCaptureFlash(Transform root)
    {
        var flash = NewRT(root, "CaptureFlash");
        flash.anchorMin = Vector2.zero; flash.anchorMax = Vector2.one;
        flash.offsetMin = flash.offsetMax = Vector2.zero;
        var img = flash.gameObject.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0f);
        img.raycastTarget = false;
        _captureFlash = flash.gameObject;
        _captureFlash.SetActive(false);
    }

    void BuildToast(Transform root)
    {
        var toast = NewRT(root, "Toast");
        toast.anchorMin = new Vector2(0.5f, 0.5f);
        toast.anchorMax = new Vector2(0.5f, 0.5f);
        toast.pivot     = new Vector2(0.5f, 0.5f);
        toast.offsetMin = new Vector2(-210, -17);
        toast.offsetMax = new Vector2( 210,  17);
        var toastImg = toast.gameObject.GetComponent<Image>();
        toastImg.color = new Color(0.08f, 0.08f, 0.12f, 0f);
        toastImg.raycastTarget = false;
        var ol = toast.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.12f); ol.effectDistance = new Vector2(1, -1);
        var tgo = new GameObject("T"); tgo.transform.SetParent(toast, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(14, 3); tRT.offsetMax = new Vector2(-14, -3);
        _toastText = tgo.AddComponent<TextMeshProUGUI>();
        _toastText.text = ""; _toastText.fontSize = 9.5f;
        _toastText.color = new Color(1f, 1f, 1f, 0f);
        _toastText.alignment = TextAlignmentOptions.Center;
        _toastText.overflowMode = TextOverflowModes.Ellipsis;
        _toastText.enableWordWrapping = false;
        _toastText.raycastTarget = false;
        KorFont(_toastText);
        _toastGO = toast.gameObject;
        _toastGO.SetActive(false);
    }

    // ── 스크롤뷰 ─────────────────────────────────────────
    GameObject MakeScroll(RectTransform panel, float topOffset)
    {
        var go = new GameObject("Scroll");
        go.transform.SetParent(panel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = new Vector2(0, -topOffset);
        go.AddComponent<Image>().color = Color.clear;
        var sr = go.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.scrollSensitivity = 30f;

        var vp = new GameObject("Viewport");
        vp.transform.SetParent(go.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        // RectMask2D — Image+Mask 조합의 스텐실 버퍼 클리핑 문제 방지
        vp.AddComponent<RectMask2D>();

        var ct = new GameObject("Content");
        ct.transform.SetParent(vp.transform, false);
        var cRT = ct.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var vlg = ct.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 0; vlg.padding = new RectOffset(0, 0, 0, 12);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        ct.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT; sr.content = cRT;
        return go;
    }

    // ── 가로 스크롤뷰 (AI 추천 결과 한 줄) ────────────────
    GameObject MakeHorizontalScroll(RectTransform panel)
    {
        var go = new GameObject("HScroll");
        go.transform.SetParent(panel, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = Color.clear;
        var sr = go.AddComponent<ScrollRect>();
        sr.horizontal = true; sr.vertical = false; sr.scrollSensitivity = 30f;

        var vp = new GameObject("Viewport");
        vp.transform.SetParent(go.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<RectMask2D>();

        var ct = new GameObject("Content");
        ct.transform.SetParent(vp.transform, false);
        var cRT = ct.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 1);
        cRT.pivot = new Vector2(0, 0.5f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var hlg = ct.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        ct.AddComponent<ContentSizeFitter>().horizontalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT; sr.content = cRT;
        return go;
    }

    // ── 숫자 입력 필드 ────────────────────────────────────
    TMP_InputField MakeInputField(Transform parent)
    {
        var go = new GameObject("IF");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = BG_INPUT;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = BORDER;
        outline.effectDistance = new Vector2(1, -1);

        var area = new GameObject("A");
        area.transform.SetParent(go.transform, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
        aRT.offsetMin = new Vector2(6, 1); aRT.offsetMax = new Vector2(-6, -1);
        area.AddComponent<RectMask2D>();

        var tgo = new GameObject("T");
        tgo.transform.SetParent(area.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.fontSize = 11; t.color = TEXT_MAIN;
        LatFont(t);

        var pgo = new GameObject("P");
        pgo.transform.SetParent(area.transform, false);
        var pRT = pgo.AddComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;
        var ph = pgo.AddComponent<TextMeshProUGUI>();
        ph.text = "0.00"; ph.fontSize = 11; ph.color = TEXT_HINT;
        LatFont(ph);

        var f = go.AddComponent<TMP_InputField>();
        f.targetGraphic  = img;
        f.textViewport   = aRT;
        f.textComponent  = t;
        f.placeholder    = ph;
        f.contentType    = TMP_InputField.ContentType.DecimalNumber;
        f.caretColor     = NAVY;
        f.selectionColor = new Color(NAVY.r, NAVY.g, NAVY.b, 0.25f);
        return f;
    }

    // ── 공통 헬퍼 ─────────────────────────────────────────
    RectTransform NewRT(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = Color.clear;
        return go.GetComponent<RectTransform>(); // Image가 RectTransform을 이미 추가함
    }

    void StylePanel(GameObject go)
    {
        go.GetComponent<Image>().color = BG_PANEL;
        // 오른쪽/왼쪽 그림자 효과 (outline으로 대체)
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.08f);
        outline.effectDistance = new Vector2(2, -2);
    }

    void AddRoundOutline(RectTransform rt, Color col)
    {
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = col;
        outline.effectDistance = new Vector2(1, -1);
    }

    TMP_Text MakeLabel(Transform parent, string text, float size,
        Color col, bool bold = false)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        if (HasKorean(text)) KorFont(t); else LatFont(t);
        return t;
    }

    void Spacer(Transform p, float h)
    {
        var go = new GameObject("Sp");
        go.transform.SetParent(p, false);
        go.AddComponent<LayoutElement>().preferredHeight = h;
    }

    void Divider(Transform p, Color? col = null)
    {
        var go = new GameObject("Hr");
        go.transform.SetParent(p, false);
        go.AddComponent<LayoutElement>().preferredHeight = 1;
        go.AddComponent<Image>().color = col ?? BORDER;
    }

    Transform RowBox(Transform p, string name, float h, Color? bg = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = h; le.flexibleWidth = 1;
        go.AddComponent<Image>().color = bg ?? BG_CARD;
        return go.transform;
    }

    // ── 뷰포트 스케일 핸들 (선택 오브젝트 위에 플로팅) ────
    void BuildScaleHandle(Transform root)
    {
        var go = new GameObject("ScaleHandle");
        go.transform.SetParent(root, false);
        _scaleHandleGO = go;

        _scaleHandleRT = go.AddComponent<RectTransform>();
        _scaleHandleRT.anchorMin = new Vector2(0.5f, 0.5f);
        _scaleHandleRT.anchorMax = new Vector2(0.5f, 0.5f);
        _scaleHandleRT.pivot     = new Vector2(0.5f, 0.0f);
        _scaleHandleRT.sizeDelta = new Vector2(82f, 26f);

        _scaleHandleImg = go.AddComponent<Image>();
        _scaleHandleImg.color = new Color(0.10f, 0.62f, 0.92f, 0.95f);
        var ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(1f, 1f, 1f, 0.30f);
        ol.effectDistance = new Vector2(1f, -1f);

        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(5f, 3f); tRT.offsetMax = new Vector2(-5f, -3f);
        _scaleHandleText = tgo.AddComponent<TextMeshProUGUI>();
        _scaleHandleText.text      = "↔  1.0×";
        _scaleHandleText.fontSize  = 9f;
        _scaleHandleText.color     = Color.white;
        _scaleHandleText.alignment = TextAlignmentOptions.Center;
        LatFont(_scaleHandleText);

        go.SetActive(false);
    }

    // ── 좌측 패널 표시/숨김 ───────────────────────────────
    void SetLeftPanelVisible(bool visible)
    {
        leftPanelRT.gameObject.SetActive(visible);
        _leftExpandBtnRT.gameObject.SetActive(!visible);
    }

    // ── 좌측 패널 펼치기 버튼 (화면 좌상단, 패널이 접혔을 때만 노출) ──
    void BuildLeftExpandButton(Transform root)
    {
        var btnRT = NewRT(root, "LeftExpandBtn");
        btnRT.anchorMin = new Vector2(0, 1); btnRT.anchorMax = new Vector2(0, 1);
        btnRT.pivot = new Vector2(0, 1);
        btnRT.offsetMin = new Vector2(8, -36); btnRT.offsetMax = new Vector2(36, -8);

        var img = btnRT.GetComponent<Image>();
        img.sprite = AICircleSprite();
        img.type = Image.Type.Simple;
        img.color = new Color(NAVY.r, NAVY.g, NAVY.b, 0.92f);

        var btn = btnRT.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.highlightedColor = NAVY_LIGHT;
        cs.pressedColor = Hex("#0F2547");
        btn.colors = cs;
        btn.onClick.AddListener(() => SetLeftPanelVisible(true));

        // 화살표 아이콘 — 우측(▶), "펼치기"
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(btnRT, false);
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin = arrowRT.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRT.sizeDelta = new Vector2(11, 11);
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = AITriangleSprite();
        arrowImg.type = Image.Type.Simple;
        arrowImg.color = new Color(1, 1, 1, 0.92f);
        arrowImg.raycastTarget = false;

        _leftExpandBtnRT = btnRT;
        btnRT.gameObject.SetActive(false);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        // New Input System(activeInputHandler=1) 환경에서 StandaloneInputModule은 동작 안 함
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}
