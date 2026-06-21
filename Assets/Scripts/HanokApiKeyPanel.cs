using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — API 키 설정 패널 (partial)
/// PlayerPrefs에 키 저장, 앱 시작 시 키 없으면 자동 표시
/// </summary>
public partial class HanokUIManager
{
    const string PREFS_API_KEY  = "HanokClaudeApiKey";
    const string DEFAULT_MODEL  = "claude-haiku-4-5-20251001";

    GameObject     _apiKeyPanel;
    TMP_InputField _apiKeyInput;

    // ── 키 조회 (PlayerPrefs 우선, 없으면 에셋 fallback) ──────────
    string GetSavedApiKey()
    {
        string key = PlayerPrefs.GetString(PREFS_API_KEY, "");
        if (!string.IsNullOrEmpty(key)) return key;
        var config = Resources.Load<ClaudeApiConfig>("ClaudeApiConfig");
        return config?.apiKey ?? "";
    }

    string GetApiModel()
    {
        var config = Resources.Load<ClaudeApiConfig>("ClaudeApiConfig");
        return string.IsNullOrEmpty(config?.model) ? DEFAULT_MODEL : config.model;
    }

    // ── 시작 시 키 없으면 자동 팝업 ──────────────────────────────
    internal void CheckApiKeyOnStart()
    {
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(PREFS_API_KEY, "")))
            ShowApiKeyPanel();
    }

    // ── 열기 / 닫기 / 저장 ────────────────────────────────────────
    internal void ShowApiKeyPanel()
    {
        if (_apiKeyPanel == null) BuildApiKeyPanel();
        _apiKeyPanel.SetActive(true);
        _apiKeyInput.text = PlayerPrefs.GetString(PREFS_API_KEY, "");
        _apiKeyInput.ActivateInputField();
    }

    void HideApiKeyPanel()
    {
        if (_apiKeyPanel != null) _apiKeyPanel.SetActive(false);
    }

    void SaveApiKey()
    {
        string key = _apiKeyInput.text.Trim();
        if (string.IsNullOrEmpty(key)) return;
        PlayerPrefs.SetString(PREFS_API_KEY, key);
        PlayerPrefs.Save();
        HideApiKeyPanel();
        ShowToast("API 키가 저장됐습니다.");
    }

    // ── 패널 UI 빌드 ──────────────────────────────────────────────
    void BuildApiKeyPanel()
    {
        var canvas = _canvasRT.transform;

        // 전체화면 반투명 오버레이 (클릭 시 닫기)
        var overlay = NewRT(canvas, "ApiKeyOverlay");
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
        var overlayBtn = overlay.gameObject.AddComponent<Button>();
        overlayBtn.targetGraphic = overlayImg;
        overlayBtn.onClick.AddListener(HideApiKeyPanel);

        // 중앙 카드 (Image가 오버레이 클릭을 가로막음 — 별도 blocker 불필요)
        var card = NewRT(overlay, "ApiKeyCard");
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(360, 220);
        var cardImg = card.GetComponent<Image>();
        cardImg.sprite = RoundedRectSprite(18f);
        cardImg.type   = Image.Type.Sliced;
        cardImg.color  = BG_PANEL;
        cardImg.material = GlassMaterial();
        AddInnerGlow(card, 18f);
        AddOuterBorder(card, 18f);

        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding            = new RectOffset(28, 28, 22, 22);
        vlg.spacing            = 12;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment     = TextAnchor.UpperCenter;

        // 타이틀
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(card, false);
        titleGO.AddComponent<LayoutElement>().preferredHeight = 26;
        var titleT = titleGO.AddComponent<TextMeshProUGUI>();
        titleT.text = "Claude API 키 설정";
        titleT.fontSize = 14; titleT.color = TEXT_MAIN;
        titleT.fontStyle = FontStyles.Bold;
        titleT.alignment = TextAlignmentOptions.Center;
        KorFont(titleT); AddTextHalo(titleT);

        // 설명
        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(card, false);
        descGO.AddComponent<LayoutElement>().preferredHeight = 30;
        var descT = descGO.AddComponent<TextMeshProUGUI>();
        descT.text = "console.anthropic.com에서 발급한 API 키를 입력하세요.\n키는 이 기기에 저장되며 AI 기능 사용 시 Anthropic 서버로 전송됩니다.";
        descT.fontSize = 9; descT.color = TEXT_SUB;
        descT.alignment = TextAlignmentOptions.Center;
        descT.textWrappingMode = TextWrappingModes.Normal;
        KorFont(descT);

        // 입력창
        var inputGO = new GameObject("ApiInput");
        inputGO.transform.SetParent(card, false);
        inputGO.AddComponent<LayoutElement>().preferredHeight = 38;
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.sprite  = RoundedRectSprite(8f);
        inputImg.type    = Image.Type.Sliced;
        inputImg.color   = BG_INPUT;
        inputImg.material = GlassMaterial();

        var area = new GameObject("Area");
        area.transform.SetParent(inputGO.transform, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
        aRT.offsetMin = new Vector2(12, 4); aRT.offsetMax = new Vector2(-12, -4);
        area.AddComponent<RectMask2D>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(area.transform, false);
        var tRT = textGO.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        var textT = textGO.AddComponent<TextMeshProUGUI>();
        textT.fontSize = 10; textT.color = TEXT_MAIN;
        textT.alignment = TextAlignmentOptions.MidlineLeft;
        LatFont(textT);

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(area.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var phT = phGO.AddComponent<TextMeshProUGUI>();
        phT.text = "sk-ant-...";
        phT.fontSize = 10; phT.color = TEXT_HINT;
        phT.alignment = TextAlignmentOptions.MidlineLeft;
        LatFont(phT);

        _apiKeyInput = inputGO.AddComponent<TMP_InputField>();
        _apiKeyInput.targetGraphic = inputImg;
        _apiKeyInput.textViewport  = aRT;
        _apiKeyInput.textComponent = textT;
        _apiKeyInput.placeholder   = phT;
        _apiKeyInput.contentType   = TMP_InputField.ContentType.Password;
        _apiKeyInput.lineType      = TMP_InputField.LineType.SingleLine;
        _apiKeyInput.caretColor    = TEXT_MAIN;
        _apiKeyInput.onSubmit.AddListener(_ => SaveApiKey());

        // 버튼 행
        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(card, false);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 40;
        var rowHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 10;
        rowHLG.childForceExpandWidth  = true;
        rowHLG.childForceExpandHeight = true;

        MakeDialogBtn(btnRow.transform, "취소", BTN_GHOST,  TEXT_MAIN,       bold: false, HideApiKeyPanel);
        MakeDialogBtn(btnRow.transform, "저장", BTN_ACTIVE, TEXT_ON_ACCENT,  bold: true,  SaveApiKey);

        _apiKeyPanel = overlay.gameObject;
    }

    // ── 종료 확인 패널 ────────────────────────────────────────────
    GameObject _quitPanel;

    void ShowQuitPanel()
    {
        if (_quitPanel == null) BuildQuitPanel();
        _quitPanel.SetActive(true);
    }

    void HideQuitPanel()
    {
        if (_quitPanel != null) _quitPanel.SetActive(false);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void BuildQuitPanel()
    {
        var canvas = _canvasRT.transform;

        var overlay = NewRT(canvas, "QuitOverlay");
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        overlay.SetAsLastSibling();
        var overlayImg = overlay.GetComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);

        var card = NewRT(overlay, "QuitCard");
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(300, 150);
        var cardImg = card.GetComponent<Image>();
        cardImg.sprite = RoundedRectSprite(18f);
        cardImg.type   = Image.Type.Sliced;
        cardImg.color  = BG_PANEL;
        cardImg.material = GlassMaterial();
        AddInnerGlow(card, 18f);
        AddOuterBorder(card, 18f);

        var vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding            = new RectOffset(28, 28, 28, 24);
        vlg.spacing            = 20;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment     = TextAnchor.UpperCenter;

        var msgGO = new GameObject("Msg");
        msgGO.transform.SetParent(card, false);
        msgGO.AddComponent<LayoutElement>().preferredHeight = 30;
        var msgT = msgGO.AddComponent<TextMeshProUGUI>();
        msgT.text = "게임을 종료하시겠습니까?";
        msgT.fontSize = 14; msgT.color = TEXT_MAIN;
        msgT.fontStyle = FontStyles.Bold;
        msgT.alignment = TextAlignmentOptions.Center;
        KorFont(msgT); AddTextHalo(msgT);

        var btnRow = new GameObject("BtnRow");
        btnRow.transform.SetParent(card, false);
        btnRow.AddComponent<LayoutElement>().preferredHeight = 40;
        var rowHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 10;
        rowHLG.childForceExpandWidth  = true;
        rowHLG.childForceExpandHeight = true;

        MakeDialogBtn(btnRow.transform, "아니오", BTN_GHOST,  TEXT_MAIN,      bold: false, HideQuitPanel);
        MakeDialogBtn(btnRow.transform, "네",     BTN_ACTIVE, TEXT_ON_ACCENT, bold: true,  QuitGame);

        _quitPanel = overlay.gameObject;
    }

    void MakeDialogBtn(Transform parent, string label, Color bg, Color fg, bool bold, UnityEngine.Events.UnityAction action)
    {
        var go  = new GameObject(label);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = RoundedRectSprite(8f);
        img.type   = Image.Type.Sliced;
        img.color  = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        if (bg == BTN_ACTIVE)
        {
            var cs = btn.colors;
            cs.highlightedColor = BTN_ACTIVE_HOVER;
            cs.pressedColor     = BTN_ACTIVE_PRESS;
            btn.colors = cs;
        }
        btn.onClick.AddListener(action);
        var lbl = (TextMeshProUGUI)MakeLabel(go.transform, label, 11, fg, bold);
        var rt  = lbl.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
