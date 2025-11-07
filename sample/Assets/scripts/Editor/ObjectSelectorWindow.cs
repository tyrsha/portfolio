// Assets/Editor/ObjectSelectorWindow.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Linq;

public partial class ObjectSelectorWindow : EditorWindow
{
    private static List<GameObject> results;
    private static TreeViewState treeViewState;
    private ObjectSelectorTreeView treeView;

    public static void Show(List<GameObject> objects)
    {
        results = objects?.Where(o => o != null).Distinct().ToList() ?? new List<GameObject>();

        if (treeViewState == null)
        {
            treeViewState = new TreeViewState();
        }

        var window = GetWindow<ObjectSelectorWindow>("Object Selector");
        window.InitializeTreeView();
        window.Show();
        window.Repaint();
    }

    private void OnGUI()
    {
        EnsureTreeView();

        if (results == null || results.Count == 0)
        {
            GUILayout.Label("No objects detected.");
            return;
        }

        GUILayout.Label("Select an object:", EditorStyles.boldLabel);

        var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
        treeView?.OnGUI(rect);
    }

    private void InitializeTreeView()
    {
        EnsureTreeView();
        treeView?.ReloadWith(results);
    }

    private void EnsureTreeView()
    {
        if (treeViewState == null)
        {
            treeViewState = new TreeViewState();
        }

        if (treeView == null)
        {
            treeView = new ObjectSelectorTreeView(treeViewState, results);
        }
    }
}