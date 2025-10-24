using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Text;

public static class HierarchyUtils
{
    public static string GetHierarchyString()
    {
        var sb = new StringBuilder();
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            AppendGameObjectHierarchy(sb, root, 0);
        }
        return sb.ToString();
    }

    private static void AppendGameObjectHierarchy(StringBuilder sb, GameObject go, int indent)
    {
        sb.AppendLine(new string(' ', indent * 2) + go.name);
        foreach (Transform child in go.transform)
        {
            AppendGameObjectHierarchy(sb, child.gameObject, indent + 1);
        }
    }
}