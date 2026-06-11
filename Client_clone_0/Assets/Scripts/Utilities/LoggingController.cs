using UnityEngine;

public static class LoggingController
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Tắt toàn bộ debug log của Unity Client
        Debug.unityLogger.logEnabled = false;
    }
}
