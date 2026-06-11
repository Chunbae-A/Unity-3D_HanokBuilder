using UnityEngine;

[CreateAssetMenu(menuName = "HanokBuilder/Claude Api Config", fileName = "ClaudeApiConfig")]
public class ClaudeApiConfig : ScriptableObject
{
    public string apiKey;
    public string model = "claude-haiku-4-5";
}
