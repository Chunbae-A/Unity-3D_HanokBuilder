using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — AI 문화재 해설 (partial)
/// 건물 선택 시 건물 위에 말풍선 팝업으로 큐레이터 해설을 표시한다.
/// </summary>
public partial class HanokUIManager
{
    // ── 오른쪽 패널 버튼 필드 ─────────────────────────────
    Image    _guideRequestBtnImg;
    TMP_Text _guideStatusText;

    // ── 말풍선 팝업 필드 ──────────────────────────────────
    GameObject    _guideBubbleGO;
    RectTransform _guideBubbleRT;
    TMP_Text      _guideBubbleTitleText;
    TMP_Text      _guideBubbleBodyText;

    bool   _guideRequestInProgress;
    string _lastGuidedAssetKey;
    string _lastGuideTitle;
    string _lastGuideBody;

    static readonly Color GUIDE_BG    = new Color(0.07f, 0.09f, 0.16f, 0.96f);
    static readonly Color GUIDE_TITLE = new Color(1.00f, 0.82f, 0.38f, 1.00f);
    static readonly Color GUIDE_BODY  = new Color(0.93f, 0.93f, 0.93f, 0.92f);
    const float GUIDE_W = 300f;

    // ── 오른쪽 패널: 해설 생성 버튼 섹션 ─────────────────
    void BuildGuideSection(Transform content)
    {
        Spacer(content, 14);
        Divider(content);
        Spacer(content, 4);
        InfoSectionLabel(content, "AI 문화재 해설");
        Spacer(content, 6);

        var row = new GameObject("GuideRow");
        row.transform.SetParent(content, false);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 26; rowLE.flexibleWidth = 1;
        var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 8; rowHLG.padding = new RectOffset(12, 12, 0, 0);
        rowHLG.childForceExpandHeight = true; rowHLG.childForceExpandWidth = false;

        var stGO = new GameObject("GuideStatus");
        stGO.transform.SetParent(row.transform, false);
        stGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        _guideStatusText = stGO.AddComponent<TextMeshProUGUI>();
        _guideStatusText.text      = "선택 건축물의 역사·특징 해설";
        _guideStatusText.fontSize  = 8.5f;
        _guideStatusText.color     = TEXT_HINT;
        _guideStatusText.alignment = TextAlignmentOptions.Left;
        KorFont(_guideStatusText);

        var btnGO = new GameObject("GuideReqBtn");
        btnGO.transform.SetParent(row.transform, false);
        btnGO.AddComponent<LayoutElement>().preferredWidth = 64;
        _guideRequestBtnImg = btnGO.AddComponent<Image>();
        _guideRequestBtnImg.sprite = RoundedRectSprite(6f);
        _guideRequestBtnImg.type   = Image.Type.Sliced;
        _guideRequestBtnImg.color  = BTN_GHOST;
        var reqBtn = btnGO.AddComponent<Button>();
        reqBtn.targetGraphic = _guideRequestBtnImg;
        var btnCs = reqBtn.colors;
        btnCs.highlightedColor = BTN_HOVER; btnCs.pressedColor = BTN_PRESS;
        reqBtn.colors = btnCs;
        reqBtn.onClick.AddListener(OnGuideRequestClicked);
        var btnLbl = (TextMeshProUGUI)MakeLabel(btnGO.transform, "해설 생성", 8.5f, TEXT_MAIN);
        var btnRT  = btnLbl.GetComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero; btnRT.anchorMax = Vector2.one;
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        KorFont(btnLbl);

        Spacer(content, 24);
    }

    // ── 말풍선 팝업 빌드 (BuildUI에서 호출) ──────────────
    void BuildGuideBubble(Transform root)
    {
        var go = new GameObject("GuideBubble");
        go.transform.SetParent(root, false);
        _guideBubbleGO = go;

        _guideBubbleRT = go.AddComponent<RectTransform>();
        _guideBubbleRT.anchorMin = new Vector2(0.5f, 0.5f);
        _guideBubbleRT.anchorMax = new Vector2(0.5f, 0.5f);
        _guideBubbleRT.pivot     = new Vector2(0.5f, 0f);   // 하단 중앙이 앵커
        _guideBubbleRT.sizeDelta = new Vector2(GUIDE_W, 0f); // 높이는 CSF가 결정

        // 독립 소팅 레이어
        var cvs = go.AddComponent<Canvas>();
        cvs.overrideSorting = true;
        cvs.sortingOrder    = 25;
        go.AddComponent<GraphicRaycaster>();

        // 배경
        var bg = go.AddComponent<Image>();
        bg.sprite = RoundedRectSprite(14f);
        bg.type   = Image.Type.Sliced;
        bg.color  = GUIDE_BG;

        // 높이 자동 조절
        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding  = new RectOffset(16, 16, 14, 16);
        vlg.spacing  = 8;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // 드래그 가능
        go.AddComponent<UIDraggablePanel>();

        // ── 닫기 버튼 (레이아웃 무시, 우상단 오버레이) ────
        var closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(go.transform, false);
        closeGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var closeRT = closeGO.AddComponent<RectTransform>();
        closeRT.anchorMin        = new Vector2(1f, 1f);
        closeRT.anchorMax        = new Vector2(1f, 1f);
        closeRT.pivot            = new Vector2(1f, 1f);
        closeRT.sizeDelta        = new Vector2(22f, 22f);
        closeRT.anchoredPosition = new Vector2(-8f, -8f);
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(1, 1, 1, 0.10f);
        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        var closeCs = closeBtn.colors;
        closeCs.highlightedColor = new Color(1, 1, 1, 0.25f);
        closeBtn.colors = closeCs;
        closeBtn.onClick.AddListener(HideGuideBubble);
        var closeLbl = (TextMeshProUGUI)MakeLabel(closeGO.transform, "✕", 9f, new Color(1, 1, 1, 0.6f));
        var closeLblRT = closeLbl.GetComponent<RectTransform>();
        closeLblRT.anchorMin = Vector2.zero; closeLblRT.anchorMax = Vector2.one;
        closeLblRT.offsetMin = closeLblRT.offsetMax = Vector2.zero;

        // ── 제목 ──────────────────────────────────────────
        var titleGO = new GameObject("BubbleTitle");
        titleGO.transform.SetParent(go.transform, false);
        titleGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        titleGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _guideBubbleTitleText = titleGO.AddComponent<TextMeshProUGUI>();
        _guideBubbleTitleText.fontSize         = 11.5f;
        _guideBubbleTitleText.fontStyle        = FontStyles.Bold;
        _guideBubbleTitleText.color            = GUIDE_TITLE;
        _guideBubbleTitleText.alignment        = TextAlignmentOptions.Left;
        _guideBubbleTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        _guideBubbleTitleText.overflowMode     = TextOverflowModes.Ellipsis;
        KorFont(_guideBubbleTitleText);

        // ── 구분선 ────────────────────────────────────────
        var divGO = new GameObject("Div");
        divGO.transform.SetParent(go.transform, false);
        divGO.AddComponent<LayoutElement>().preferredHeight = 1;
        divGO.AddComponent<Image>().color = new Color(1f, 0.82f, 0.38f, 0.28f);

        // ── 본문 텍스트 ───────────────────────────────────
        var bodyGO = new GameObject("BubbleBody");
        bodyGO.transform.SetParent(go.transform, false);
        bodyGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        bodyGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _guideBubbleBodyText = bodyGO.AddComponent<TextMeshProUGUI>();
        _guideBubbleBodyText.fontSize         = 9.5f;
        _guideBubbleBodyText.lineSpacing      = 4f;
        _guideBubbleBodyText.color            = GUIDE_BODY;
        _guideBubbleBodyText.alignment        = TextAlignmentOptions.Left;
        _guideBubbleBodyText.textWrappingMode = TextWrappingModes.Normal;
        _guideBubbleBodyText.overflowMode     = TextOverflowModes.Overflow;
        KorFont(_guideBubbleBodyText);

        // ── 꼬리 삼각형 (레이아웃 무시, 하단 중앙) ────────
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(go.transform, false);
        arrowGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var arrowRT = arrowGO.AddComponent<RectTransform>();
        arrowRT.anchorMin        = new Vector2(0.5f, 0f);
        arrowRT.anchorMax        = new Vector2(0.5f, 0f);
        arrowRT.pivot            = new Vector2(0.5f, 1f);  // 꼭대기가 앵커 포인트
        arrowRT.sizeDelta        = new Vector2(18f, 11f);
        arrowRT.anchoredPosition = new Vector2(0f, 1f);    // 1px 겹침으로 이음새 제거
        arrowRT.localEulerAngles = new Vector3(0f, 0f, -90f); // ▼ 방향
        var arrowImg = arrowGO.AddComponent<Image>();
        arrowImg.sprite = AITriangleSprite();
        arrowImg.type   = Image.Type.Simple;
        arrowImg.color  = GUIDE_BG;

        go.SetActive(false);
    }

    // ── 표시·위치 조정·숨김 ──────────────────────────────
    void ShowGuideBubble(string title, string body)
    {
        if (_guideBubbleGO == null) return;
        if (_guideBubbleTitleText != null) _guideBubbleTitleText.text = title;
        if (_guideBubbleBodyText  != null) _guideBubbleBodyText.text  = body;
        _guideBubbleGO.SetActive(true);
        StartCoroutine(PositionBubbleAfterLayout());
    }

    IEnumerator PositionBubbleAfterLayout()
    {
        yield return null; // ContentSizeFitter가 높이를 계산할 때까지 한 프레임 대기
        LayoutRebuilder.ForceRebuildLayoutImmediate(_guideBubbleRT);
        PositionBubbleAboveBuilding();
    }

    void PositionBubbleAboveBuilding()
    {
        if (selectedObject == null || Camera.main == null) return;

        // 건물 바운드 상단 계산
        var rends = selectedObject.GetComponentsInChildren<Renderer>();
        Vector3 worldTop;
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            worldTop = new Vector3(b.center.x, b.max.y + 0.4f, b.center.z);
        }
        else worldTop = selectedObject.transform.position + Vector3.up * 3f;

        var sp = Camera.main.WorldToScreenPoint(worldTop);
        if (sp.z <= 0f) return; // 카메라 뒤

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, new Vector2(sp.x, sp.y), null, out Vector2 lp)) return;

        // 화면 밖으로 나가지 않도록 클램프
        float halfW  = GUIDE_W * 0.5f;
        float cw     = _canvasRT.rect.width  * 0.5f;
        float ch     = _canvasRT.rect.height * 0.5f;
        lp.x = Mathf.Clamp(lp.x, -cw + halfW + 20f, cw - halfW - 20f);
        lp.y = Mathf.Clamp(lp.y + 14f, -ch + 60f, ch - 120f);

        _guideBubbleRT.anchoredPosition = lp;
    }

    void HideGuideBubble()
    {
        if (_guideBubbleGO != null) _guideBubbleGO.SetActive(false);
    }

    // ── 이벤트 핸들러 ──────────────────────────────────────
    void OnGuideRequestClicked()
    {
        if (selectedObject == null)
        { SetGuideStatus("에셋을 먼저 선택해주세요."); return; }
        RequestGuideForObject(selectedObject, forceRefresh: true);
    }

    void TriggerAutoGuide(GameObject obj)
    {
        if (obj == null) { HideGuideBubble(); SetGuideStatus("선택 건축물의 역사·특징 해설"); return; }
        RequestGuideForObject(obj, forceRefresh: false);
    }

    void RequestGuideForObject(GameObject obj, bool forceRefresh)
    {
        if (_guideRequestInProgress) return;

        var meta = obj.GetComponent<HanokPlacedAssetMetadata>();
        string assetKey = !string.IsNullOrEmpty(meta?.assetKey)
            ? meta.assetKey : CleanPlacedObjectName(obj.name);

        if (!forceRefresh && assetKey == _lastGuidedAssetKey)
        {
            // 같은 에셋 재선택 — 이미 해설이 있으면 팝업만 다시 표시
            if (!string.IsNullOrEmpty(_lastGuideBody) &&
                _guideBubbleGO != null && !_guideBubbleGO.activeSelf)
                ShowGuideBubble(_lastGuideTitle, _lastGuideBody);
            return;
        }

        string displayName = !string.IsNullOrEmpty(meta?.displayName)
            ? meta.displayName : assetKey;

        string categoryLabel = "";
        if (_assetEntries != null)
        {
            string clean = CleanPlacedObjectName(obj.name);
            var entry = _assetEntries.Find(e =>
                (e.prefab != null && e.prefab.name == clean) || e.assetKey == assetKey);
            if (entry?.categories != null)
            {
                var cats = new List<string>();
                foreach (var c in entry.categories)
                    if (!string.IsNullOrEmpty(c.label)) cats.Add(c.label);
                if (cats.Count > 0) categoryLabel = string.Join(", ", cats);
            }
        }

        string apiKey = GetSavedApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _lastGuidedAssetKey = assetKey;
            SetGuideStatus("⚙ API 키 미설정");
            return;
        }

        _lastGuidedAssetKey = assetKey;
        StartCoroutine(RequestGuideCo(assetKey, displayName, categoryLabel));
    }

    IEnumerator RequestGuideCo(string assetKey, string displayName, string categoryLabel)
    {
        _guideRequestInProgress = true;
        SetGuideStatus("해설 생성 중...");
        if (_guideRequestBtnImg != null) _guideRequestBtnImg.color = BTN_ACTIVE;

        string system =
            "당신은 한국 전통 건축 문화재에 정통한 큐레이터이자 역사학자입니다. " +
            "수십 년간의 현장 연구를 통해 조선·고려·통일신라 시대의 건축 양식과 그 문화적 맥락을 깊이 이해하고 있습니다. " +
            "박물관 관람객이나 건축 학도에게 전문성과 생동감을 갖춰 해설하는 것이 당신의 소명입니다. " +
            "해설 원칙: 마크다운 기호(#, *, -, ** 등)를 일절 사용하지 않는다. " +
            "자연스러운 줄글 2~3 문단으로 작성한다. " +
            "첫 문단은 역사적 배경·시대적 맥락·건립 목적, " +
            "둘째 문단은 건축 구조·양식의 특징과 장인의 기술, " +
            "셋째 문단은 문화적·상징적 의미와 현재적 가치를 담는다. " +
            "각 문단은 3~4문장, 품격 있고 생동감 있는 문체로.";

        var userSb = new StringBuilder();
        userSb.Append("다음 건축물을 해설해 주십시오.\n\n건축물명: ").AppendLine(displayName);
        if (!string.IsNullOrEmpty(categoryLabel))
            userSb.Append("분류: ").AppendLine(categoryLabel);

        string bodyJson =
            "{\"model\":"    + JsonStr(GetApiModel()) +
            ",\"max_tokens\":900" +
            ",\"system\":"   + JsonStr(system) +
            ",\"messages\":[{\"role\":\"user\",\"content\":" + JsonStr(userSb.ToString()) + "}]}";

        byte[] raw = Encoding.UTF8.GetBytes(bodyJson);

        using var www = new UnityWebRequest("https://api.anthropic.com/v1/messages", "POST");
        www.uploadHandler   = new UploadHandlerRaw(raw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("content-type",      "application/json");
        www.SetRequestHeader("x-api-key",          GetSavedApiKey());
        www.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return www.SendWebRequest();

        _guideRequestInProgress = false;
        if (_guideRequestBtnImg != null) _guideRequestBtnImg.color = BTN_GHOST;

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GuideAPI] {www.responseCode}: {www.error}\n{www.downloadHandler.text}");
            SetGuideStatus($"요청 실패 ({www.responseCode})");
            yield break;
        }

        ClaudeResponse resp = null;
        try { resp = JsonUtility.FromJson<ClaudeResponse>(www.downloadHandler.text); }
        catch (System.Exception e)
        {
            Debug.LogError($"[GuideAPI] 파싱 실패: {e.Message}");
            SetGuideStatus("응답 파싱 실패");
            yield break;
        }

        if (resp?.content == null || resp.content.Length == 0)
        { SetGuideStatus("응답 없음"); yield break; }

        var meta2 = selectedObject?.GetComponent<HanokPlacedAssetMetadata>();
        string title = !string.IsNullOrEmpty(meta2?.displayName) ? meta2.displayName : assetKey;

        _lastGuideTitle = title;
        _lastGuideBody  = resp.content[0].text;

        SetGuideStatus("해설 완료 ✓");
        ShowGuideBubble(title, _lastGuideBody);
    }

    void SetGuideStatus(string msg)
    {
        if (_guideStatusText == null) return;
        _guideStatusText.text  = msg;
        _guideStatusText.color = TEXT_HINT;
    }

    void ClearGuide()
    {
        _lastGuidedAssetKey = null;
        _lastGuideTitle     = null;
        _lastGuideBody      = null;
        HideGuideBubble();
        SetGuideStatus("선택 건축물의 역사·특징 해설");
    }
}
