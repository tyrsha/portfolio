// Assets/Editor/ObjectSelectorWindow.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class ObjectSelectorWindow : EditorWindow
{
    private static List<GameObject> results;
    private Vector2 scroll;

    public static void Show(List<GameObject> objects)
    {
        results = objects;
        var window = GetWindow<ObjectSelectorWindow>("Object Selector");
        window.Show();
    }

    private void OnGUI()
    {
        if (results == null || results.Count == 0)
        {
            GUILayout.Label("No objects detected.");
            return;
        }

        GUILayout.Label("Select an object:", EditorStyles.boldLabel);

        scroll = GUILayout.BeginScrollView(scroll);

        foreach (var obj in results)
        {
            string hierarchyPath = BuildHierarchyPath(obj);

            if (GUILayout.Button(hierarchyPath, GUILayout.Height(26)))
            {
                Selection.activeGameObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        GUILayout.EndScrollView();
    }

    /// <summary>
    /// 부모까지 포함한 Hierarchy Path 생성
    /// ex) "Canvas / Panel / Button"
    /// </summary>
    private static string BuildHierarchyPath(GameObject obj)
    {
        StringBuilder sb = new StringBuilder(obj.name);
        Transform t = obj.transform.parent;

        while (t != null)
        {
            sb.Insert(0, $"{t.name} / ");
            t = t.parent;
        }
        return sb.ToString();
    }
}