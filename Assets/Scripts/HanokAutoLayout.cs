using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HanokUIManager — AI 자동 레이아웃 생성 (partial)
/// 자연어 입력 → Claude가 완성형 건물 + 자연물 배치 좌표 JSON 반환 → 씬 자동 배치
/// </summary>
public partial class HanokUIManager
{
    // ── JSON Schema ───────────────────────────────────────────────
    const string AUTOLAYOUT_SCHEMA =
        "{\"type\":\"object\",\"properties\":{" +
        "\"placements\":{\"type\":\"array\",\"items\":{" +
        "\"type\":\"object\",\"properties\":{" +
        "\"assetKey\":{\"type\":\"string\"}," +
        "\"x\":{\"type\":\"number\"}," +
        "\"z\":{\"type\":\"number\"}," +
        "\"rotY\":{\"type\":\"number\"}" +
        "},\"required\":[\"assetKey\",\"x\",\"z\",\"rotY\"]," +
        "\"additionalProperties\":false}}}," +
        "\"required\":[\"placements\"],\"additionalProperties\":false}";

    // ── 완성형 + 자연물 카탈로그 ──────────────────────────────────
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
        sb.AppendLine("【자연물】");     naturals.ForEach(l => sb.AppendLine(l));
        return sb.ToString();
    }

    // ── Claude API 호출 ───────────────────────────────────────────
    IEnumerator RequestAIAutoLayout(string userPrompt)
    {
        string apiKey = GetSavedApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            ShowApiKeyPanel();
            EndAIRequest();
            yield break;
        }

        string instruction =
            "너는 한국 전통 마을 게임 맵 레벨 디자이너야.\n" +
            "사용자 요청에서 테마·분위기·규모·밀도·특정 건물 요구를 모두 읽어내서, 실제 게임 맵처럼 몰입감 있고 자연스러운 배치 JSON을 반환해.\n\n" +

            "▶ 에셋 실제 크기 (이걸 모르면 겹침 발생)\n" +
            "  완성형 건물(Complete): 가로·세로 약 10~16m. 건물 중심 간 최소 18m 이상 확보\n" +
            "  자연물(Natural): 약 3~6m. 자연물끼리 최소 5m, 건물과는 최소 8m\n\n" +

            "▶ 배치 가능 범위\n" +
            "  건물: x·z 모두 -20 ~ +20 이내만 사용\n" +
            "  자연물: x·z 모두 -27 ~ +27 이내만 사용\n\n" +

            "▶ 밀도·규모 해석 (요청 문맥에서 판단)\n" +
            "  조용한/한적한/소규모/한두채 → 건물 2~4개, 자연물 8~12개\n" +
            "  일반/보통 마을 → 건물 5~7개, 자연물 10~15개\n" +
            "  번화한/밀도 높은/붐비는/복잡한 → 건물 10~14개, 자연물 12~20개\n" +
            "  대규모/단지/도성/궁궐/전국 → 건물 13~18개, 자연물 15~25개\n\n" +

            "▶ 공간 구성 원칙\n" +
            "  중심부에 주요 건물, 그 주변에 부속 건물, 외곽·빈 공간에 자연물로 테두리\n" +
            "  건물은 마당·광장·진입로를 기준으로 그룹화 (ㄱ자·ㄷ자·ㅁ자 배치 활용)\n" +
            "  자연물은 건물 사이 빈 곳, 진입로 양옆, 맵 가장자리에 고르게 배치\n" +
            "  특정 건물 이름이 요청에 등장하면 해당 assetKey를 반드시 포함시켜\n\n" +

            "▶ 건물 방향 (rotY)\n" +
            "  기본 남향=0. 마당 중심 배치 시 ㄷ자: 앞=0, 좌=90, 우=270\n" +
            "  시장처럼 혼잡한 경우 다양한 각도 혼용\n" +

            "▶ 테마별 특성\n" +
            "  사찰/절: 주불전 중앙, 종루·요사채 주변, 소나무·연못 자연물 많이, 정적·엄숙한 배치\n" +
            "  관아/동헌: 대문-중문-내당 일직선 대칭, 위계 명확, 나무는 좌우 대칭으로\n" +
            "  양반가/한옥: 사랑채 앞쪽, 안채 안쪽, 행랑채 옆, 후원에 자연물\n" +
            "  서원/서당: 강당 중앙, 동재·서재 양옆, 소나무·느티나무 많이, 조용한 분위기\n" +
            "  시장/장터: 건물 촘촘히 밀집, 다양한 방향, 사람 통로 형태로 빈 공간 확보\n" +
            "  왕궁/궁궐: 대규모, 정전 중심 대칭, 행각 둘러싸기, 웅장한 규모\n" +
            "  마을/민가: 자연스럽게 흩어진 배치, 좁은 골목길 형태, 텃밭 느낌 자연물\n" +
            "  산사/암자: 건물 2~4개 소규모, 바위·소나무 많이, 산속 고요함\n" +
            "  주막/객주: 건물 2~3개 중심, 나무 그늘 느낌, 길가 배치\n" +
            "  농촌: 건물 분산 배치, 들판 느낌, 나무 군데군데\n" +
            "  정원/원림: 자연물 최대한 많이, 건물은 정자·누각 위주\n" +
            "  축제/연회: 건물 광장 둘러싸기, 개방된 중심부, 화려한 느낌\n\n" +

            "▶ 기타\n" +
            "  같은 assetKey 여러 번 다른 위치에 반복 가능\n" +
            "  카탈로그에 없는 assetKey 절대 사용 금지\n\n" +
            "카탈로그:\n" + BuildAutoLayoutCatalog() +
            "\n사용자 요청: " + userPrompt;

        var reqBody = new ClaudeRequest
        {
            model      = GetApiModel(),
            max_tokens = 4096,
            messages   = new[] { new ClaudeMessage { role = "user", content = instruction } }
        };

        string baseJson = JsonUtility.ToJson(reqBody);
        string bodyJson = baseJson[..^1] +
            ",\"output_config\":{\"format\":{\"type\":\"json_schema\",\"schema\":" + AUTOLAYOUT_SCHEMA + "}}}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

        using var www = new UnityWebRequest("https://api.anthropic.com/v1/messages", "POST");
        www.uploadHandler   = new UploadHandlerRaw(bodyRaw);
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

        ClaudeResponse response = null;
        try { response = JsonUtility.FromJson<ClaudeResponse>(www.downloadHandler.text); }
        catch (System.Exception e) { Debug.LogError($"[AutoLayout] 응답 파싱 실패: {e.Message}"); }

        if (response == null || response.content == null || response.content.Length == 0)
        {
            ShowAIMessage("AI 응답을 해석하지 못했습니다.");
            EndAIRequest();
            yield break;
        }

        AutoLayoutSpec spec = null;
        try { spec = JsonUtility.FromJson<AutoLayoutSpec>(response.content[0].text); }
        catch (System.Exception e) { Debug.LogError($"[AutoLayout] 스펙 파싱 실패: {e.Message}"); }

        if (spec == null || spec.placements == null || spec.placements.Length == 0)
        {
            ShowAIMessage("배치 계획을 해석하지 못했습니다.");
            EndAIRequest();
            yield break;
        }

        ExecuteAutoLayout(spec);
        EndAIRequest();
    }

    // ── 배치 실행 ─────────────────────────────────────────────────
    void ExecuteAutoLayout(AutoLayoutSpec spec)
    {
        Vector3 center = Vector3.zero;
        int placed = 0;
        GameObject last = null;

        // 이미 배치된 오브젝트의 XZ 원형 영역 (중심, 반경) 추적
        var occupied = new List<(Vector2 pos, float radius)>();

        foreach (var p in spec.placements)
        {
            var entry = _assetEntries.Find(
                e => e.prefab.name == p.assetKey || e.assetKey == p.assetKey);
            if (entry == null)
            {
                Debug.LogWarning($"[AutoLayout] 알 수 없는 assetKey: {p.assetKey}");
                continue;
            }

            bool isBuilding = false;
            foreach (var c in entry.categories) if (c.key == "Complete") { isBuilding = true; break; }
            float maxR  = isBuilding ? 20f : 27f;
            float clampedX = Mathf.Clamp(p.x, -maxR, maxR);
            float clampedZ = Mathf.Clamp(p.z, -maxR, maxR);
            var worldPos = center + new Vector3(clampedX, 0f, clampedZ);

            var obj = SpawnAt(entry, worldPos);
            if (obj == null) continue;

            obj.transform.eulerAngles = new Vector3(0f, p.rotY, 0f);
            PlaceOnFloor(obj);

            // 실제 렌더러 bounds로 반경 측정
            var renderers = obj.GetComponentsInChildren<Renderer>();
            Vector2 objCenter;
            float objRadius;
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                objCenter = new Vector2(b.center.x, b.center.z);
                objRadius = Mathf.Max(b.extents.x, b.extents.z);
            }
            else
            {
                objCenter = new Vector2(worldPos.x, worldPos.z);
                objRadius = 1f;
            }

            // 기존 배치 오브젝트와 겹치는지 확인
            bool overlaps = false;
            foreach (var (oPos, oRadius) in occupied)
            {
                if (Vector2.Distance(objCenter, oPos) < objRadius + oRadius + 0.5f)
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                Object.Destroy(obj);
                Debug.Log($"[AutoLayout] 겹침 스킵: {p.assetKey} ({clampedX:F1}, {clampedZ:F1})");
                continue;
            }

            occupied.Add((objCenter, objRadius));
            last = obj;
            placed++;
        }

        if (last != null) SelectObject(last);
        ShowToast($"AI가 {placed}개 오브젝트를 배치했습니다.");
    }

    // ── DTOs ──────────────────────────────────────────────────────
    [System.Serializable]
    class AutoLayoutSpec { public PlacementItem[] placements; }

    [System.Serializable]
    class PlacementItem
    {
        public string assetKey;
        public float  x;
        public float  z;
        public float  rotY;
    }
}
