using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// HanokUIManager — AI 프롬프트 기반 에셋 추천 (partial)
/// 화면 우측 하단 원형 버튼으로 입력창을 토글하고, Claude(Haiku)에게 카탈로그 중
/// 적합한 에셋을 추천받아 우측 패널 "AI 추천" 영역에 카드로 표시한다.
/// </summary>
public partial class HanokUIManager
{
    // ── 상태 ─────────────────────────────────────────────
    Transform     _aiResultContainer;
    TMP_InputField _aiInputField;
    RectTransform _aiButtonRT;
    bool          _aiRequestInProgress;
    string        _aiCatalog;
    static Sprite _aiCircleSprite;
    static Sprite _aiTriangleSprite;

    // 화면 우하단 모서리에 고정되는 AI 토글 버튼 위치 (우측 패널이 숨겨졌을 때만 보임)
    static readonly Vector2 AI_BTN_MIN = new Vector2(-72, 30);
    static readonly Vector2 AI_BTN_MAX = new Vector2(-16, 86);

    // ── 우측 패널: AI 추천 섹션 ───────────────────────────
    void BuildAIRecommendationSection(Transform content)
    {
        InfoSectionLabel(content, "AI 추천");

        var resultsRT = NewRT(content, "AIResults");
        var vlg = resultsRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        resultsRT.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        _aiResultContainer = resultsRT;
        ShowAIMessage("프롬프트를 입력해 보세요");
    }

    // ── 우측 패널 상단: AI 프롬프트 입력 바 ───────────────
    void BuildAIInputBar(Transform content)
    {
        Spacer(content, 8);

        var row = NewRT(content, "AIInputRow");
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 36; le.flexibleWidth = 1;

        var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(12, 12, 0, 0);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        _aiInputField = MakeAIInputField(row);
        _aiInputField.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        _aiInputField.onSubmit.AddListener(_ => OnAIPromptSubmit());

        var sendGO = new GameObject("Send");
        sendGO.transform.SetParent(row, false);
        sendGO.AddComponent<LayoutElement>().preferredWidth = 48;
        var sendImg = sendGO.AddComponent<Image>();
        sendImg.color = NAVY;
        var sendBtn = sendGO.AddComponent<Button>();
        sendBtn.targetGraphic = sendImg;
        sendBtn.onClick.AddListener(OnAIPromptSubmit);

        var sendLbl = MakeLabel(sendGO.transform, "전송", 11, Color.white, bold: true);
        var sendLblRT = sendLbl.GetComponent<RectTransform>();
        sendLblRT.anchorMin = Vector2.zero; sendLblRT.anchorMax = Vector2.one;
        sendLblRT.offsetMin = sendLblRT.offsetMax = Vector2.zero;

        Spacer(content, 8);
        Divider(content);
    }

    // ── 화면 우측 하단: 원형 AI 토글 버튼 ─────────────────
    // 우측 패널이 열려 있는 동안에는 숨겨진다 (SetRightPanelVisible 참고)
    void BuildAIPromptWidget(Transform root)
    {
        var btnRT = NewRT(root, "AIButton");
        btnRT.anchorMin = new Vector2(1, 0);
        btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(1, 0);
        btnRT.offsetMin = AI_BTN_MIN;
        btnRT.offsetMax = AI_BTN_MAX;

        var btnImg = btnRT.GetComponent<Image>();
        btnImg.sprite = AICircleSprite();
        btnImg.type = Image.Type.Simple;
        btnImg.color = NAVY;

        var btn = btnRT.gameObject.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var cs = btn.colors;
        cs.highlightedColor = NAVY_LIGHT;
        cs.pressedColor = Hex("#0F2547");
        btn.colors = cs;

        var lbl = MakeLabel(btnRT, "AI", 14, Color.white, bold: true);
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        _aiButtonRT = btnRT;
        btn.onClick.AddListener(() => SetAIOverlayVisible(true));
    }

    // ── AI 오버레이 창 표시/숨김 ──────────────────────────
    void SetAIOverlayVisible(bool visible)
    {
        if (_aiOverlayRT == null) return;
        _aiOverlayRT.gameObject.SetActive(visible);
        _aiButtonRT?.gameObject.SetActive(!visible);
        if (visible) _aiInputField?.ActivateInputField();
    }

    void ToggleRightPanel() => SetAIOverlayVisible(!(_aiOverlayRT?.gameObject.activeSelf ?? false));

    // 우측 편집 패널은 항상 고정 표시 — 이 함수는 호환성 유지용
    void SetRightPanelVisible(bool visible) { }

    // 자유 텍스트 입력 필드 (검색창과 동일한 구성, placeholder만 다름)
    TMP_InputField MakeAIInputField(Transform parent)
    {
        var go = new GameObject("AIInput");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = BG_INPUT;
        AddRoundOutline(go.GetComponent<RectTransform>(), BORDER);

        var area = new GameObject("Area");
        area.transform.SetParent(go.transform, false);
        var aRT = area.AddComponent<RectTransform>();
        aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
        aRT.offsetMin = new Vector2(8, 2); aRT.offsetMax = new Vector2(-8, -2);
        area.AddComponent<RectMask2D>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(area.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 11; text.color = TEXT_MAIN;
        text.alignment = TextAlignmentOptions.Left;
        KorFont(text);

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(area.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = phRT.offsetMax = Vector2.zero;
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text = "궁금한 에셋을 설명해보세요...";
        ph.fontSize = 11; ph.color = TEXT_HINT;
        ph.alignment = TextAlignmentOptions.Left;
        KorFont(ph);

        var field = go.AddComponent<TMP_InputField>();
        field.targetGraphic = img;
        field.textViewport = aRT;
        field.textComponent = text;
        field.placeholder = ph;
        field.contentType = TMP_InputField.ContentType.Standard;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.caretColor = NAVY;
        field.selectionColor = new Color(NAVY.r, NAVY.g, NAVY.b, 0.25f);
        return field;
    }

    // 56x56 정사각형 RectTransform에 채워질 흰색 원형 스프라이트 (NAVY로 틴트되어 표시됨)
    static Sprite AICircleSprite()
    {
        if (_aiCircleSprite != null) return _aiCircleSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
        float radius = size / 2f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = Vector2.Distance(new Vector2(x, y), center);
            byte alpha = (byte)Mathf.Clamp(255f * (radius - dist), 0f, 255f);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        _aiCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _aiCircleSprite;
    }

    // 우측 패널 "접기" 버튼에 쓰일 흰색 오른쪽 화살표(▶) 스프라이트
    static Sprite AITriangleSprite()
    {
        if (_aiTriangleSprite != null) return _aiTriangleSprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float nx = x / (float)(size - 1);
            float ny = Mathf.Abs(y / (float)(size - 1) - 0.5f) * 2f;
            bool inside = ny <= (1f - nx);
            pixels[y * size + x] = new Color32(255, 255, 255, inside ? (byte)255 : (byte)0);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        _aiTriangleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _aiTriangleSprite;
    }

    void OnAIPromptSubmit()
    {
        if (_aiRequestInProgress) return;
        string prompt = _aiInputField.text.Trim();
        if (prompt.Length == 0) return;

        _aiRequestInProgress = true;
        _aiInputField.interactable = false;
        ShowAIMessage("AI에게 묻는 중...");
        StartCoroutine(RequestAIRecommendations(prompt));
    }

    void EndAIRequest()
    {
        _aiRequestInProgress = false;
        if (_aiInputField != null) _aiInputField.interactable = true;
    }

    // ── 카탈로그 (assetKey|displayName|tags,...) 1회 생성 후 캐싱 ──
    string BuildAICatalog()
    {
        if (_aiCatalog != null) return _aiCatalog;

        var sb = new StringBuilder();
        foreach (var entry in _assetEntries)
        {
            sb.Append(entry.prefab.name).Append('|').Append(entry.displayName).Append('|');
            sb.Append(string.Join(",", entry.searchTags));
            sb.Append('\n');
        }
        _aiCatalog = sb.ToString();
        return _aiCatalog;
    }

    // Claude 응답이 반드시 이 형식을 따르도록 강제하는 JSON Schema (RecommendationList와 1:1 대응)
    const string RECOMMENDATION_SCHEMA =
        "{\"type\":\"object\",\"properties\":{\"recommendations\":{\"type\":\"array\",\"items\":" +
        "{\"type\":\"object\",\"properties\":{\"assetKey\":{\"type\":\"string\"},\"reason\":{\"type\":\"string\"}}," +
        "\"required\":[\"assetKey\",\"reason\"],\"additionalProperties\":false}}}," +
        "\"required\":[\"recommendations\"],\"additionalProperties\":false}";

    // ── Claude API 호출 ───────────────────────────────────
    IEnumerator RequestAIRecommendations(string userPrompt)
    {
        var config = Resources.Load<ClaudeApiConfig>("ClaudeApiConfig");
        if (config == null || string.IsNullOrEmpty(config.apiKey))
        {
            ShowAIMessage("API 키가 설정되지 않았습니다.\nAssets/HanokBuilder/Resources/ClaudeApiConfig 를 생성하고 키를 입력하세요.");
            EndAIRequest();
            yield break;
        }

        string instruction =
            "너는 한옥 에셋 추천 도우미야. 아래 카탈로그(assetKey|표시명|태그) 안의 항목 중에서만 골라야 해.\n" +
            "사용자의 설명에 가장 잘 맞는 에셋을 최대 30개 추천해.\n\n" +
            "카탈로그:\n" + BuildAICatalog() +
            "\n사용자 요청: " + userPrompt;

        var reqBody = new ClaudeRequest
        {
            model = config.model,
            max_tokens = 4096,
            messages = new[] { new ClaudeMessage { role = "user", content = instruction } }
        };

        // JsonUtility는 임의의 중첩 객체(JSON Schema)를 직렬화할 수 없으므로,
        // 직렬화 결과의 마지막 '}' 앞에 output_config를 문자열로 이어붙인다.
        string baseJson = JsonUtility.ToJson(reqBody);
        string bodyJson = baseJson[..^1] +
            ",\"output_config\":{\"format\":{\"type\":\"json_schema\",\"schema\":" + RECOMMENDATION_SCHEMA + "}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

        using var www = new UnityWebRequest("https://api.anthropic.com/v1/messages", "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("content-type", "application/json");
        www.SetRequestHeader("x-api-key", config.apiKey);
        www.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            ShowAIMessage($"요청 실패 ({www.responseCode}): {www.error}");
            EndAIRequest();
            yield break;
        }

        ClaudeResponse response = null;
        try { response = JsonUtility.FromJson<ClaudeResponse>(www.downloadHandler.text); }
        catch (System.Exception e) { Debug.LogError($"[HanokAI] 응답 파싱 실패: {e.Message}"); }

        if (response == null || response.content == null || response.content.Length == 0)
        {
            ShowAIMessage("AI 응답을 해석하지 못했습니다.");
            EndAIRequest();
            yield break;
        }

        RecommendationList list = null;
        try { list = JsonUtility.FromJson<RecommendationList>(response.content[0].text); }
        catch (System.Exception e) { Debug.LogError($"[HanokAI] 추천 목록 파싱 실패: {e.Message}"); }

        if (list == null || list.recommendations == null || list.recommendations.Length == 0)
        {
            ShowAIMessage("추천 결과가 없습니다.");
            EndAIRequest();
            yield break;
        }

        RenderAIRecommendations(list.recommendations);
        EndAIRequest();
    }

    // ── 추천 결과 렌더링 (좌측 패널과 동일한 카드/Spawn 재사용) ──
    void RenderAIRecommendations(RecommendationItem[] items)
    {
        var matches = new List<HanokAssetEntry>();
        foreach (var item in items)
        {
            var entry = _assetEntries.Find(e => e.prefab.name == item.assetKey);
            if (entry != null) matches.Add(entry);
        }

        if (matches.Count == 0)
        {
            ShowAIMessage("카탈로그에서 일치하는 에셋을 찾지 못했습니다.");
            return;
        }

        SpawnAIRecommendations(matches);

        ClearAIResults();

        for (int i = 0; i < matches.Count; i += COLS)
        {
            var row = new GameObject("AIRow");
            row.transform.SetParent(_aiResultContainer, false);
            row.AddComponent<RectTransform>();

            var rle = row.AddComponent<LayoutElement>();
            rle.preferredHeight = CELL_H + 4f;
            rle.flexibleWidth = 1;
            row.AddComponent<Image>().color = Color.clear;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 2, 2);
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            for (int j = 0; j < COLS; j++)
            {
                int idx = i + j;
                if (idx < matches.Count)
                {
                    var prefab = matches[idx].prefab;
                    var rawImg = MakeGridCell(row.transform, matches[idx].displayName, () => Spawn(prefab));
                    EnqueueThumbnail(prefab, rawImg);
                }
                else
                {
                    AddAIBlankCell(row.transform);
                }
            }
        }

        StartCoroutine(RebuildAIResultsLayout());
    }

    // ── 추천 에셋을 가운데 3D 뷰에 동시 배치 ──────────────
    // 가장 큰 에셋의 footprint를 기준으로 정사각형 격자를 만들어
    // 카메라 피벗 주변에 서로 겹치지 않게 펼쳐서 배치한다.
    void SpawnAIRecommendations(List<HanokAssetEntry> matches)
    {
        var basePos = GetSpawnPos();
        var spawned = new List<GameObject>();
        float maxFootprint = 0f;

        foreach (var entry in matches)
        {
            var obj = SpawnAt(entry.prefab, basePos);
            spawned.Add(obj);

            var rends = obj.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) continue;
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            maxFootprint = Mathf.Max(maxFootprint, b.size.x, b.size.z);
        }

        float cell = Mathf.Max(maxFootprint + 1.5f, 2.5f);
        int cols = Mathf.CeilToInt(Mathf.Sqrt(spawned.Count));
        int rows = Mathf.CeilToInt(spawned.Count / (float)cols);
        float startX = -(cols - 1) * cell * 0.5f;
        float startZ = -(rows - 1) * cell * 0.5f;

        for (int i = 0; i < spawned.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            spawned[i].transform.position = new Vector3(
                basePos.x + startX + col * cell,
                basePos.y,
                basePos.z + startZ + row * cell);
            PlaceOnFloor(spawned[i]);
        }

        SelectObject(spawned[spawned.Count - 1]);
        Camera.main?.GetComponent<HanokCameraController>()?.FrameAll();
    }

    void AddAIBlankCell(Transform parent)
    {
        var blank = new GameObject("Blank");
        blank.transform.SetParent(parent, false);
        blank.AddComponent<RectTransform>();
        var le = blank.AddComponent<LayoutElement>();
        le.preferredWidth = CELL_W;
        le.flexibleWidth = 1;
        blank.AddComponent<Image>().color = Color.clear;
    }

    void ClearAIResults()
    {
        if (_aiResultContainer == null) return;
        var children = new List<GameObject>();
        foreach (Transform ch in _aiResultContainer) children.Add(ch.gameObject);
        foreach (var ch in children) DestroyImmediate(ch);
    }

    void ShowAIMessage(string message)
    {
        ClearAIResults();

        var go = new GameObject("AIMsg");
        go.transform.SetParent(_aiResultContainer, false);
        go.AddComponent<LayoutElement>().preferredHeight = 40;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = message;
        t.fontSize = 10;
        t.color = TEXT_SUB;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        if (HasKorean(message)) KorFont(t); else LatFont(t);
    }

    IEnumerator RebuildAIResultsLayout()
    {
        yield return null;
        if (_aiResultContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_aiResultContainer.GetComponent<RectTransform>());
    }

    // ── Claude API 요청/응답 DTO ──────────────────────────
    [System.Serializable]
    class ClaudeMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    class ClaudeRequest
    {
        public string model;
        public int max_tokens;
        public ClaudeMessage[] messages;
    }

    [System.Serializable]
    class ClaudeContentBlock
    {
        public string type;
        public string text;
    }

    [System.Serializable]
    class ClaudeResponse
    {
        public ClaudeContentBlock[] content;
    }

    [System.Serializable]
    class RecommendationItem
    {
        public string assetKey;
        public string reason;
    }

    [System.Serializable]
    class RecommendationList
    {
        public RecommendationItem[] recommendations;
    }
}
