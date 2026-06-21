using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HanokUIManager — Claude Tool Use 기반 에이전트 맵 레이아웃 (partial)
/// Claude가 place_building / place_natural / finish 툴을 직접 호출하며 씬을 단계별로 설계한다.
/// </summary>
public partial class HanokUIManager
{
    // ── 툴 정의 JSON ─────────────────────────────────────────────
    const string AGENT_TOOLS =
        "[" +
        "{\"name\":\"place_building\"," +
        "\"description\":\"완성형 한옥 건물을 씬에 배치합니다. 건물 크기는 약 10~16m이므로 중심 간 최소 18m 이상 확보하세요.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"assetKey\":{\"type\":\"string\",\"description\":\"카탈로그의 prefab 이름 (정확히 일치)\"}," +
        "\"x\":{\"type\":\"number\",\"description\":\"X 좌표. 건물은 반드시 -20 ~ +20 이내\"}," +
        "\"z\":{\"type\":\"number\",\"description\":\"Z 좌표. 건물은 반드시 -20 ~ +20 이내\"}," +
        "\"rotY\":{\"type\":\"number\",\"description\":\"Y축 회전 (0=남향, 90=서향, 180=북향, 270=동향)\"}," +
        "\"reason\":{\"type\":\"string\",\"description\":\"이 위치/방향으로 배치한 건축적·문화적 이유\"}" +
        "},\"required\":[\"assetKey\",\"x\",\"z\",\"rotY\",\"reason\"],\"additionalProperties\":false}}," +

        "{\"name\":\"place_natural\"," +
        "\"description\":\"자연물(나무, 바위, 덤불 등)을 씬에 배치합니다. 크기 약 3~6m.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"assetKey\":{\"type\":\"string\",\"description\":\"카탈로그의 prefab 이름 (정확히 일치)\"}," +
        "\"x\":{\"type\":\"number\",\"description\":\"X 좌표. 자연물은 반드시 -27 ~ +27 이내\"}," +
        "\"z\":{\"type\":\"number\",\"description\":\"Z 좌표. 자연물은 반드시 -27 ~ +27 이내\"}," +
        "\"rotY\":{\"type\":\"number\",\"description\":\"Y축 회전 (0~360)\"}" +
        "},\"required\":[\"assetKey\",\"x\",\"z\",\"rotY\"],\"additionalProperties\":false}}," +

        "{\"name\":\"finish\"," +
        "\"description\":\"모든 배치가 완료됐을 때 반드시 호출합니다.\"," +
        "\"input_schema\":{\"type\":\"object\",\"properties\":{" +
        "\"summary\":{\"type\":\"string\",\"description\":\"전체 배치에 대한 한국 건축 문화 관점의 해설 (2~4문장, 한국어)\"}" +
        "},\"required\":[\"summary\"],\"additionalProperties\":false}}" +
        "]";

    // ── DTO ──────────────────────────────────────────────────────
    [System.Serializable]
    class AgentToolInput
    {
        public string assetKey;
        public float  x, z, rotY;
        public string reason, summary;
    }

    struct AgentToolCall { public string id, name, inputJson; }

    // ── 에셋 카탈로그 빌드 ────────────────────────────────────────
    string BuildAutoLayoutCatalog()
    {
        var buildings = new List<string>();
        var naturals  = new List<string>();

        foreach (var e in _assetEntries)
        {
            bool isComplete = false, isNatural = false;
            foreach (var c in e.categories)
            {
                if (c.key == "Complete") isComplete = true;
                if (c.key == "Natural")  isNatural  = true;
            }
            string line = $"{e.prefab.name}: {e.displayName}";
            if (isComplete) buildings.Add(line);
            if (isNatural)  naturals.Add(line);
        }

        var sb = new StringBuilder();
        sb.AppendLine("【완성형 건물】"); buildings.ForEach(l => sb.AppendLine(l));
        sb.AppendLine("【자연물】");     naturals.ForEach(l  => sb.AppendLine(l));
        return sb.ToString();
    }

    // ── 시스템 프롬프트 ───────────────────────────────────────────
    string BuildAgentSystemPrompt(string catalog) =>
        "너는 한국 전통 건축 전문가 AI야. place_building과 place_natural 툴을 사용해 씬을 단계적으로 설계해.\n" +
        "배치할 때마다 reason 필드에 건축적·문화적 이유를 기술하고, 완료 후 반드시 finish()를 호출해.\n\n" +

        "【설계 순서】\n" +
        "1. 사용자 요청 분석: 테마·규모·밀도 파악\n" +
        "2. 주요 건물부터 배치 (중심 → 주변 순서)\n" +
        "3. 부속 건물 배치 (마당·골목 형성)\n" +
        "4. 자연물로 외곽·빈 공간 채우기\n" +
        "5. finish() 호출\n\n" +

        "【밀도 기준】\n" +
        "  조용한/소규모 → 건물 2~4개, 자연물 6~10개\n" +
        "  일반 마을 → 건물 5~7개, 자연물 8~12개\n" +
        "  밀도 높은/번화한 → 건물 10~14개, 자연물 12~18개\n" +
        "  대규모/궁궐 → 건물 13~18개, 자연물 15~25개\n\n" +

        "【테마】\n" +
        "  사찰: 주불전 중앙·종루 주변·소나무 연못 많이\n" +
        "  관아: 대문-중문-내당 일직선 대칭\n" +
        "  양반가: 사랑채 앞·안채 안쪽·후원에 자연물\n" +
        "  서원: 강당 중앙·소나무 조용한 분위기\n" +
        "  시장: 건물 밀집·다양한 방향\n" +
        "  정원: 자연물 최대·정자 위주\n" +
        "  마을: 골목 형성·자연스럽게 분산\n\n" +

        "겹침 발생 시 tool_result에 다른 좌표 시도 요청이 오면 즉시 수정 배치할 것.\n" +
        "카탈로그에 없는 assetKey 절대 사용 금지.\n\n" +
        "카탈로그:\n" + catalog;

    // ── 메인 코루틴 ──────────────────────────────────────────────
    IEnumerator RequestAIAgentLayout(string userPrompt)
    {
        string apiKey = GetSavedApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            ShowApiKeyPanel();
            EndAIRequest();
            yield break;
        }

        string system = BuildAgentSystemPrompt(BuildAutoLayoutCatalog());

        var messages = new List<string>
        {
            $"{{\"role\":\"user\",\"content\":{JsonStr(userPrompt)}}}"
        };

        var    occupied = new List<(Vector2 pos, float radius)>();
        int    placed   = 0;
        string summary  = "";
        bool   finished = false;

        const int MAX_TURNS = 20;

        for (int turn = 0; turn < MAX_TURNS && !finished; turn++)
        {
            ShowAIMessage("설계 중...");

            string body = BuildAgentRequestBody(system, messages);
            byte[] raw  = Encoding.UTF8.GetBytes(body);

            using var www = new UnityWebRequest("https://api.anthropic.com/v1/messages", "POST");
            www.uploadHandler   = new UploadHandlerRaw(raw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("content-type",      "application/json");
            www.SetRequestHeader("x-api-key",          apiKey);
            www.SetRequestHeader("anthropic-version", "2023-06-01");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ShowAIMessage($"요청 실패 ({www.responseCode}): {www.error}");
                EndAIRequest();
                yield break;
            }

            string resp       = www.downloadHandler.text;
            string stopReason = AgentExtractStr(resp, "stop_reason");
            var    toolCalls  = AgentExtractToolCalls(resp);

            // 어시스턴트 메시지를 히스토리에 추가 (원본 content 배열 보존 필수)
            string assistantContent = AgentExtractRawContent(resp);
            messages.Add($"{{\"role\":\"assistant\",\"content\":{assistantContent}}}");

            if (toolCalls.Count == 0 || stopReason == "end_turn")
                break;

            // 툴 실행 → 결과 수집
            bool   hadOverlap  = false;
            var    toolResults = new List<string>();
            foreach (var tc in toolCalls)
            {
                var (apiResult, isOverlap) = ExecuteAgentTool(tc.name, tc.inputJson,
                    ref occupied, ref placed, ref summary, ref finished);

                toolResults.Add(
                    $"{{\"type\":\"tool_result\",\"tool_use_id\":{JsonStr(tc.id)},\"content\":{JsonStr(apiResult)}}}");

                if (isOverlap) hadOverlap = true;
            }

            if (hadOverlap) ShowAIMessage("수정 중...");

            // 툴 결과를 user 메시지로 추가
            messages.Add($"{{\"role\":\"user\",\"content\":[{string.Join(",", toolResults)}]}}");
        }

        // 완료 표시
        if (placed == 0)
            ShowAIMessage("배치할 오브젝트를 찾지 못했습니다.");
        else if (!string.IsNullOrEmpty(summary))
            ShowAIMessage($"완성!\n\n{summary}");
        else
            ShowAIMessage($"완성!  ({placed}개 배치됨)");

        EndAIRequest();
    }

    // ── 툴 실행 — (API에 돌려줄 결과 문자열, 겹침 여부) ─────────
    (string apiResult, bool isOverlap) ExecuteAgentTool(
        string toolName, string inputJson,
        ref List<(Vector2 pos, float radius)> occupied,
        ref int placed, ref string summary, ref bool finished)
    {
        AgentToolInput inp;
        try { inp = JsonUtility.FromJson<AgentToolInput>(inputJson); }
        catch { return ("입력 파싱 실패", false); }

        if (toolName == "finish")
        {
            summary  = inp.summary ?? "";
            finished = true;
            return ("완료", false);
        }

        bool  isBuilding = toolName == "place_building";
        float maxR = isBuilding ? 20f : 27f;
        float cx   = Mathf.Clamp(inp.x, -maxR, maxR);
        float cz   = Mathf.Clamp(inp.z, -maxR, maxR);

        var entry = _assetEntries.Find(
            e => e.prefab.name == inp.assetKey || e.assetKey == inp.assetKey);
        if (entry == null)
            return ($"카탈로그에 없는 에셋: {inp.assetKey}", false);

        var obj = SpawnAt(entry, new Vector3(cx, 0f, cz));
        if (obj == null) return ("스폰 실패", false);

        obj.transform.eulerAngles = new Vector3(0f, inp.rotY, 0f);
        PlaceOnFloor(obj);

        // Renderer bounds 기반 겹침 감지
        var   rends   = obj.GetComponentsInChildren<Renderer>();
        var   center2 = new Vector2(cx, cz);
        float radius  = isBuilding ? 8f : 3f;
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            center2 = new Vector2(b.center.x, b.center.z);
            radius  = Mathf.Max(b.extents.x, b.extents.z);
        }

        foreach (var (oPos, oRadius) in occupied)
        {
            if (Vector2.Distance(center2, oPos) < radius + oRadius + 0.3f)
            {
                Object.Destroy(obj);
                return ($"위치 겹침 취소: {inp.assetKey} at ({cx:F0},{cz:F0}). 다른 좌표를 선택하세요.", true);
            }
        }

        occupied.Add((center2, radius));
        placed++;
        SelectObject(obj);
        return ($"배치 완료: {inp.assetKey} at ({cx:F1},{cz:F1}) rotY={inp.rotY}", false);
    }

    // ── Request 빌드 ──────────────────────────────────────────────
    string BuildAgentRequestBody(string system, List<string> messages)
    {
        string messagesArr = "[" + string.Join(",", messages) + "]";
        return
            "{" +
            $"\"model\":{JsonStr(GetApiModel())}," +
            "\"max_tokens\":4096," +
            $"\"system\":{JsonStr(system)}," +
            $"\"tools\":{AGENT_TOOLS}," +
            $"\"messages\":{messagesArr}" +
            "}";
    }

    // ── JSON 유틸리티 ─────────────────────────────────────────────
    static string JsonStr(string s)
    {
        if (s == null) return "null";
        var sb = new StringBuilder("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:   sb.Append(c);      break;
            }
        }
        return sb.Append('"').ToString();
    }

    // "key":"value" 에서 값 추출
    static string AgentExtractStr(string json, string key)
    {
        string pat = $"\"{key}\":\"";
        int pos = json.IndexOf(pat);
        if (pos < 0) return "";
        pos += pat.Length;
        var sb = new StringBuilder();
        while (pos < json.Length && json[pos] != '"')
        {
            if (json[pos] == '\\' && pos + 1 < json.Length) { pos++; sb.Append(json[pos]); }
            else sb.Append(json[pos]);
            pos++;
        }
        return sb.ToString();
    }

    // 응답에서 tool_use 블록 전체 추출
    static List<AgentToolCall> AgentExtractToolCalls(string json)
    {
        var list   = new List<AgentToolCall>();
        int search = 0;
        while (true)
        {
            int idx = json.IndexOf("\"type\":\"tool_use\"", search);
            if (idx < 0) break;

            int blockStart = json.LastIndexOf('{', idx);
            if (blockStart < 0) break;
            string block = AgentExtractObj(json, blockStart);
            if (block == null) break;

            string id   = AgentExtractStr(block, "id");
            string name = AgentExtractStr(block, "name");

            int    inputIdx = block.IndexOf("\"input\":");
            string inputJ   = "{}";
            if (inputIdx >= 0)
                inputJ = AgentExtractObj(block, inputIdx + "\"input\":".Length) ?? "{}";

            if (!string.IsNullOrEmpty(name))
                list.Add(new AgentToolCall { id = id, name = name, inputJson = inputJ });

            search = idx + 1;
        }
        return list;
    }

    // 응답의 content 배열 원문 추출 (어시스턴트 히스토리 재구성에 필수)
    static string AgentExtractTextContent(string json)
    {
        // content 배열에서 type=text 블록의 text 값만 추출
        int idx = json.IndexOf("\"content\":");
        if (idx < 0) return "";
        int arrStart = json.IndexOf('[', idx);
        if (arrStart < 0) return "";
        string arr = AgentExtractArr(json, arrStart);
        int pos = 0;
        var sb = new StringBuilder();
        while (pos < arr.Length)
        {
            int ob = arr.IndexOf('{', pos);
            if (ob < 0) break;
            string block = AgentExtractObj(arr, ob);
            if (block == null) break;
            pos = ob + block.Length;
            if (AgentExtractStr(block, "type") == "text")
            {
                string t = AgentExtractStr(block, "text");
                if (!string.IsNullOrEmpty(t)) { if (sb.Length > 0) sb.Append('\n'); sb.Append(t); }
            }
        }
        return sb.ToString();
    }

    static string AgentExtractRawContent(string json)
    {
        int idx = json.IndexOf("\"content\":");
        if (idx < 0) return "[]";
        int arrStart = json.IndexOf('[', idx);
        return arrStart < 0 ? "[]" : AgentExtractArr(json, arrStart);
    }

    static string AgentExtractObj(string json, int from)
    {
        int start = json.IndexOf('{', from);
        if (start < 0) return null;
        int depth = 0; bool inStr = false;
        var sb = new StringBuilder();
        for (int i = start; i < json.Length; i++)
        {
            char c = json[i]; sb.Append(c);
            if (inStr)
            {
                if      (c == '\\') { i++; if (i < json.Length) sb.Append(json[i]); }
                else if (c == '"')  inStr = false;
            }
            else
            {
                if      (c == '"') inStr = true;
                else if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) break; }
            }
        }
        return depth == 0 ? sb.ToString() : null;
    }

    static string AgentExtractArr(string json, int from)
    {
        int start = json.IndexOf('[', from);
        if (start < 0) return "[]";
        int depth = 0; bool inStr = false;
        var sb = new StringBuilder();
        for (int i = start; i < json.Length; i++)
        {
            char c = json[i]; sb.Append(c);
            if (inStr)
            {
                if      (c == '\\') { i++; if (i < json.Length) sb.Append(json[i]); }
                else if (c == '"')  inStr = false;
            }
            else
            {
                if      (c == '"') inStr = true;
                else if (c == '[') depth++;
                else if (c == ']') { depth--; if (depth == 0) break; }
            }
        }
        return depth == 0 ? sb.ToString() : "[]";
    }
}
