using UnityEngine;

public static class DLog
{
    public static bool ENABLE_LOG = false;

    // for debug log, only open when you need to debug, otherwise it will cause performance loss
    public static bool ENABLE_DEBUG = false;
    public static bool ENABLE_WARNING = true;

    public static void Log(string msg)
    {
        if (ENABLE_LOG && ENABLE_DEBUG)
        {
            Debug.Log(msg);
        }
    }

    public static void Warning(string msg)
    {
        if (ENABLE_LOG && ENABLE_WARNING)
        {
            Debug.LogWarning(msg);
        }
    }

    public static void LogError(string msg)
    {
        Debug.LogError(msg); // always open
    }
}