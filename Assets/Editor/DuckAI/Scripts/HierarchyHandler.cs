using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class HierarchyHandler
{
    /// <summary>
    /// Generates a string representation of the hierarchy of all root GameObjects in the currently active scene.
    /// </summary>
    /// <remarks>This method retrieves all root GameObjects in the active scene and recursively traverses their child
    /// objects to construct a hierarchical string representation. Each level of the hierarchy is indented for
    /// clarity.</remarks>
    /// example output:
    //Current hierarchy:
    //Main Camera[activeSelf = True, activeInHierarchy = True, tag = MainCamera, layer = Default]
    //Directional Light [activeSelf = False, activeInHierarchy = False, tag = Untagged, layer = Default]
    //GameObject[activeSelf = True, activeInHierarchy = False, tag = Untagged, layer = Default]
    //Global Volume[activeSelf = False, activeInHierarchy = False, tag = Untagged, layer = Default]
    //EventSystem [activeSelf = True, activeInHierarchy = True, tag = Untagged, layer = Default]
    //duck[activeSelf = True, activeInHierarchy = True, tag = Player, layer = Default]
    //  testObject[activeSelf = False, activeInHierarchy = False, tag = Untagged, layer = Default]
    //    neew[activeSelf = True, activeInHierarchy = False, tag = Untagged, layer = Default]

    #region HIERARCHY LISTING
    public static string GetHierarchyString()
    {
        StringBuilder stringBuilder = new(); //[1]

        Scene scene = SceneManager.GetActiveScene();

        GameObject[] roots = scene.GetRootGameObjects(); //[2]

        foreach (GameObject root in roots) // Iterate through each root GameObject
        {
            AppendGameObjectHierarchy(stringBuilder, root, 0);
        }
        return stringBuilder.ToString();
    }

    private static void AppendGameObjectHierarchy(StringBuilder stringBuilder, GameObject gameObject, int indent)
    {
        bool activeStatus = gameObject.activeSelf;

        stringBuilder.AppendLine(new string(' ', indent * 2) + gameObject.name + " [activeSelf = " + gameObject.activeSelf + ", activeInHierarchy = " + gameObject.activeInHierarchy + ", tag = " + gameObject.tag + ", layer = " + LayerMask.LayerToName(gameObject.layer) + "]");

        foreach (Transform child in gameObject.transform) // Recursively process each child GameObject
        {
            AppendGameObjectHierarchy(stringBuilder, child.gameObject, indent + 1);
        }

        // DebugColor.Log($"{gameObject.name}{status}", "Green");
    }
    #endregion


    #region COMPONENTS LISTING

    /// <summary>
    /// Returns a readable list of all components attached to a specific GameObject.
    //  example output:
    // 1. UnityEngine.Transform
    //2. UnityEngine.SpriteRenderer enabled = True
    //3. UnityEngine.Animator enabled = True
    //4. test enabled = True
    //5. UnityEngine.Rigidbody2D
    /// </summary>
    public static string GetComponentsString(GameObject gameObject, string typeFilter = null)
    {
        if (gameObject == null) return "GameObject not found.";

        StringBuilder stringBuilder = new();

        Component[] components = gameObject.GetComponents<Component>();
        if (components == null || components.Length == 0)
        {
            stringBuilder.AppendLine("  (no components)");
            return stringBuilder.ToString();
        }

        int index = 0;
        foreach (Component component in components)
        {
            index++;

            Type type = component.GetType();

            if (!string.IsNullOrEmpty(typeFilter) && !type.Name.Equals(typeFilter, StringComparison.OrdinalIgnoreCase) && !type.FullName.EndsWith("." + typeFilter, StringComparison.OrdinalIgnoreCase)) continue;

            // Check for enabled status if the component is a Behaviour or Renderer
            string status = "";
            if (component is Behaviour behaviour)
            {
                status = $" (enabled={behaviour.enabled})";
            }
            else if (component is Renderer renderer)
            {
                status = $" (enabled={renderer.enabled})";
            }

            stringBuilder.AppendLine($" {index}. {type} {status}");
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Searches the active scene and returns the first GameObject with the given name (exact match).
    /// </summary>
    public static GameObject FindInActiveScene(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>();
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].name.Equals(name))
                    return transforms[j].gameObject;
            }
        }
        return null;
    }
    #endregion

    /// <summary>
    /// Extracts the name of a game object from the provided prompt, searches for the game object in the active scene,
    /// and retrieves a list of its components.
    /// </summary>
    /// <remarks>The method uses regular expressions to extract the game object name from the prompt. If the
    /// game object is not found in the active scene, the returned components list will be empty.</remarks>
    /// <param name="prompt">A string containing the name of the game object enclosed in single or double quotes.</param>
    /// <returns>A formatted string containing the name of the game object and its components, or an empty string if the prompt
    /// is null, empty, or does not contain a valid game object name.</returns>
    public static string LookForComponentsInPrompt(string prompt)
    {
        if (string.IsNullOrEmpty(prompt)) return "";
        else
        {
            Match match = Regex.Match(prompt, "[\"']([^\"']+)[\"']"); // Extract text within quotes
            if (!match.Success)
            {
                return "";
            }
            string extractedGameObjectName = match.Groups[1].Value; // duck
            GameObject foundGameObject = FindInActiveScene(extractedGameObjectName);
            string componentsList = GetComponentsString(foundGameObject);

            return $"\n{extractedGameObjectName} Components: \n {componentsList} ";
        }
    }
}

#region References and Notes

//[1] StringBuilder is a Dynamic Object. It doesn’t create a new object in the memory
//but dynamically expands the needed memory to accommodate the modified or new string.A String object is immutable, i.e. a
//String cannot be changed once created. To avoid string replacing, appending, removing or inserting new strings in the
//initial string C# introduce StringBuilder concept. 
//https://www.geeksforgeeks.org/c-sharp/stringbuilder-in-c-sharp/


////[2] https://docs.unity3d.com/6000.2/Documentation/ScriptReference/SceneManagement.Scene.GetRootGameObjects.html
///

#endregion
