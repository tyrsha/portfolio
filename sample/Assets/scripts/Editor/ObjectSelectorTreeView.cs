using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public partial class ObjectSelectorWindow
{
    private class ObjectSelectorTreeView : TreeView
    {
        private readonly Dictionary<int, GameObject> idToGameObject = new Dictionary<int, GameObject>();
        private int nextId = 1;

        public ObjectSelectorTreeView(TreeViewState state, List<GameObject> initialObjects) : base(state)
        {
            showBorder = true;
            ReloadWith(initialObjects);
        }

        public void ReloadWith(List<GameObject> objects)
        {
            idToGameObject.Clear();
            nextId = 1;
            items = objects?.Where(o => o != null).Distinct().ToList() ?? new List<GameObject>();
            Reload();
            ExpandAll();
        }

        private List<GameObject> items = new List<GameObject>();

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

            if (items == null || items.Count == 0)
            {
                SetupParentsAndChildrenFromDepths(root, new List<TreeViewItem>());
                return root;
            }

            var nodeLookup = new Dictionary<Transform, TreeViewItem>();

            foreach (var obj in items)
            {
                if (obj == null)
                {
                    continue;
                }

                var stack = new Stack<Transform>();
                var current = obj.transform;

                while (current != null)
                {
                    stack.Push(current);
                    current = current.parent;
                }

                TreeViewItem parentItem = root;

                while (stack.Count > 0)
                {
                    var transform = stack.Pop();

                    if (!nodeLookup.TryGetValue(transform, out var childItem))
                    {
                        childItem = new TreeViewItem
                        {
                            id = nextId++,
                            displayName = transform.name
                        };

                        parentItem.AddChild(childItem);
                        nodeLookup.Add(transform, childItem);
                    }

                    parentItem = childItem;
                }

                idToGameObject[parentItem.id] = obj;
            }

            SetupDepthsFromParentsAndChildren(root);

            return root;
        }

        protected override void SingleClickedItem(int id)
        {
            base.SingleClickedItem(id);

            if (idToGameObject.TryGetValue(id, out var go) && go != null)
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
        }
    }
}