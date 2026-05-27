using UnityEditor;
using UnityEngine;

namespace DevSuite.SceneNotes
{
    [CustomEditor(typeof(SceneNote))]
    public class SceneNoteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SceneNote note = (SceneNote)target;

            // Draw a beautiful banner representing the status
            Rect bannerRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(30));
            Color bannerColor = note.GetColor();
            EditorGUI.DrawRect(bannerRect, bannerColor);

            var bannerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            bannerStyle.normal.textColor = Color.white;
            GUI.Label(bannerRect, $"SCENE NOTE - {note.importance.ToString().ToUpper()} IMPORTANCE", bannerStyle);

            EditorGUILayout.Space(10);

            // Edit Fields
            EditorGUI.BeginChangeCheck();
            
            note.title = EditorGUILayout.TextField("Title", note.title);
            note.type = (NoteType)EditorGUILayout.EnumPopup("Note Type", note.type);
            note.importance = (NoteImportance)EditorGUILayout.EnumPopup("Importance", note.importance);
            note.status = (NoteStatus)EditorGUILayout.EnumPopup("Task Status", note.status);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Description / Details");
            note.description = EditorGUILayout.TextArea(note.description, GUILayout.Height(60));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(note);
                EditorApplication.RepaintHierarchyWindow();
            }
        }
    }

    [InitializeOnLoad]
    public static class SceneNoteShortcutListener
    {
        static SceneNoteShortcutListener()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;

            // Shift + N shortcut to add a scene note
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.N && e.shift)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 spawnPos;

                // Try to raycast onto scene colliders
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    spawnPos = hit.point;
                }
                else
                {
                    // Draw onto plane at y=0 or 10 units forward
                    float enter;
                    Plane plane = new Plane(Vector3.up, Vector3.zero);
                    if (plane.Raycast(ray, out enter))
                    {
                        spawnPos = ray.GetPoint(enter);
                    }
                    else
                    {
                        spawnPos = ray.GetPoint(10f);
                    }
                }

                CreateNoteAt(spawnPos);
                e.Use();
            }
        }

        private static void CreateNoteAt(Vector3 position)
        {
            GameObject go = new GameObject("Scene Note");
            go.transform.position = position;
            var note = go.AddComponent<SceneNote>();
            note.title = "Fix this object";
            note.description = "Add details here...";
            
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create Scene Note");
            Debug.Log("[DevSuite] Created scene note. Use the Inspector to edit it.");
        }
    }
}
