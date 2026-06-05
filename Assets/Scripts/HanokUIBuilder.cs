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

        // ── 왼쪽 패널 (280px) ────────────────────────────
        var leftRT = NewRT(root, "Left");
        leftRT.anchorMin = new Vector2(0, 0); leftRT.anchorMax = new Vector2(0, 1);
        leftRT.pivot = new Vector2(0, 0.5f);
        leftRT.offsetMin = Vector2.zero; leftRT.offsetMax = new Vector2(280, 0);
        StylePanel(leftRT.gameObject);
        BuildLeftHeader(leftRT);
        var lScroll = MakeScroll(leftRT, 104);
        assetContent = lScroll.transform.Find("Viewport/Content");

        // ── 오른쪽 패널 (280px) ───────────────────────────
        var rightRT = NewRT(root, "Right");
        rightRT.anchorMin = new Vector2(1, 0); rightRT.anchorMax = new Vector2(1, 1);
        rightRT.pivot = new Vector2(1, 0.5f);
        rightRT.offsetMin = new Vector2(-280, 0); rightRT.offsetMax = Vector2.zero;
        StylePanel(rightRT.gameObject);
        BuildRightHeader(rightRT);
        var rScroll = MakeScroll(rightRT, 56);
        BuildEditPanel(rScroll.transform.Find("Viewport/Content"));

        // ── 가운데 뷰포트 툴바 ────────────────────────────
        BuildViewportToolbar(root);
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
        tRT.offsetMin = new Vector2(16, 0); tRT.offsetMax = new Vector2(-12, 0);
        title.alignment = TextAlignmentOptions.Left;

        // 검색창
        var searchBar = NewRT(panel, "SearchBar");
        searchBar.anchorMin = new Vector2(0, 1); searchBar.anchorMax = new Vector2(1, 1);
        searchBar.pivot = new Vector2(0.5f, 1);
        searchBar.offsetMin = new Vector2(10, -100); searchBar.offsetMax = new Vector2(-10, -60);
        searchBar.GetComponent<Image>().color = BG_INPUT;
        AddRoundOutline(searchBar, BORDER);

        // 검색 플레이스홀더 — NewRT 대신 순수 TextMeshProUGUI 자식으로 생성
        // (NewRT 는 Image를 추가하므로 같은 GO에 TMP 추가 불가)
        var sText = new GameObject("SText");
        sText.transform.SetParent(searchBar, false);
        var sRT = sText.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 1);
        sRT.offsetMin = new Vector2(12, 2); sRT.offsetMax = new Vector2(-8, -2);
        var ph = sText.AddComponent<TextMeshProUGUI>();
        ph.text = "검색..."; ph.fontSize = 11; ph.color = TEXT_HINT;
        ph.alignment = TextAlignmentOptions.Left;
        KorFont(ph);
    }

    // ── 오른쪽 헤더 ───────────────────────────────────────
    void BuildRightHeader(RectTransform panel)
    {
        var hdr = NewRT(panel, "Hdr");
        hdr.anchorMin = new Vector2(0, 1); hdr.anchorMax = new Vector2(1, 1);
        hdr.pivot = new Vector2(0.5f, 1);
        hdr.offsetMin = new Vector2(0, -56); hdr.offsetMax = Vector2.zero;
        hdr.GetComponent<Image>().color = NAVY;

        var t = MakeLabel(hdr, "문화해설 카드", 13, Color.white, bold: true);
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
        panel.pivot = new Vector2(0, 0.5f);
        panel.offsetMin = new Vector2(288, -100); panel.offsetMax = new Vector2(332, 100);
        var img = panel.gameObject.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0.92f);
        AddRoundOutline(panel, BORDER);

        var vlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2; vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // 툴 정의 — 특수문자 대신 한글 라벨 (KorFont 사용)
        var tools = new (string icon, EditTool tool)[]
        {
            ("선택", EditTool.Select),
            ("이동", EditTool.Move),
            ("회전", EditTool.Rotate),
            ("삭제", EditTool.Delete),
        };

        toolBtns = new Button[tools.Length];
        for (int i = 0; i < tools.Length; i++)
        {
            int idx = i;
            var (icon, tool) = tools[i];
            var go = new GameObject("Tool_" + tool);
            go.transform.SetParent(panel, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40; le.flexibleWidth = 1;
            var btnImg = go.AddComponent<Image>();
            btnImg.color = (i == 0) ? NAVY : BTN_GHOST;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var cs = btn.colors;
            cs.highlightedColor = Hex("#D0CCBF"); cs.pressedColor = NAVY;
            btn.colors = cs;
            btn.onClick.AddListener(() => SetTool(tool));
            toolBtns[i] = btn;

            var lbl = MakeLabel(go.transform, icon, 10,
                (i == 0) ? Color.white : TEXT_SUB);
            var lRT = lbl.GetComponent<RectTransform>();
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = lRT.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
            KorFont(lbl); // 한글 라벨
        }

        // 구분선
        var divGO = new GameObject("Div");
        divGO.transform.SetParent(panel, false);
        divGO.AddComponent<LayoutElement>().preferredHeight = 1;
        divGO.AddComponent<Image>().color = BORDER;

        // 삭제 버튼 (선택 오브젝트)
        var delGO = new GameObject("DelBtn");
        delGO.transform.SetParent(panel, false);
        var delLE = delGO.AddComponent<LayoutElement>();
        delLE.preferredHeight = 40; delLE.flexibleWidth = 1;
        var delImg = delGO.AddComponent<Image>();
        delImg.color = BTN_GHOST;
        var delBtn = delGO.AddComponent<Button>();
        delBtn.targetGraphic = delImg;
        delBtn.onClick.AddListener(DeleteSelected);
        var delLbl = MakeLabel(delGO.transform, "지우기", 9, TEXT_SUB);
        var dlRT = delLbl.GetComponent<RectTransform>();
        dlRT.anchorMin = Vector2.zero; dlRT.anchorMax = Vector2.one;
        dlRT.offsetMin = dlRT.offsetMax = Vector2.zero;
        delLbl.alignment = TextAlignmentOptions.Center;
        KorFont(delLbl);
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

    // InputField 생성
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

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }
}
