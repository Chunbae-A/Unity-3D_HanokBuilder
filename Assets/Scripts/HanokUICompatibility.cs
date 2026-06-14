using UnityEngine;

/// <summary>
/// Compatibility glue for partial UI files that were merged from different feature branches.
/// Keeps missing shared fields/constants in one place so existing UI files can stay mostly untouched.
/// </summary>
public partial class HanokUIManager
{
    const string ASSETINFO_PATH = "HanokAssetInfo";
    const string CATEGORY_PATH = "HanokCategories";

    RectTransform rightPanelRT;
    RectTransform viewportHintRT;
    RectTransform viewSwitcherRT;

    GameObject SpawnAt(GameObject prefab, Vector3 position)
    {
        var obj = Instantiate(prefab, position, Quaternion.Euler(-90f, 0f, 0f));
        obj.name = prefab.name;

        if (obj.transform.localScale.magnitude > 50f)
            obj.transform.localScale = Vector3.one;

        obj.transform.localScale = Vector3.one * 23f;
        obj.transform.position = new Vector3(position.x, 0f, position.z);

        OptimizeRenderers(obj);
        AttachSelectable(obj);
        PushUndoSpawn(obj);
        SelectObject(obj);

        var camCtrl = Camera.main?.GetComponent<HanokCameraController>();
        StartCoroutine(FinishSpawn(obj, camCtrl));
        return obj;
    }
}
