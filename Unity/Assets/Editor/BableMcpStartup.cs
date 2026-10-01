using UnityEditor;
using MCPForUnity.Editor.Services.Transport.Transports;

[InitializeOnLoad]
public static class BableMcpStartup
{
    private static double nextAttempt;
    static BableMcpStartup()
    {
        var scripts = System.IO.Path.GetFullPath("../tools/mcp-env/Scripts");
        if(System.IO.File.Exists(System.IO.Path.Combine(scripts,"uvx.exe")))
        {
            EditorPrefs.SetString("MCPForUnity.UvxPath", System.IO.Path.Combine(scripts,"uvx.exe"));
            var path=System.Environment.GetEnvironmentVariable("PATH")??"";
            if(!path.Contains(scripts))System.Environment.SetEnvironmentVariable("PATH",scripts+";"+path);
        }
        EditorApplication.update += EnsureConnection;
    }
    private static void EnsureConnection()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextAttempt) return;
        nextAttempt = EditorApplication.timeSinceStartup + 10;
        if (!StdioBridgeHost.IsRunning) StdioBridgeHost.StartAutoConnect();
    }
}
