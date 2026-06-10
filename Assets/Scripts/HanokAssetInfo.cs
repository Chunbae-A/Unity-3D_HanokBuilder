using UnityEngine;

[CreateAssetMenu(menuName = "HanokBuilder/Asset Info", fileName = "AssetInfo_New")]
public class HanokAssetInfo : ScriptableObject
{
    public string assetKey;     // 프리팹 이름과 매칭되는 키 (영문 ID, Claude API 등에 그대로 전달)
    public string displayName;  // 패널에 표시할 한글 이름
    public string[] tags;       // 추가 검색어/동의어 (예: "들보", "수원화성")
}
