using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HanokUIManager — AI 문화재 해설 패널 (partial)
/// 씬에 배치된 건물 선택 시 Claude가 건축물의 역사·특징·시대를 자동 해설한다.
/// </summary>
public partial class HanokUIManager
{
    // ── 상태 ──────────────────────────────────────────────
    TMP_Text _guideBodyText;
    Image    _guideRequestBtnImg;
    bool     _guideRequestInProgress;
    string   _lastGuidedAssetKey;

    // ── UI 빌드: 오른쪽 편집 패널 하단에 해설 섹션 추가 ──
    void BuildGuideSection(Transform content)
    {
        Spacer(content, 14);
        Divider(content);
        Spacer(content, 4);
        InfoSectionLabel(content, "AI 문화재 해설");
        Spacer(content, 6);

        // 해설 생성 버튼 행
        var row = new GameObject("GuideRow");
        row.transform.SetParent(content, false);
        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 26; rowLE.flexibleWidth = 1;
        var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 8; rowHLG.padding = new RectOffset(12, 12, 0, 0);
        rowHLG.childForceExpandHeight = true; rowHLG.childForceExpandWidth = false;

        var hintGO = new GameObject("GuideHint");
        hintGO.transform.SetParent(row.transform, false);
        hintGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        var hintT = hintGO.AddComponent<TextMeshProUGUI>();
        hintT.text = "선택 건축물의 역사·특징 해설";
        hintT.fontSize = 8.5f; hintT.color = TEXT_HINT;
        hintT.alignment = TextAlignmentOptions.Left;
        KorFont(hintT);

        var btnGO = new GameObject("GuideReqBtn");
        btnGO.transform.SetParent(row.transform, false);
        btnGO.AddComponent<LayoutElement>().preferredWidth = 64;
        _guideRequestBtnImg = btnGO.AddComponent<Image>();
        _guideRequestBtnImg.sprite = RoundedRectSprite(6f);
        _guideRequestBtnImg.type = Image.Type.Sliced;
        _guideRequestBtnImg.color = BTN_GHOST;
        var reqBtn = btnGO.AddComponent<Button>();
        reqBtn.targetGraphic = _guideRequestBtnImg;
        var cs = reqBtn.colors;
        cs.highlightedColor = BTN_HOVER; cs.pressedColor = BTN_PRESS;
        reqBtn.colors = cs;
        reqBtn.onClick.AddListener(OnGuideRequestClicked);
        var btnLbl = (TextMeshProUGUI)MakeLabel(btnGO.transform, "해설 생성", 8.5f, TEXT_MAIN);
        var btnLblRT = btnLbl.GetComponent<RectTransform>();
        btnLblRT.anchorMin = Vector2.zero; btnLblRT.anchorMax = Vector2.one;
        btnLblRT.offsetMin = btnLblRT.offsetMax = Vector2.zero;
        KorFont(btnLbl);

        Spacer(content, 6);

        // 해설 텍스트 카드 (ContentSizeFitter로 높이 자동 조절)
        var cardGO = new GameObject("GuideCard");
        cardGO.transform.SetParent(content, false);
        cardGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        var cardImg = cardGO.AddComponent<Image>();
        cardImg.sprite = RoundedRectSprite(8f);
        cardImg.type = Image.Type.Sliced;
        cardImg.color = BG_CARD;
        cardImg.material = GlassMaterial();
        AddInnerGlow(cardGO, 8f);
        cardGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var cardVLG = cardGO.AddComponent<VerticalLayoutGroup>();
        cardVLG.padding = new RectOffset(12, 12, 10, 10);
        cardVLG.childForceExpandWidth = true;
        cardVLG.childForceExpandHeight = false;

        var textGO = new GameObject("GuideText");
        textGO.transform.SetParent(cardGO.transform, false);
        textGO.AddComponent<LayoutElement>().flexibleWidth = 1;
        textGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _guideBodyText = textGO.AddComponent<TextMeshProUGUI>();
        _guideBodyText.text = "에셋을 선택하면 AI가 해당 건축물의\n역사와 특징을 해설합니다.";
        _guideBodyText.fontSize = 9.5f;
        _guideBodyText.color = TEXT_HINT;
        _guideBodyText.alignment = TextAlignmentOptions.Left;
        _guideBodyText.textWrappingMode = TextWrappingModes.Normal;
        _guideBodyText.overflowMode = TextOverflowModes.Overflow;
        KorFont(_guideBodyText);

        Spacer(content, 24);
    }

    // ── 이벤트 핸들러 ──────────────────────────────────────
    void OnGuideRequestClicked()
    {
        if (selectedObject == null)
        { SetGuideText("에셋을 먼저 선택해주세요.", isHint: true); return; }
        RequestGuideForObject(selectedObject, forceRefresh: true);
    }

    // SelectObject()에서 호출 — 새 에셋 선택 시 자동 해설
    void TriggerAutoGuide(GameObject obj)
    {
        if (obj == null) { ClearGuide(); return; }
        RequestGuideForObject(obj, forceRefresh: false);
    }

    void RequestGuideForObject(GameObject obj, bool forceRefresh)
    {
        if (_guideBodyText == null || _guideRequestInProgress) return;

        var meta = obj.GetComponent<HanokPlacedAssetMetadata>();
        string assetKey = !string.IsNullOrEmpty(meta?.assetKey)
            ? meta.assetKey : CleanPlacedObjectName(obj.name);

        // 같은 에셋이면 재요청 안 함 (forceRefresh=true면 항상 요청)
        if (!forceRefresh && assetKey == _lastGuidedAssetKey) return;

        string displayName = !string.IsNullOrEmpty(meta?.displayName)
            ? meta.displayName : assetKey;

        // 라이브러리에서 카테고리 레이블 조회
        string categoryLabel = "";
        if (_assetEntries != null)
        {
            string cleanedName = CleanPlacedObjectName(obj.name);
            var entry = _assetEntries.Find(e =>
                (e.prefab != null && e.prefab.name == cleanedName) || e.assetKey == assetKey);
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
            SetGuideText("⚙ 버튼에서 Claude API 키를\n설정하면 해설이 활성화됩니다.", isHint: true);
            return;
        }

        _lastGuidedAssetKey = assetKey;
        StartCoroutine(RequestGuideCo(assetKey, displayName, categoryLabel));
    }

    IEnumerator RequestGuideCo(string assetKey, string displayName, string categoryLabel)
    {
        _guideRequestInProgress = true;
        SetGuideText("해설을 불러오는 중...", isHint: true);
        if (_guideRequestBtnImg != null) _guideRequestBtnImg.color = BTN_ACTIVE;

        var sb = new StringBuilder();
        sb.Append("한국 전통 건축물: ").AppendLine(displayName);
        if (!string.IsNullOrEmpty(categoryLabel))
            sb.Append("카테고리: ").AppendLine(categoryLabel);
        sb.Append("에셋 ID: ").AppendLine(assetKey);
        sb.AppendLine();
        sb.AppendLine("위 한국 전통 건축물에 대해 역사적 배경(시대), 건축 구조적 특징, 문화적 의미를 2~3문장으로 간결하게 한국어로 설명해줘.");

        string bodyJson =
            "{\"model\":" + JsonStr(GetApiModel()) +
            ",\"max_tokens\":512" +
            ",\"messages\":[{\"role\":\"user\",\"content\":" + JsonStr(sb.ToString()) + "}]}";

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
            Debug.LogError($"[GuideAPI] {www.responseCode} {www.error}\n{www.downloadHandler.text}");
            SetGuideText($"요청 실패 ({www.responseCode})\n자세한 내용은 콘솔 확인", isHint: true);
            yield break;
        }

        ClaudeResponse resp = null;
        try { resp = JsonUtility.FromJson<ClaudeResponse>(www.downloadHandler.text); }
        catch (System.Exception e) { Debug.LogError($"[GuideAPI] 파싱 실패: {e.Message}\n{www.downloadHandler.text}"); SetGuideText("응답 파싱 실패", isHint: true); yield break; }

        if (resp?.content == null || resp.content.Length == 0)
        { SetGuideText("응답 없음", isHint: true); yield break; }

        SetGuideText(resp.content[0].text, isHint: false);
    }

    void SetGuideText(string text, bool isHint)
    {
        if (_guideBodyText == null) return;
        _guideBodyText.text  = text;
        _guideBodyText.color = isHint ? TEXT_HINT : TEXT_MAIN;
    }

    void ClearGuide()
    {
        _lastGuidedAssetKey = null;
        SetGuideText("에셋을 선택하면 AI가 해당 건축물의\n역사와 특징을 해설합니다.", isHint: true);
    }
}
