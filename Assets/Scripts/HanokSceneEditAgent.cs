using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// HanokUIManager — 자연어 씬 수정 에이전트 (partial)
/// "사랑채를 동쪽으로 5m 옮겨줘" 같은 자유 텍스트를 받아
/// Claude Tool Use로 씬 오브젝트를 직접 조작한다.
/// 대화 히스토리를 유지해 "그거", "방금 거" 같은 지시도 이해한다.
/// </summary>
public partial class HanokUIManager
{
    // ── 씬 편집 상태 ────────────────────────────────────────────────
    bool _sceneEditMode;
    Image           _sceneEditBtnImg;
    TextMeshProUGUI _sceneEditBtnLabel;
    readonly List<string>          _sceneEditHistory = new List<string>();
    readonly List<(string u, string b)> _sceneChatLog = new List<(string, string)>();
    Dictionary<int, GameObject> _sceneObjCache;

    // ── Tool 정의 ────────────────────────────────────────────────────
    const string SCENE_EDIT_TOOLS =
        "[" +
        "{\"name\":\"move_object\"," +
        "\"description\":\"씬의 오브젝트를 새 위치로 이동합니다.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"description\":\"씬 상태의 id 값\"}," +
        "\"x\":{\"type\":\"number\",\"description\":\"새 X 좌표\"}," +
        "\"z\":{\"type\":\"number\",\"description\":\"새 Z 좌표\"}" +
        "},\"required\":[\"id\",\"x\",\"z\"],\"additionalProperties\":false}}," +

        "{\"name\":\"rotate_object\"," +
        "\"description\":\"오브젝트의 Y축 회전을 설정합니다.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"id\":{\"type\":\"string\"}," +
        "\"rot_y\":{\"type\":\"number\",\"description\":\"Y축 회전 (0=남향 90=서향 180=북향 270=동향)\"}" +
        "},\"required\":[\"id\",\"rot_y\"],\"additionalProperties\":false}}," +

        "{\"name\":\"delete_object\"," +
        "\"description\":\"씬에서 오브젝트를 제거합니다.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"id\":{\"type\":\"string\"}" +
        "},\"required\":[\"id\"],\"additionalProperties\":false}}," +

        "{\"name\":\"spawn_object\"," +
        "\"description\":\"카탈로그의 에셋을 씬에 새로 배치합니다.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"asset_key\":{\"type\":\"string\",\"description\":\"카탈로그의 assetKey (정확히 일치)\"}," +
        "\"x\":{\"type\":\"number\"}," +
        "\"z\":{\"type\":\"number\"}," +
        "\"rot_y\":{\"type\":\"number\"}" +
        "},\"required\":[\"asset_key\",\"x\",\"z\",\"rot_y\"],\"additionalProperties\":false}}," +

        "{\"name\":\"scale_object\"," +
        "\"description\":\"오브젝트의 크기를 배율로 조정합니다. 1.0=원본 크기. '반으로 줄여'→0.5, '두 배로 키워'→2.0\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"description\":\"씬 상태의 id 값\"}," +
        "\"scale\":{\"type\":\"number\",\"description\":\"균등 스케일 배율 (0.1~5.0)\"}" +
        "},\"required\":[\"id\",\"scale\"],\"additionalProperties\":false}}," +

        "{\"name\":\"move_relative\"," +
        "\"description\":\"오브젝트를 현재 위치에서 상대적으로 이동합니다. '동쪽으로 5m'→dx=5, '뒤로 3m'→dz=-3\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"description\":\"씬 상태의 id 값\"}," +
        "\"dx\":{\"type\":\"number\",\"description\":\"X축 이동량(m). 동(+)/서(-)\"}," +
        "\"dz\":{\"type\":\"number\",\"description\":\"Z축 이동량(m). 북(+)/남(-)\"}" +
        "},\"required\":[\"id\",\"dx\",\"dz\"],\"additionalProperties\":false}}," +

        "{\"name\":\"duplicate_object\"," +
        "\"description\":\"씬의 오브젝트를 복사해 근처에 새로 배치합니다.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"id\":{\"type\":\"string\",\"description\":\"복사할 오브젝트 id\"}," +
        "\"dx\":{\"type\":\"number\",\"description\":\"원본 대비 X 오프셋\"}," +
        "\"dz\":{\"type\":\"number\",\"description\":\"원본 대비 Z 오프셋\"}" +
        "},\"required\":[\"id\",\"dx\",\"dz\"],\"additionalProperties\":false}}," +

        "{\"name\":\"clear_scene\"," +
        "\"description\":\"씬의 배치된 오브젝트를 모두 제거합니다. '다 지워', '초기화'에 사용.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{}," +
        "\"additionalProperties\":false}}" +

        "]";

    // ── 씬 상태 직렬화 ───────────────────────────────────────────────
    string BuildSceneState()
    {
        var metas = Object.FindObjectsOfType<HanokPlacedAssetMetadata>();
        if (metas.Length == 0) return "[]";

        var sb = new StringBuilder("[");
        for (int i = 0; i < metas.Length; i++)
        {
            var m  = metas[i];
            var t  = m.transform;
            var p  = t.position;
            float rotY = t.eulerAngles.y;
            sb.Append("{");
            sb.Append($"\"id\":\"{m.gameObject.GetInstanceID()}\",");
            sb.Append($"\"asset_key\":{SceneJsonStr(m.assetKey)},");
            sb.Append($"\"display_name\":{SceneJsonStr(m.displayName)},");
            float scale = t.localScale.x;
            sb.Append($"\"x\":{p.x:F2},\"z\":{p.z:F2},\"rot_y\":{rotY:F1},\"scale\":{scale:F2}");
            sb.Append("}");
            if (i < metas.Length - 1) sb.Append(",");
        }
        sb.Append("]");
        return sb.ToString();
    }

    // ── 카탈로그 (assetKey 목록만 경량으로) ──────────────────────────
    string BuildSceneEditCatalog()
    {
        var sb = new StringBuilder();
        foreach (var e in _assetEntries)
            if (!string.IsNullOrEmpty(e.assetKey))
                sb.AppendLine($"{e.assetKey}: {e.displayName}");
        return sb.ToString();
    }

    // ── 메인 코루틴 ──────────────────────────────────────────────────
    IEnumerator RequestSceneEdit(string userPrompt)
    {
        string apiKey = GetSavedApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            ShowApiKeyPanel();
            EndAIRequest();
            yield break;
        }

        // 오브젝트 딕셔너리 1회 빌드 — FindById가 매 툴마다 FindObjectsOfType 순회하지 않도록
        _sceneObjCache = new Dictionary<int, GameObject>();
        foreach (var m in Object.FindObjectsOfType<HanokPlacedAssetMetadata>())
            _sceneObjCache[m.gameObject.GetInstanceID()] = m.gameObject;

        // 카메라 중심이 바닥(y=0)과 만나는 지점 계산
        var cam = Camera.main;
        float camCX = 0f, camCZ = 0f;
        if (cam != null)
        {
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Mathf.Abs(ray.direction.y) > 0.001f)
            {
                float t = -ray.origin.y / ray.direction.y;
                if (t > 0f)
                {
                    var hit = ray.origin + ray.direction * t;
                    camCX = hit.x; camCZ = hit.z;
                }
            }
        }

        // 씬 상태는 시스템 프롬프트에만 — 히스토리엔 쌓지 않아 컨텍스트 폭증 방지
        string system =
            "너는 한국 전통 건축 씬 편집 AI야. 사용자의 자연어 지시를 해석해 move_object, rotate_object, " +
            "delete_object, spawn_object 툴로 씬을 수정해.\n\n" +
            "규칙:\n" +
            "- id는 씬 상태의 id 값을 정확히 사용할 것\n" +
            "- spawn_object의 asset_key는 카탈로그에 있는 값만 사용할 것\n" +
            "- '동쪽'=+X, '서쪽'=-X, '남쪽'=-Z, '북쪽'=+Z\n" +
            "- 방향 표현('옮겨줘', '이동해줘')은 현재 좌표에서 delta를 계산해 새 절대 좌표로 변환할 것\n" +
            "- '그거', '방금 배치한 것' 등은 직전 작업 오브젝트를 지칭함\n" +
            $"- 위치를 명시하지 않고 spawn_object 할 때는 카메라 중심 좌표 x={camCX:F1}, z={camCZ:F1} 을 기본값으로 사용할 것\n\n" +
            "현재 씬 상태:\n" + BuildSceneState() + "\n\n" +
            "카탈로그:\n" + BuildSceneEditCatalog();

        // 히스토리엔 순수 대화만 (씬 상태 미포함)
        _sceneEditHistory.Add($"{{\"role\":\"user\",\"content\":{SceneJsonStr(userPrompt)}}}");

        // 사용자 말풍선 즉시 표시 (로딩 중에도 채팅 유지)
        _sceneChatLog.Add((userPrompt, "..."));
        ShowSceneChat();

        string body = BuildSceneEditRequestBody(system);
        byte[] raw  = Encoding.UTF8.GetBytes(body);

        using var www = new UnityWebRequest("https://api.anthropic.com/v1/messages", "POST");
        www.uploadHandler   = new UploadHandlerRaw(raw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.timeout         = 20;
        www.SetRequestHeader("content-type",      "application/json");
        www.SetRequestHeader("x-api-key",          apiKey);
        www.SetRequestHeader("anthropic-version", "2023-06-01");

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            // 마지막 항목의 bot 응답을 에러 메시지로 교체
            string errMsg = $"요청 실패 ({www.responseCode})";
            _sceneChatLog[_sceneChatLog.Count - 1] = (userPrompt, errMsg);
            _sceneEditHistory.RemoveAt(_sceneEditHistory.Count - 1);
            ShowSceneChat();
            _sceneObjCache = null;
            EndAIRequest();
            yield break;
        }

        string resp      = www.downloadHandler.text;
        Debug.Log($"[SceneEdit] response: {resp.Substring(0, Mathf.Min(resp.Length, 400))}");
        var    toolCalls = AgentExtractToolCalls(resp);

        string botMsg;
        if (toolCalls.Count == 0)
        {
            string textContent = AgentExtractTextContent(resp);
            botMsg = string.IsNullOrEmpty(textContent) ? "처리할 수 없는 요청입니다." : textContent;
            _sceneEditHistory.Add($"{{\"role\":\"assistant\",\"content\":{SceneJsonStr(botMsg)}}}");
        }
        else
        {
            bool dummy = false;
            var summaries = new List<string>();
            foreach (var tc in toolCalls)
                summaries.Add(ExecuteSceneEditTool(tc.name, tc.inputJson, ref dummy));
            botMsg = string.Join("\n", summaries);
            _sceneEditHistory.Add($"{{\"role\":\"assistant\",\"content\":{SceneJsonStr(botMsg)}}}");
        }

        // "..." 플레이스홀더를 실제 응답으로 교체
        _sceneChatLog[_sceneChatLog.Count - 1] = (userPrompt, botMsg);
        ShowSceneChat();

        _sceneObjCache = null;
        EndAIRequest();
    }

    // ── 툴 실행 ──────────────────────────────────────────────────────
    string ExecuteSceneEditTool(string toolName, string inputJson, ref bool finished)
    {
        switch (toolName)
        {
            case "move_object":
            {
                string id = SceneParseStr(inputJson, "id");
                float  x  = SceneParseFloat(inputJson, "x");
                float  z  = SceneParseFloat(inputJson, "z");
                var obj = FindById(id);
                if (obj == null) return $"오류: id={id} 오브젝트를 찾을 수 없음";
                var pos = obj.transform.position;
                obj.transform.position = new Vector3(x, pos.y, z);
                PlaceOnFloor(obj);
                return $"이동 완료: {obj.name} → ({x:F1}, {z:F1})";
            }
            case "rotate_object":
            {
                string id    = SceneParseStr(inputJson, "id");
                float  rotY  = SceneParseFloat(inputJson, "rot_y");
                var obj = FindById(id);
                if (obj == null) return $"오류: id={id} 오브젝트를 찾을 수 없음";
                var euler = obj.transform.eulerAngles;
                obj.transform.eulerAngles = new Vector3(euler.x, rotY, euler.z);
                return $"회전 완료: {obj.name} → rotY={rotY:F1}";
            }
            case "delete_object":
            {
                string id = SceneParseStr(inputJson, "id");
                var obj = FindById(id);
                if (obj == null) return $"오류: id={id} 오브젝트를 찾을 수 없음";
                string name = obj.name;
                Object.Destroy(obj);
                return $"삭제 완료: {name}";
            }
            case "spawn_object":
            {
                string assetKey = SceneParseStr(inputJson, "asset_key");
                float  x        = SceneParseFloat(inputJson, "x");
                float  z        = SceneParseFloat(inputJson, "z");
                float  rotY     = SceneParseFloat(inputJson, "rot_y");
                var entry = _assetEntries.Find(e => e.assetKey == assetKey || (e.prefab != null && e.prefab.name == assetKey));
                if (entry == null) return $"오류: assetKey={assetKey} 카탈로그에 없음";
                var obj = SpawnAt(entry, new Vector3(x, 0f, z));
                if (obj == null) return "오류: 생성 실패";
                obj.transform.eulerAngles = new Vector3(0f, rotY, 0f);
                PlaceOnFloor(obj);
                // 같은 요청 내에서 바로 참조할 수 있도록 캐시에 추가
                _sceneObjCache?.TryAdd(obj.GetInstanceID(), obj);
                return $"배치 완료: {entry.displayName} id={obj.GetInstanceID()} at ({x:F1},{z:F1})";
            }
            case "scale_object":
            {
                string id    = SceneParseStr(inputJson, "id");
                float  scale = SceneParseFloat(inputJson, "scale");
                scale = Mathf.Clamp(scale, 0.1f, 5f);
                var obj = FindById(id);
                if (obj == null) return $"오류: id={id} 오브젝트를 찾을 수 없음";
                obj.transform.localScale = Vector3.one * scale;
                return $"크기 조정 완료: {obj.name} → scale={scale:F2}";
            }
            case "move_relative":
            {
                string id = SceneParseStr(inputJson, "id");
                float  dx = SceneParseFloat(inputJson, "dx");
                float  dz = SceneParseFloat(inputJson, "dz");
                var obj = FindById(id);
                if (obj == null) return $"오류: id={id} 오브젝트를 찾을 수 없음";
                var pos = obj.transform.position;
                obj.transform.position = new Vector3(pos.x + dx, pos.y, pos.z + dz);
                PlaceOnFloor(obj);
                return $"상대이동 완료: {obj.name} → ({obj.transform.position.x:F1}, {obj.transform.position.z:F1})";
            }
            case "duplicate_object":
            {
                string id = SceneParseStr(inputJson, "id");
                float  dx = SceneParseFloat(inputJson, "dx");
                float  dz = SceneParseFloat(inputJson, "dz");
                var src = FindById(id);
                if (src == null) return $"오류: id={id} 오브젝트를 찾을 수 없음";
                var meta = src.GetComponent<HanokPlacedAssetMetadata>();
                if (meta == null) return "오류: 메타데이터 없음";
                var entry = _assetEntries.Find(e => e.assetKey == meta.assetKey || (e.prefab != null && e.prefab.name == meta.assetKey));
                if (entry == null) return $"오류: assetKey={meta.assetKey} 카탈로그에 없음";
                var srcPos = src.transform.position;
                var copy = SpawnAt(entry, new Vector3(srcPos.x + dx, 0f, srcPos.z + dz));
                if (copy == null) return "오류: 복사 실패";
                copy.transform.eulerAngles = src.transform.eulerAngles;
                copy.transform.localScale  = src.transform.localScale;
                PlaceOnFloor(copy);
                _sceneObjCache?.TryAdd(copy.GetInstanceID(), copy);
                return $"복사 완료: {meta.displayName} id={copy.GetInstanceID()} at ({copy.transform.position.x:F1},{copy.transform.position.z:F1})";
            }
            case "clear_scene":
            {
                var metas = Object.FindObjectsOfType<HanokPlacedAssetMetadata>();
                int count = metas.Length;
                foreach (var m in metas) Object.Destroy(m.gameObject);
                _sceneObjCache = null;
                return $"씬 초기화 완료: {count}개 오브젝트 제거";
            }
            default:
                return $"알 수 없는 툴: {toolName}";
        }
    }

    // ── 씬 편집 전용 헬퍼 ────────────────────────────────────────────
    GameObject FindById(string instanceId)
    {
        if (!int.TryParse(instanceId, out int id)) return null;
        if (_sceneObjCache != null && _sceneObjCache.TryGetValue(id, out var cached))
            return cached;
        // 캐시 없는 경우 폴백 (코루틴 외부 호출 시)
        foreach (var m in Object.FindObjectsOfType<HanokPlacedAssetMetadata>())
            if (m.gameObject.GetInstanceID() == id) return m.gameObject;
        return null;
    }

    string BuildSceneEditRequestBody(string system)
    {
        var msgs = string.Join(",", _sceneEditHistory);
        return
            "{\"model\":" + SceneJsonStr(GetApiModel()) + "," +
            "\"max_tokens\":1024," +
            "\"system\":" + SceneJsonStr(system) + "," +
            "\"tools\":" + SCENE_EDIT_TOOLS + "," +
            "\"tool_choice\":{\"type\":\"any\"}," +
            "\"messages\":[" + msgs + "]}";
    }

    // JSON 유틸 (AgentLayout의 JsonStr과 별칭 분리)
    static string SceneJsonStr(string s)
    {
        if (s == null) return "null";
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                        .Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
    }

    static string SceneParseStr(string json, string key)
    {
        string search = $"\"{key}\"";
        int ki = json.IndexOf(search, System.StringComparison.Ordinal);
        if (ki < 0) return "";
        int ci = json.IndexOf(':', ki + search.Length);
        if (ci < 0) return "";
        ci++;
        while (ci < json.Length && json[ci] == ' ') ci++;
        if (ci >= json.Length) return "";
        if (json[ci] == '"')
        {
            int end = json.IndexOf('"', ci + 1);
            return end < 0 ? "" : json.Substring(ci + 1, end - ci - 1);
        }
        int endN = ci;
        while (endN < json.Length && json[endN] != ',' && json[endN] != '}') endN++;
        return json.Substring(ci, endN - ci).Trim();
    }

    static float SceneParseFloat(string json, string key)
    {
        float.TryParse(SceneParseStr(json, key),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float v);
        return v;
    }

    // ── 씬 채팅 전용 패널 (기존 추천 패널과 별개) ────────────────────
    GameObject    _sceneChatPanel;
    RectTransform _sceneChatPanelRT;
    Transform     _sceneChatContainer;
    ScrollRect    _sceneChatScroll;

    void BuildSceneChatPanel()
    {
        var panelRT = NewRT(_canvasRT.transform, "SceneChatPanel");
        panelRT.anchorMin = new Vector2(0.5f, 0f);
        panelRT.anchorMax = new Vector2(0.5f, 0f);
        panelRT.pivot     = new Vector2(0.5f, 0f);
        panelRT.offsetMin = new Vector2(-320, 66);
        panelRT.offsetMax = new Vector2( 320, 380);

        var pImg = panelRT.GetComponent<Image>();
        pImg.sprite   = RoundedRectSprite(18f);
        pImg.type     = Image.Type.Sliced;
        pImg.color    = BG_PANEL;
        pImg.material = GlassMaterial();
        AddInnerGlow(panelRT, 18f);
        AddOuterBorder(panelRT, 18f);

        var scrollGO = new GameObject("VScroll");
        scrollGO.transform.SetParent(panelRT, false);
        var sRT = scrollGO.AddComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero; sRT.anchorMax = Vector2.one;
        sRT.offsetMin = new Vector2(0, 4); sRT.offsetMax = new Vector2(0, -4);
        scrollGO.AddComponent<Image>().color = Color.clear;
        var sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 40f;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        var vp = new GameObject("Viewport");
        vp.transform.SetParent(scrollGO.transform, false);
        var vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<RectMask2D>();

        var ct = new GameObject("Content");
        ct.transform.SetParent(vp.transform, false);
        var cRT = ct.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot     = new Vector2(0.5f, 1f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;

        var vlg = ct.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.padding = new RectOffset(14, 14, 10, 10);
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        ct.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT; sr.content = cRT;

        _sceneChatPanelRT   = panelRT;
        _sceneChatPanel     = panelRT.gameObject;
        _sceneChatContainer = ct.transform;
        _sceneChatScroll    = sr;

        _sceneChatPanel.SetActive(false);
    }

    // ── 씬 편집 모드 토글 ─────────────────────────────────────────────
    void SetSceneEditMode(bool on)
    {
        _sceneEditMode = on;
        if (_sceneEditBtnImg   != null) _sceneEditBtnImg.color   = on ? BTN_ACTIVE : BTN_GHOST;
        if (_sceneEditBtnLabel != null) _sceneEditBtnLabel.color  = on ? TEXT_ON_ACCENT : TEXT_MAIN;

        if (on)
        {
            SetLayoutMode(false);
            if (_aiResultsPanelRT != null) _aiResultsPanelRT.gameObject.SetActive(false);
            if (_sceneChatPanel == null) BuildSceneChatPanel();
            _sceneChatPanel.SetActive(true);
            ShowSceneChat();
        }
        else
        {
            // 히스토리 유지 — 패널만 숨김
            if (_sceneChatPanel != null) _sceneChatPanel.SetActive(false);
        }
    }

    // ── 채팅 로그 렌더 ────────────────────────────────────────────────
    void ShowSceneChat()
    {
        if (_sceneChatContainer == null) return;

        foreach (Transform ch in _sceneChatContainer)
            Object.Destroy(ch.gameObject);

        foreach (var (u, b) in _sceneChatLog)
        {
            AddChatBubble(u, isUser: true);
            AddChatBubble(b, isUser: false);
        }

        StartCoroutine(ScrollChatToBottom());
    }

    void AddChatBubble(string text, bool isUser)
    {
        // 행: 가로 꽉 참 (VLG childForceExpandWidth=true 덕분에 자동)
        var row = new GameObject(isUser ? "UserRow" : "BotRow");
        row.transform.SetParent(_sceneChatContainer, false);
        var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
        rowHLG.childForceExpandWidth  = false;
        rowHLG.childForceExpandHeight = false;
        rowHLG.padding = new RectOffset(0, 0, 0, 0);

        // 유저: 스페이서 먼저 → 버블이 오른쪽
        if (isUser) ChatSpacer(row.transform);

        // 말풍선 (패널 폭 640px, 약 55% = 350px)
        var bubble = new GameObject("Bubble");
        bubble.transform.SetParent(row.transform, false);
        var bImg = bubble.AddComponent<Image>();
        bImg.sprite = RoundedRectSprite(12f);
        bImg.type   = Image.Type.Sliced;
        bImg.color  = isUser
            ? BTN_ACTIVE
            : new Color(0.20f, 0.20f, 0.26f, 0.92f);

        var bLE = bubble.AddComponent<LayoutElement>();
        bLE.preferredWidth = 350;
        bLE.flexibleWidth  = 0;

        var bCSF = bubble.AddComponent<ContentSizeFitter>();
        bCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bVLG = bubble.AddComponent<VerticalLayoutGroup>();
        bVLG.padding = new RectOffset(12, 12, 8, 8);
        bVLG.childForceExpandWidth  = true;
        bVLG.childForceExpandHeight = false;

        var tGO = new GameObject("Text");
        tGO.transform.SetParent(bubble.transform, false);
        tGO.AddComponent<LayoutElement>().minHeight = 14;
        var t = tGO.AddComponent<TextMeshProUGUI>();
        t.text  = text;
        t.fontSize = 10;
        t.color = TEXT_ON_ACCENT; // 두 버블 모두 흰색 (유저=남색, AI=어두운 배경)
        t.textWrappingMode = TextWrappingModes.Normal;
        if (HasKorean(text)) KorFont(t); else LatFont(t);

        // AI: 버블 먼저 → 스페이서가 오른쪽
        if (!isUser) ChatSpacer(row.transform);
    }

    void ChatSpacer(Transform parent)
    {
        var s = new GameObject("Spacer");
        s.transform.SetParent(parent, false);
        s.AddComponent<LayoutElement>().flexibleWidth = 1;
    }

    IEnumerator ScrollChatToBottom()
    {
        yield return null;
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            _sceneChatContainer.GetComponent<RectTransform>());
        if (_sceneChatScroll != null)
            _sceneChatScroll.verticalNormalizedPosition = 0f;
    }

    public void ClearSceneEditHistory()
    {
        _sceneEditHistory.Clear();
        _sceneChatLog.Clear();
    }
}
