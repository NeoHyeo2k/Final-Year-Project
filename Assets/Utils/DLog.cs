using UnityEngine;

public static class DLog
{
    public static bool ENABLE_LOG = false;

    public static void Log(string msg)
    {
        if (ENABLE_LOG)
        {
            DLog.Log(msg);
        }
    }

    public static void Warning(string msg)
    {
        if (ENABLE_LOG)
        {
            DLog.LogWarning(msg);
        }
    }

    public static void Error(string msg)
    {
        DLog.LogError(msg); // error 一般不关
    }
}