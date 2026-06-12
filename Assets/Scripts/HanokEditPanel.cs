using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — 오른쪽 패널: AI 프롬프트 입력 + AI 에셋 추천 (partial)
/// </summary>
public partial class HanokUIManager
{
    void BuildEditPanel(Transform content)
    {
        // ── AI 프롬프트 입력 바 ────────────────────────────
        BuildAIInputBar(content);

        // ── AI 에셋 추천 ──────────────────────────────────
        Spacer(content, 8);
        BuildAIRecommendationSection(content);

        Spacer(content, 12);
    }

    // ── 섹션 라벨 ─────────────────────────────────────────
    void InfoSectionLabel(Transform parent, string kor)
    {
        var go = new GameObject("SecLbl");
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 26; le.flexibleWidth = 1;
        go.AddComponent<Image>().color = BG_ROOT;
        var tgo = new GameObject("T"); tgo.transform.SetParent(go.transform, false);
        var tRT = tgo.AddComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(12, 0); tRT.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = kor; t.fontSize = 10; t.fontStyle = FontStyles.Bold;
        t.color = NAVY; t.alignment = TextAlignmentOptions.Left;
        KorFont(t);
    }
}
