using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// HanokUIManager — AI 프롬프트 기반 에셋 추천 (partial)
/// 화면 하단 중앙의 둥근 프롬프트 바에 입력 후 전송하면, Claude(Haiku)에게 카탈로그 중
/// 적합한 에셋을 추천받아 프롬프트 바 바로 위 결과 패널에 가로 한 줄로 표시한다.
/// </summary>
public partial class HanokUIManager
{
    // ── 상태 ─────────────────────────────────────────────
    Transform     _aiResultContainer;
    TMP_InputField _aiInputField;
    RectTransform _aiResultsPanelRT;
    bool          _aiRequestInProgress;
    string        _aiCatalog;
    static Sprite _aiCircleSprite;
    static Sprite _aiTriangleSprite;
    static readonly Dictionary<float, Sprite> _roundedRectCache = new Dictionary<float, Sprite>();
    static readonly Dictionary<float, Sprite> _topRoundedRectCache = new Dictionary<float, Sprite>();
    static readonly Dictionary<float, Sprite> _innerGlowCache = new Dictionary<float, Sprite>();
    static readonly Dictionary<(float radius, float thickness), Sprite> _ringCache = new Dictionary<(float radius, float thickness), Sprite>();

    // ── 화면 하단 중앙: 둥근 모서리 프롬프트 바 (항상 표시) ──
    void BuildAIPromptWidget(Transform root)
    {
        BuildAIResultsPanel(root);

        var barRT = NewRT(root, "AIPromptBar");
        barRT.anchorMin = new Vector2(0.5f, 0f);
        barRT.anchorMax = new Vector2(0.5f, 0f);
        barRT.pivot     = new Vector2(0.5f, 0f);
        barRT.offsetMin = new Vector2(-220, 14);
        barRT.offsetMax = new Vector2(220, 58);

        var barImg = barRT.GetComponent<Image>();
        barImg.sprite = RoundedRectSprite(18f);
        barImg.type = Image.Type.Sliced;
        barImg.color = BG_INPUT;
        barImg.material = GlassMaterial();
        AddInnerGlow(barRT, 18f);
        AddOuterBorder(barRT, 18f);

        var hlg = barRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8; hlg.padding = new RectOffset(8, 8, 8, 8);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        _aiInputField = MakeAIInputField(barRT);
        _aiInputField.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        _aiInputField.onSubmit.AddListener(_ => OnAIPromptSubmit());

        var sendGO = new GameObject("Send");
        sendGO.transform.SetParent(barRT, false);
        sendGO.AddComponent<LayoutElement>().preferredWidth = 48;
        var sendImg = sendGO.AddComponent<Image>();
        sendImg.sprite = RoundedRectSprite(8f);
        sendImg.type = Image.Type.Sliced;
        sendImg.color = BTN_ACTIVE;

        var sendOutline = sendGO.AddComponent<Outline>();
        sendOutline.effectColor = GLOW;
        sendOutline.effectDistance = new Vector2(1, -1);

        var sendBtn = sendGO.AddComponent<Button>();
        sendBtn.targetGraphic = sendImg;
        var cs = sendBtn.colors;
        cs.highlightedColor = BTN_ACTIVE_HOVER;
        cs.pressedColor = BTN_ACTIVE_PRESS;
        sendBtn.colors = cs;
        sendBtn.onClick.AddListener(OnAIPromptSubmit);

        var sendLbl = MakeLabel(sendGO.transform, "전송", 11, TEXT_ON_ACCENT, bold: true);
        var sendLblRT = sendLbl.GetComponent<RectTransform>();
        sendLblRT.anchorMin = Vector2.zero; sendLblRT.anchorMax = Vector2.one;
        sendLblRT.offsetMin = sendLblRT.offsetMax = Vector2.zero;
    }

    // ── 프롬프트 바 바로 위: AI 추천 결과 한 줄(가로 스크롤) ──
    // 제출 전에는 숨겨져 있고, 추천/안내 메시지가 도착하면 표시된다.
    void BuildAIResultsPanel(Transform root)
    {
        var panelRT = NewRT(root, "AIResultsPanel");
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot     = new Vector2(0.5f, 0f);
        panelRT.offsetMin = new Vector2(-320, 66);
        panelRT.offsetMax = new Vector2(320, 170);

        var panelImg = panelRT.GetComponent<Image>();
        panelImg.sprite = RoundedRectSprite(18f);
        panelImg.type = Image.Type.Sliced;
        panelImg.color = BG_PANEL;
        panelImg.material = GlassMaterial();
        AddInnerGlow(panelRT, 18f);
        AddOuterBorder(panelRT, 18f);

        var hScroll = MakeHorizontalScroll(panelRT);
        _aiResultContainer = hScroll.transform.Find("Viewport/Content");

        _aiResultsPanelRT = panelRT;
        panelRT.gameObject.SetActive(false);
    }

    // 자유 텍스트 입력 필드 (배경/테두리는 프롬프트 바 자체가 담당하므로 투명 처리)
    TMP_InputField MakeAIInputField(Transform parent)
    {
        var go = new GameObject("AIInput");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = Color.clear;

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
        field.caretColor = TEXT_MAIN;
        field.selectionColor = new Color(TEXT_MAIN.r, TEXT_MAIN.g, TEXT_MAIN.b, 0.25f);
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

    // 좌측 패널 접기/펼치기 버튼에 쓰일 흰색 오른쪽 화살표(▶) 스프라이트
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

    // 모서리가 둥근 9-slice 사각형 스프라이트 (반지름(px)별 캐시) — 패널/카드/버튼/입력창 배경 공용
    static Sprite RoundedRectSprite(float radius)
    {
        if (_roundedRectCache.TryGetValue(radius, out var cached) && cached != null)
            return cached;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool cornerX = x < radius || x > size - 1 - radius;
            bool cornerY = y < radius || y > size - 1 - radius;
            byte alpha = 255;
            if (cornerX && cornerY)
            {
                float cx = x < radius ? radius : size - 1 - radius;
                float cy = y < radius ? radius : size - 1 - radius;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                alpha = (byte)Mathf.Clamp(255f * (radius - dist + 1f), 0f, 255f);
            }
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var border = new Vector4(radius, radius, radius, radius);
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        _roundedRectCache[radius] = sprite;
        return sprite;
    }

    // 위쪽 모서리만 둥근 9-slice 사각형 스프라이트 — 패널 상단에 딱 맞붙는 헤더 바 전용
    static Sprite TopRoundedRectSprite(float radius)
    {
        if (_topRoundedRectCache.TryGetValue(radius, out var cached) && cached != null)
            return cached;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool cornerX = x < radius || x > size - 1 - radius;
            bool cornerY = y > size - 1 - radius; // 위쪽 모서리만 라운딩 (아래쪽은 사각형 유지)
            byte alpha = 255;
            if (cornerX && cornerY)
            {
                float cx = x < radius ? radius : size - 1 - radius;
                float cy = size - 1 - radius;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                alpha = (byte)Mathf.Clamp(255f * (radius - dist + 1f), 0f, 255f);
            }
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var border = new Vector4(radius, radius, radius, radius);
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        _topRoundedRectCache[radius] = sprite;
        return sprite;
    }

    // 테두리 쪽으로 갈수록 밝아지는 내부 글로우 스프라이트 — 볼록한(convex) 입체감 표현
    static Sprite InnerGlowSprite(float radius)
    {
        if (_innerGlowCache.TryGetValue(radius, out var cached) && cached != null)
            return cached;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool cornerX = x < radius || x > size - 1 - radius;
            bool cornerY = y < radius || y > size - 1 - radius;
            float edgeDist;
            float shapeAlpha = 255f;
            if (cornerX && cornerY)
            {
                float cx = x < radius ? radius : size - 1 - radius;
                float cy = y < radius ? radius : size - 1 - radius;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                edgeDist = radius - dist;
                shapeAlpha = Mathf.Clamp(255f * (radius - dist + 1f), 0f, 255f);
            }
            else
            {
                edgeDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
            }

            float t = Mathf.Clamp01(edgeDist / radius);
            float glow = (1f - t) * 255f;
            byte alpha = (byte)Mathf.Min(glow, shapeAlpha);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var border = new Vector4(radius, radius, radius, radius);
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        _innerGlowCache[radius] = sprite;
        return sprite;
    }

    // 모서리가 둥근 9-slice 사각형의 가장자리를 따라가는 고리(ring) 스프라이트 — 패널 외곽 테두리 전용
    static Sprite RoundedRectRingSprite(float radius, float thickness)
    {
        var key = (radius, thickness);
        if (_ringCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool cornerX = x < radius || x > size - 1 - radius;
            bool cornerY = y < radius || y > size - 1 - radius;
            float shapeAlpha = 255f;
            float edgeDist;
            if (cornerX && cornerY)
            {
                float cx = x < radius ? radius : size - 1 - radius;
                float cy = y < radius ? radius : size - 1 - radius;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                shapeAlpha = Mathf.Clamp(255f * (radius - dist + 1f), 0f, 255f);
                edgeDist = radius - dist;
            }
            else
            {
                edgeDist = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
            }

            float ringMask = Mathf.Clamp01(thickness - edgeDist + 1f);
            byte alpha = (byte)(shapeAlpha * ringMask);
            pixels[y * size + x] = new Color32(255, 255, 255, alpha);
        }
        tex.SetPixels32(pixels);
        tex.Apply();

        var border = new Vector4(radius, radius, radius, radius);
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        _ringCache[key] = sprite;
        return sprite;
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

    // ── 추천 결과 렌더링: 결과 패널에 가로 한 줄로 표시 (카드/Spawn은 좌측 패널과 동일하게 재사용) ──
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

        ClearAIResults();

        for (int i = 0; i < matches.Count; i++)
        {
            var prefab = matches[i].prefab;
            var rawImg = MakeGridCell(_aiResultContainer, matches[i].displayName, () => Spawn(prefab));
            StartCoroutine(CaptureThumbnail(prefab, rawImg, i));
        }

        _aiResultsPanelRT.gameObject.SetActive(true);
        StartCoroutine(RebuildAIResultsLayout());
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
        _aiResultsPanelRT.gameObject.SetActive(true);

        var go = new GameObject("AIMsg");
        go.transform.SetParent(_aiResultContainer, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 600; le.preferredHeight = CELL_H;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = message;
        t.fontSize = 10;
        t.color = TEXT_SUB;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        if (HasKorean(message)) KorFont(t); else LatFont(t);
        AddTextHalo(t);
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
