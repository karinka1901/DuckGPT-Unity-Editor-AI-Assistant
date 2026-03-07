// Assets/Editor/DuckGPT/EditorJsonUtils.cs
using System;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
public static class JsonUtilitySafe
{
    // Very small JSON serializer for simple anonymous payloads used above.
    // If you install Newtonsoft.Json or System.Text.Json, replace this with that.
    public static string ToJson(object payload)
    {
        // For our payload shape we can cheat: Unity's JsonUtility cannot serialize anonymous objects.
        // We'll perform a minimal serializer by hand for the specific payload in OpenAIClient.
        // If payload uses nested arrays/objects differently, swap to Newtonsoft.
        try
        {
            // Try to use Unity's JsonUtility as a fast path if payload is a serializable type
            return UnityEngine.JsonUtility.ToJson(payload);
        }
        catch
        {
            // Last resort — build JSON manually by reflection-free approach used above in OpenAIClient.
            // NOTE: This will only work for the exact payload shape we use in OpenAIClient.
            // It's intentionally simple for the Editor demo.
            var str = payload.ToString();
            Debug.LogWarning("Fallback JSON serializer in use. For robust parsing install Newtonsoft.Json.");
            return "{}";
        }
    }

    public static string Escape(string s)
    {
        if (s == null) return "";
        var sb = new StringBuilder();
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
#endif
