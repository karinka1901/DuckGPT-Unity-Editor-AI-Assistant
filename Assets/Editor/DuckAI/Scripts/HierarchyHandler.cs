using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Text;
/// <summary>
/// Generates a string representation of the hierarchy of all root GameObjects in the currently active scene.
/// </summary>
/// <remarks>This method retrieves all root GameObjects in the active scene and recursively traverses their child
/// objects to construct a hierarchical string representation. Each level of the hierarchy is indented for
/// clarity.</remarks>
public static class HierarchyHandler
{
    public static string GetHierarchyString()
    {
        var stringBuilder = new StringBuilder();
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            AppendGameObjectHierarchy(stringBuilder, root, 0);
        }
        return stringBuilder.ToString();
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