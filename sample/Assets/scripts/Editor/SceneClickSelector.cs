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
                Debug.Log($"겹친 오브젝트 {pickedObjects.Count}개: {string.Join(", ", pickedObjects.Select(s => s.name))}");

                // 예: 첫 번째 오브젝트 선택
                Selection.activeGameObject = pickedObjects[0];
            }

            OpenSelector(pickedObjects);

            e.Use();
        }
    }

    private static void FindPickedObjects(SceneView sceneView, Event e, List<GameObject> output)
    {
        Debug.Log($"mousePos: {e.mousePosition}");
        Vector2 mousePos = e.mousePosition;

        // SceneView 좌표계 → Screen 좌표계 변환
        // mousePos.y = sceneView.camera.pixelHeight - mousePos.y;

        // 클릭 근처 Rect 정의 (픽셀 단위, 겹침 포함)
        Rect pickRect = new Rect(mousePos.x - 1, mousePos.y - 1, 2, 2);

        // HandleUtility.PickRectObjects 사용 (겹친 모든 오브젝트 반환)
        GameObject[] pickedObjects = HandleUtility.PickRectObjects(pickRect, false);

        foreach (var obj in pickedObjects)
        {
            output.Add(obj);
        }
    }
    
    private static void OpenSelector(List<GameObject> found)
    {
        ObjectSelectorWindow.Show(found);
        Debug.Log($"Selector opened. Found: {found.Count} objects.");
    }
}
