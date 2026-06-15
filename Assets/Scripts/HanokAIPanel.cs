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
    static Sprite _aiRoundedRectSprite;
    const int AI_AUTO_PLACE_MIN = 3;
    const int AI_AUTO_PLACE_MAX = 12;
    const float AI_AUTO_PLACE_SPACING = 3.2f;
    const float AI_AUTO_PLACE_JITTER = 0.65f;

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
        barImg.sprite = AIRoundedRectSprite();
        barImg.type = Image.Type.Sliced;
        barImg.color = BG_INPUT;
        AddRoundOutline(barRT, BORDER);

        var hlg = barRT.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6; hlg.padding = new RectOffset(6, 6, 6, 6);
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        _aiInputField = MakeAIInputField(barRT);
        _aiInputField.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
        _aiInputField.onSubmit.AddListener(_ => OnAIPromptSubmit());

        var sendGO = new GameObject("Send");
        sendGO.transform.SetParent(barRT, false);
        sendGO.AddComponent<LayoutElement>().preferredWidth = 48;
        var sendImg = sendGO.AddComponent<Image>();
        sendImg.sprite = AIRoundedRectSprite();
        sendImg.type = Image.Type.Sliced;
        sendImg.color = NAVY;
        var sendBtn = sendGO.AddComponent<Button>();
        sendBtn.targetGraphic = sendImg;
        var cs = sendBtn.colors;
        cs.highlightedColor = NAVY_LIGHT;
        cs.pressedColor = Hex("#0F2547");
        sendBtn.colors = cs;
        sendBtn.onClick.AddListener(OnAIPromptSubmit);

        var sendLbl = MakeLabel(sendGO.transform, "전송", 11, Color.white, bold: true);
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
        panelImg.sprite = AIRoundedRectSprite();
        panelImg.type = Image.Type.Sliced;
        panelImg.color = BG_PANEL;
        AddRoundOutline(panelRT, BORDER);

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

    // 모서리가 둥근 9-slice 사각형 스프라이트 (프롬프트 바 / 결과 패널 / 전송 버튼 배경 공용)
    static Sprite AIRoundedRectSprite()
    {
        if (_aiRoundedRectSprite != null) return _aiRoundedRectSprite;

        const int size = 64;
        const float radius = 20f;
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
        _aiRoundedRectSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        return _aiRoundedRectSprite;
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
        foreach (var entry in GetAICatalogEntries())
        {
            sb.Append(entry.assetKey).Append('|').Append(entry.displayName).Append('|');
            sb.Append(string.Join(",", entry.searchTags));
            sb.Append('\n');
        }
        _aiCatalog = sb.ToString();
        return _aiCatalog;
    }

    List<HanokAssetEntry> GetAICatalogEntries()
    {
        var cultureEntries = _assetEntries.FindAll(e => e.isCultureAsset);
        return cultureEntries.Count > 0 ? cultureEntries : _assetEntries;
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
            var localItems = BuildLocalRecommendations(userPrompt);
            if (localItems.Length == 0)
            {
                ShowAIMessage("추가 에셋에서 일치하는 항목을 찾지 못했습니다.\nAPI 키를 설정하면 더 자연어에 가까운 추천을 받을 수 있습니다.");
            }
            else
            {
                int placed = RenderAIRecommendations(localItems);
                if (placed == 0)
                    ShowToast("API 키 없이 추가 에셋명/태그로 추천했습니다.");
            }
            EndAIRequest();
            yield break;
        }

        string instruction =
            "너는 문화포털 메타버스 에셋 추천 도우미야. 아래 카탈로그(assetKey|표시명|태그) 안의 항목 중에서만 골라야 해.\n" +
            "사용자의 설명에 가장 잘 맞는 추가 에셋을 최대 30개 추천해. assetKey는 카탈로그의 값을 정확히 복사해야 해.\n\n" +
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

    // ── 추천 결과 렌더링 + 자동 배치 ─────────────────────
    int RenderAIRecommendations(RecommendationItem[] items)
    {
        var matches = new List<HanokAssetEntry>();
        var seen = new HashSet<string>();
        foreach (var item in items)
        {
            var entry = _assetEntries.Find(e => e.assetKey == item.assetKey || e.prefab.name == item.assetKey);
            if (entry != null && seen.Add(entry.assetKey))
                matches.Add(entry);
        }

        if (matches.Count == 0)
        {
            ShowAIMessage("카탈로그에서 일치하는 에셋을 찾지 못했습니다.");
            return 0;
        }

        ClearAIResults();

        for (int i = 0; i < matches.Count; i++)
        {
            var entry = matches[i];
            var prefab = entry.prefab;
            var rawImg = MakeGridCell(_aiResultContainer, entry.displayName, () => Spawn(entry));
            StartCoroutine(CaptureThumbnail(prefab, rawImg, i));
        }

        _aiResultsPanelRT.gameObject.SetActive(true);
        StartCoroutine(RebuildAIResultsLayout());

        int placed = AutoPlaceRecommendationMatches(matches);
        if (placed > 0)
            ShowToast($"추천 에셋 {placed}개를 자동 배치했습니다.");
        return placed;
    }

    int AutoPlaceRecommendationMatches(List<HanokAssetEntry> matches)
    {
        if (matches == null || matches.Count == 0) return 0;

        var pool = new List<HanokAssetEntry>(matches);
        for (int i = 0; i < pool.Count; i++)
        {
            int swap = UnityEngine.Random.Range(i, pool.Count);
            var tmp = pool[i];
            pool[i] = pool[swap];
            pool[swap] = tmp;
        }

        int min = Mathf.Min(AI_AUTO_PLACE_MIN, pool.Count);
        int max = Mathf.Min(AI_AUTO_PLACE_MAX, pool.Count);
        int count = UnityEngine.Random.Range(min, max + 1);
        var center = GetAIAutoPlaceCenter();
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt(count / (float)cols);
        int placed = 0;
        GameObject last = null;

        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = (col - (cols - 1) * 0.5f) * AI_AUTO_PLACE_SPACING;
            float z = (row - (rows - 1) * 0.5f) * AI_AUTO_PLACE_SPACING;
            x += UnityEngine.Random.Range(-AI_AUTO_PLACE_JITTER, AI_AUTO_PLACE_JITTER);
            z += UnityEngine.Random.Range(-AI_AUTO_PLACE_JITTER, AI_AUTO_PLACE_JITTER);

            var obj = SpawnAt(pool[i], center + new Vector3(x, 0f, z));
            if (obj == null) continue;

            var euler = obj.transform.eulerAngles;
            obj.transform.eulerAngles = new Vector3(euler.x, UnityEngine.Random.Range(0f, 360f), euler.z);
            PlaceOnFloor(obj);
            last = obj;
            placed++;
        }

        if (last != null) SelectObject(last);
        return placed;
    }

    Vector3 GetAIAutoPlaceCenter()
    {
        var center = GetSpawnPos();
        center.y = 0f;
        return center;
    }

    RecommendationItem[] BuildLocalRecommendations(string prompt)
    {
        var catalogEntries = GetAICatalogEntries();
        var tokens = TokenizePrompt(prompt);
        var scored = new List<ScoredAsset>();

        foreach (var entry in catalogEntries)
        {
            int score = ScoreLocalAsset(entry, tokens);
            if (score > 0)
                scored.Add(new ScoredAsset { entry = entry, score = score });
        }

        scored.Sort((a, b) =>
        {
            int byScore = b.score.CompareTo(a.score);
            if (byScore != 0) return byScore;
            return string.Compare(a.entry.displayName, b.entry.displayName, System.StringComparison.OrdinalIgnoreCase);
        });

        if (scored.Count == 0)
        {
            foreach (var entry in catalogEntries)
            {
                if (scored.Count >= 30) break;
                scored.Add(new ScoredAsset { entry = entry, score = 0 });
            }
        }

        int count = Mathf.Min(30, scored.Count);
        var result = new RecommendationItem[count];
        for (int i = 0; i < count; i++)
            result[i] = new RecommendationItem
            {
                assetKey = scored[i].entry.assetKey,
                reason = "local"
            };
        return result;
    }

    List<string> TokenizePrompt(string prompt)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        foreach (char c in prompt)
        {
            if (char.IsLetterOrDigit(c) || c >= '가' && c <= '힣')
            {
                sb.Append(char.ToLowerInvariant(c));
                continue;
            }

            AddToken(tokens, sb);
        }
        AddToken(tokens, sb);
        return tokens;
    }

    void AddToken(List<string> tokens, StringBuilder sb)
    {
        if (sb.Length < 2)
        {
            sb.Length = 0;
            return;
        }

        tokens.Add(sb.ToString());
        sb.Length = 0;
    }

    int ScoreLocalAsset(HanokAssetEntry entry, List<string> tokens)
    {
        if (tokens.Count == 0) return 1;

        string display = entry.displayName.ToLowerInvariant();
        string name = entry.prefab.name.ToLowerInvariant();
        string haystack = (entry.assetKey + " " + entry.displayName + " " + entry.prefab.name + " " +
            string.Join(" ", entry.searchTags)).ToLowerInvariant();

        int score = 0;
        foreach (var token in tokens)
        {
            if (display.Contains(token)) score += 8;
            if (name.Contains(token)) score += 5;
            if (haystack.Contains(token)) score += 3;
        }
        return score;
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

    class ScoredAsset
    {
        public HanokAssetEntry entry;
        public int score;
    }
}
