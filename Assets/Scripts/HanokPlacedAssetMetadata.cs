using UnityEngine;

/// <summary>
/// Runtime metadata copied onto placed assets so UI can show library names
/// instead of raw prefab/model names.
/// </summary>
public class HanokPlacedAssetMetadata : MonoBehaviour
{
    public string assetKey;
    public string prefabName;
    public string displayName;
}
