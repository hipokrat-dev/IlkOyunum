using UnityEditor;

// Unity açıldığında "MCP for Unity" sunucusunun otomatik başlamasını sağlar,
// böylece Claude Code editöre elle bir şey yapmadan bağlanabilir.
[InitializeOnLoad]
static class McpAutoStart
{
    const string AutoStartKey = "MCPForUnity.AutoStartOnLoad";

    static McpAutoStart()
    {
        if (EditorPrefs.GetBool(AutoStartKey, false)) return;
        EditorPrefs.SetBool(AutoStartKey, true);
        // MCP paketi ayarı bir sonraki domain reload'da okur.
        EditorApplication.delayCall += EditorUtility.RequestScriptReload;
    }
}
