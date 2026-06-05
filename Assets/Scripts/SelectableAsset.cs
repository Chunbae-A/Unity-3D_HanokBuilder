using UnityEngine;

/// <summary>
/// 씬 오브젝트에 붙이는 선택 마커 컴포넌트.
/// FBX 자식 구조 대응을 위해 루트 참조를 보관.
/// </summary>
public class SelectableAsset : MonoBehaviour
{
    HanokUIManager _manager;
    public GameObject Root { get; private set; }

    public void Init(HanokUIManager manager, GameObject root)
    {
        _manager = manager;
        Root     = root;
    }
}
