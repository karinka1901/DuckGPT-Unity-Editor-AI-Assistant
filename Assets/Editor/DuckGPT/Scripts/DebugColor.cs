using UnityEngine;

public static class DebugColor
{
    public static void Log(string message, string color)
    {
        Debug.Log($"<color={color}>{message}</color>");
    }
}
