using UnityEngine;

/// <summary>
/// 선택된 오브젝트에 외곽선(outline) 효과를 주는 컴포넌트.
/// 약간 크게 스케일한 단색 메시를 뒤에 겹쳐서 글로우 효과를 냄.
/// </summary>
public class HanokSelectionHighlight : MonoBehaviour
{
    static readonly int ColorProp = Shader.PropertyToID("_Color");
    static readonly int ZWriteProp = Shader.PropertyToID("_ZWrite");

    GameObject _outlineRoot;

    public void Show()
    {
        if (_outlineRoot != null) return;

        _outlineRoot = new GameObject("_Outline");
        _outlineRoot.transform.SetParent(transform, false);
        _outlineRoot.hideFlags = HideFlags.HideAndDontSave;

        var srcRends = GetComponentsInChildren<MeshRenderer>();
        foreach (var r in srcRends)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var child = new GameObject("ol_" + r.name);
            child.transform.SetParent(_outlineRoot.transform, false);
            child.transform.localPosition = r.transform.localPosition;
            child.transform.localRotation = r.transform.localRotation;
            child.transform.localScale    = r.transform.localScale * 1.04f;

            var cf = child.AddComponent<MeshFilter>();
            cf.sharedMesh = mf.sharedMesh;

            var cr = child.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Standard"));

            // 뒷면만 렌더해서 앞면 원본 가리지 않게
            mat.SetFloat("_Cull", 1f);       // Front = 1 (뒷면만)
            mat.color = new Color(0.2f, 0.7f, 1f, 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.1f, 0.4f, 0.8f));
            cr.material = mat;
            cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cr.receiveShadows = false;
        }
    }

    public void Hide()
    {
        if (_outlineRoot == null) return;
        Destroy(_outlineRoot);
        _outlineRoot = null;
    }

    void OnDestroy() => Hide();
}
