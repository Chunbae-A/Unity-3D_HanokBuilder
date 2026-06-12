using UnityEngine;

[CreateAssetMenu(menuName = "HanokBuilder/Asset Category", fileName = "Category_New")]
public class HanokAssetCategory : ScriptableObject
{
    public string key;
    public string label;
    public HanokAssetCategory parent;
    public int order;
}
