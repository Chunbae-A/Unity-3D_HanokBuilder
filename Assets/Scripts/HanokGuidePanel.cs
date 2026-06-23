using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — AI 문화재 해설 (partial)
/// Expanded: 말풍선 전체 표시  /  Collapsed: 건물 위 작은 pill  /  Hidden: 완전 비활성
/// </summary>
public partial class HanokUIManager
{
    enum GuideState { Hidden, Collapsed, Expanded }

    // ── 필드 ──────────────────────────────────────────────
    GuideState    _guideState = GuideState.Hidden;
    GameObject    _guideBubbleGO;
    RectTransform _guideBubbleRT;
    TMP_Text      _guideBubbleTitleText;
    TMP_Text      _guideBubbleBodyText;

    GameObject    _guideIndicatorGO;   // 접힌 상태 pill
    RectTransform _guideIndicatorRT;
    TMP_Text      _guideIndicatorText;

    bool   _guideRequestInProgress;
    string _lastGuidedAssetKey;
    string _lastGuideTitle;
    string _lastGuideBody;
    float  _guideBubbleBaseScaleMag;

    static readonly Color GUIDE_BG    = new Color(0.07f, 0.09f, 0.16f, 0.96f);
    static readonly Color GUIDE_TITLE = new Color(1.00f, 0.82f, 0.38f, 1.00f);
    static readonly Color GUIDE_BODY  = new Color(0.93f, 0.93f, 0.93f, 0.92f);
    const float GUIDE_W = 300f;

    // ── UI 빌드 (BuildUI에서 호출) ────────────────────────
    void BuildGuideBubble(Transform root)
    {
        BuildFullBubble(root);
        BuildCollapseIndicator(root);
    }

    void BuildFullBubble(Transform root)
    {
        var go = new GameObject("GuideBubble");
        go.transform.SetParent(root, false);
        _guideBubbleGO = go;

        _guideBubbleRT = go.AddComponent<RectTransform>();
        _guideBubbleRT.anchorMin = new Vector2(0.5f, 0.5f);
        _guideBubbleRT.anchorMax = new Vector2(0.5f, 0.5f);
        _guideBubbleRT.pivot     = new Vector2(0.5f, 0f);
        _guideBubbleRT.sizeDelta = new Vector2(GUIDE_W, 0f);

        var cvs = go.AddComponent<Canvas>();
        cvs.overrideSorting = true; cvs.sortingOrder = 25;
        go.AddComponent<GraphicRaycaster>();

        var bg = go.AddComponent<Image>();
        bg.sprite = RoundedRectSprite(14f);
        bg.type = Image.Type.Sliced;
        bg.color = GUIDE_BG;

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        go.AddComponent<UIDraggablePanel>();

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding  = new RectOffset(14, 14, 12, 14);
        vlg.spacing  = 8;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // ── 헤더 행: 제목 + [−접기] [✕] ─────────────────
        var hdrGO = new GameObject("Header");
        hdrGO.transform.SetParent(go.transform, false);
        hdrGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        hdrGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var hdrHLG = hdrGO.AddComponent<HorizontalLayoutGroup>();
        hdrHLG.spacing = 4;
        hdrHLG.childForceExpandHeight = true;
        hdrHLG.childForceExpandWidth  = false;
        hdrHLG.childAlignment = TextAnchor.MiddleLeft;

        // 제목
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(hdrGO.transform, false);
        titleGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        titleGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _guideBubbleTitleText = titleGO.AddComponent<TextMeshProUGUI>();
        _guideBubbleTitleText.fontSize         = 11f;
        _guideBubbleTitleText.fontStyle        = FontStyles.Bold;
        _guideBubbleTitleText.color            = GUIDE_TITLE;
        _guideBubbleTitleText.alignment        = TextAlignmentOptions.Left;
        _guideBubbleTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        _guideBubbleTitleText.overflowMode     = TextOverflowModes.Ellipsis;
        KorFont(_guideBubbleTitleText);

        // [−] 접기 버튼
        MakeHeaderBtn(hdrGO.transform, "−", 22f, OnCollapseClicked);
        // [✕] 완전 닫기 버튼
        MakeHeaderBtn(hdrGO.transform, "×", 22f, OnGuideCloseClicked);

        // ── 구분선 ────────────────────────────────────────
        var divGO = new GameObject("Div");
        divGO.transform.SetParent(go.transform, false);
        divGO.AddComponent<LayoutElement>().preferredHeight = 1;
        divGO.AddComponent<Image>().color = new Color(1f, 0.82f, 0.38f, 0.28f);

        // ── 본문 ──────────────────────────────────────────
        var bodyGO = new GameObject("Body");
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

        // ── 꼬리 삼각형 ───────────────────────────────────
        AddBubbleTail(go.transform);

        go.SetActive(false);
    }

    void BuildCollapseIndicator(Transform root)
    {
        var go = new GameObject("GuideIndicator");
        go.transform.SetParent(root, false);
        _guideIndicatorGO = go;

        _guideIndicatorRT = go.AddComponent<RectTransform>();
        _guideIndicatorRT.anchorMin = new Vector2(0.5f, 0.5f);
        _guideIndicatorRT.anchorMax = new Vector2(0.5f, 0.5f);
        _guideIndicatorRT.pivot     = new Vector2(0.5f, 0f);
        _guideIndicatorRT.sizeDelta = new Vector2(100f, 24f);

        var cvs = go.AddComponent<Canvas>();
        cvs.overrideSorting = true; cvs.sortingOrder = 25;
        go.AddComponent<GraphicRaycaster>();

        var bg = go.AddComponent<Image>();
        bg.sprite = RoundedRectSprite(12f);
        bg.type   = Image.Type.Sliced;
        bg.color  = GUIDE_BG;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        var cs = btn.colors;
        cs.highlightedColor = new Color(0.13f, 0.17f, 0.28f, 0.98f);
        cs.pressedColor     = new Color(0.10f, 0.13f, 0.22f, 1.00f);
        btn.colors = cs;
        btn.onClick.AddListener(OnExpandClicked);

        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding  = new RectOffset(10, 10, 0, 0);
        hlg.spacing  = 4;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        var textGO = new GameObject("IndText");
        textGO.transform.SetParent(go.transform, false);
        textGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        _guideIndicatorText = textGO.AddComponent<TextMeshProUGUI>();
        _guideIndicatorText.fontSize         = 8f;
        _guideIndicatorText.color            = GUIDE_TITLE;
        _guideIndicatorText.alignment        = TextAlignmentOptions.Center;
        _guideIndicatorText.textWrappingMode = TextWrappingModes.NoWrap;
        _guideIndicatorText.overflowMode     = TextOverflowModes.Ellipsis;
        KorFont(_guideIndicatorText);

        // ── 꼬리 삼각형 ───────────────────────────────────
        AddBubbleTail(go.transform);

        go.SetActive(false);
    }

    // 헤더 인라인 소형 버튼 생성 헬퍼
    void MakeHeaderBtn(Transform parent, string label, float w, UnityEngine.Events.UnityAction cb)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredWidth = w;
        var img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.08f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cs = btn.colors;
        cs.highlightedColor = new Color(1, 1, 1, 0.20f);
        cs.pressedColor     = new Color(1, 1, 1, 0.30f);
        btn.colors = cs;
        btn.onClick.AddListener(cb);
        var lbl = (TextMeshProUGUI)MakeLabel(go.transform, label, 9f, new Color(1, 1, 1, 0.65f));
        KorFont(lbl);
        var lrt = lbl.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
    }

    // 공통 꼬리 삼각형
    void AddBubbleTail(Transform parent)
    {
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(parent, false);
        arrowGO.AddComponent<LayoutElement>().ignoreLayout = true;
        var rt = arrowGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(16f, 10f);
        rt.anchoredPosition  = new Vector2(0f, 1f);
        rt.localEulerAngles  = new Vector3(0f, 0f, -90f);
        var img = arrowGO.AddComponent<Image>();
        img.sprite = AITriangleSprite();
        img.type   = Image.Type.Simple;
        img.color  = GUIDE_BG;
    }

    // ── 상태 전환 ─────────────────────────────────────────
    void SetGuideState(GuideState state)
    {
        _guideState = state;
        _guideBubbleGO?.SetActive(state == GuideState.Expanded);
        _guideIndicatorGO?.SetActive(state == GuideState.Collapsed);
    }

    void OnCollapseClicked()
    {
        if (_guideIndicatorText != null)
            _guideIndicatorText.text = $"▸ {_lastGuideTitle ?? "해설 펼치기"}";
        SetGuideState(GuideState.Collapsed);
    }

    void OnExpandClicked()
    {
        SetGuideState(GuideState.Expanded);
        StartCoroutine(PositionBubbleAfterLayout());
    }

    void OnGuideCloseClicked()
    {
        SetGuideState(GuideState.Hidden);
        if (selectedObject != null && selectedObject.GetComponent<HanokGuideClosedMarker>() == null)
            selectedObject.AddComponent<HanokGuideClosedMarker>();
    }

    void HideGuideBubble()
    {
        SetGuideState(GuideState.Hidden);
        _lastGuidedAssetKey = null;
    }

    // ── 위치·스케일 동기화 (Update에서 호출) ──────────────
    internal void UpdateGuideBubble()
    {
        bool anyVisible = _guideState != GuideState.Hidden
                       && selectedObject != null
                       && Camera.main != null;
        if (!anyVisible) return;

        float cur    = selectedObject.transform.lossyScale.magnitude;
        float factor = _guideBubbleBaseScaleMag > 0.001f
            ? Mathf.Clamp(cur / _guideBubbleBaseScaleMag, 0.25f, 4f) : 1f;

        if (!GetBuildingCanvasPos(out Vector2 topPos)) return;

        float ch = _canvasRT.rect.height * 0.5f;
        float targetY = Mathf.Clamp(topPos.y + 14f, -ch + 60f, ch - 120f);

        if (_guideState == GuideState.Expanded && _guideBubbleRT != null)
        {
            _guideBubbleRT.localScale = Vector3.one * factor;
            var p = _guideBubbleRT.anchoredPosition;
            p.y = targetY;
            _guideBubbleRT.anchoredPosition = p;
        }
        else if (_guideState == GuideState.Collapsed && _guideIndicatorRT != null)
        {
            _guideIndicatorRT.localScale = Vector3.one * factor;
            float cw = _canvasRT.rect.width * 0.5f;
            _guideIndicatorRT.anchoredPosition = new Vector2(
                Mathf.Clamp(topPos.x, -cw + 60f, cw - 60f), targetY);
        }
    }

    bool GetBuildingCanvasPos(out Vector2 result)
    {
        result = Vector2.zero;
        if (selectedObject == null || Camera.main == null) return false;

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
        if (sp.z <= 0f) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, new Vector2(sp.x, sp.y), null, out result);
    }

    IEnumerator PositionBubbleAfterLayout()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_guideBubbleRT);
        if (!GetBuildingCanvasPos(out Vector2 lp)) yield break;

        float halfW = GUIDE_W * 0.5f;
        float cw    = _canvasRT.rect.width  * 0.5f;
        float ch    = _canvasRT.rect.height * 0.5f;
        lp.x = Mathf.Clamp(lp.x, -cw + halfW + 20f, cw - halfW - 20f);
        lp.y = Mathf.Clamp(lp.y + 14f, -ch + 60f, ch - 120f);
        _guideBubbleRT.anchoredPosition = lp;
    }

    // ── 선택·삭제 연동 ────────────────────────────────────
    void TriggerAutoGuide(GameObject obj)
    {
        if (obj == null) { SetGuideState(GuideState.Hidden); return; }
        RequestGuideForObject(obj);
    }

    void RequestGuideForObject(GameObject obj)
    {
        if (_guideRequestInProgress) return;

        var meta = obj.GetComponent<HanokPlacedAssetMetadata>();
        string assetKey = !string.IsNullOrEmpty(meta?.assetKey)
            ? meta.assetKey : CleanPlacedObjectName(obj.name);

        // X로 닫은 오브젝트는 영구 무시
        if (obj.GetComponent<HanokGuideClosedMarker>() != null) return;
        // 현재 표시 중인 같은 에셋 재클릭 무시
        if (assetKey == _lastGuidedAssetKey) return;

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

        if (string.IsNullOrEmpty(GetSavedApiKey()))
        {
            _lastGuidedAssetKey = assetKey;
            return;
        }

        _lastGuidedAssetKey = assetKey;
        StartCoroutine(RequestGuideCo(assetKey, displayName, categoryLabel));
    }

    IEnumerator RequestGuideCo(string assetKey, string displayName, string categoryLabel)
    {
        _guideRequestInProgress = true;

        string system =
            "당신은 한국 전통 건축 문화재에 정통한 큐레이터이자 역사학자입니다. " +
            "수십 년간의 현장 연구를 통해 조선·고려·통일신라 시대의 건축 양식과 문화적 맥락을 깊이 이해하고 있습니다. " +
            "해설 원칙: 마크다운 기호를 일절 사용하지 않는다. " +
            "역사적 배경·건축 특징·문화적 의미를 자연스럽게 녹여낸 단 하나의 완결된 문단(정확히 3문장)으로 작성한다. " +
            "반드시 마침표로 끝맺는다. 품격 있고 생동감 있는 문체로.";

        var sb = new StringBuilder();
        sb.Append("다음 건축물을 해설해 주십시오.\n\n건축물명: ").AppendLine(displayName);
        if (!string.IsNullOrEmpty(categoryLabel))
            sb.Append("분류: ").AppendLine(categoryLabel);

        string body =
            "{\"model\":"  + JsonStr(GetApiModel()) +
            ",\"max_tokens\":500" +
            ",\"system\":" + JsonStr(system) +
            ",\"messages\":[{\"role\":\"user\",\"content\":" + JsonStr(sb.ToString()) + "}]}";

        using var www = new UnityWebRequest("https://api.anthropic.com/v1/messages", "POST");
        www.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("content-type",      "application/json");
        www.SetRequestHeader("x-api-key",          GetSavedApiKey());
        www.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return www.SendWebRequest();

        _guideRequestInProgress = false;

        if (www.result != UnityWebRequest.Result.Success)
        { Debug.LogError($"[GuideAPI] {www.responseCode}: {www.error}\n{www.downloadHandler.text}"); yield break; }

        ClaudeResponse resp = null;
        try { resp = JsonUtility.FromJson<ClaudeResponse>(www.downloadHandler.text); }
        catch (System.Exception e) { Debug.LogError($"[GuideAPI] 파싱 실패: {e.Message}"); yield break; }

        if (resp?.content == null || resp.content.Length == 0) yield break;

        var meta2 = selectedObject?.GetComponent<HanokPlacedAssetMetadata>();
        string title   = !string.IsNullOrEmpty(meta2?.displayName) ? meta2.displayName : assetKey;
        string guideText = resp.content[0].text.Trim();

        if (_guideBubbleTitleText != null) _guideBubbleTitleText.text = title;
        if (_guideBubbleBodyText  != null) _guideBubbleBodyText.text  = guideText;
        _guideBubbleBaseScaleMag = selectedObject != null
            ? selectedObject.transform.lossyScale.magnitude : 1f;
        _guideBubbleRT.localScale = Vector3.one;

        SetGuideState(GuideState.Expanded);
        StartCoroutine(PositionBubbleAfterLayout());
    }

    void ClearGuide()
    {
        _lastGuidedAssetKey = null;
        SetGuideState(GuideState.Hidden);
    }
}

public class HanokGuideClosedMarker : MonoBehaviour { }
