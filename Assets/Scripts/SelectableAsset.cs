using UnityEngine;

public class SelectableAsset : MonoBehaviour
{
    private HanokUIManager uiManager;
    public  GameObject     Root { get; private set; }

    public void Init(HanokUIManager manager, GameObject root)
    {
        uiManager = manager;
        Root      = root;
    }
}