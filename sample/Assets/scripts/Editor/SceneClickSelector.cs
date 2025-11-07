using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Pool;

[InitializeOnLoad]
public static class SceneClickSelector
{
    static SceneClickSelector()
    {
        // 씬 뷰 업데이트 이벤트에 등록
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        // 우클릭 감지 (마우스 오른쪽 버튼)
        if (e.control &&
            e.type == EventType.MouseDown && 
            e.button == 1)
        {
            using var _ = ListPool<GameObject>.Get(out var pickedObjects);
            FindPickedObjects(sceneView, e, pickedObjects);

            if (pickedObjects.Count > 0)
            {
                Selection.activeGameObject = pickedObjects[0];
            }

            OpenSelector(pickedObjects);

            e.Use();
        }
    }

    private static void FindPickedObjects(SceneView sceneView, Event e, List<GameObject> output)
    {
        foreach (var go in GetAllOverlapping(e.mousePosition))
        {
            if (go != null)
            {
                output.Add(go);
            }
        }
    }
    
    // Get an ordered list of all visually overlapping GameObjects at the screen position from top to bottom
    internal static IEnumerable<GameObject> GetAllOverlapping(Vector2 position)
    {
        var overlapping = new List<GameObject>();
        var ignore = new List<GameObject>();

        while (true)
        {
            var go = HandleUtility.PickGameObject(position, false, ignore.ToArray(), null);
            
            // Prevent infinite loop if object cannot be ignored (this needs to be fixed so print an error)
            if (overlapping.Count > 0 && go == overlapping.Last())
            {
                break;
            }

            overlapping.Add(go);
            ignore.Add(go);

            yield return go;
        }
    }
    
    private static void OpenSelector(List<GameObject> found)
    {
        found.Reverse();
        ObjectSelectorWindow.Show(found);
    }
}
