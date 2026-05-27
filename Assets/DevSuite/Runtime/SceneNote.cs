using UnityEngine;

namespace DevSuite
{
    public enum NoteType
    {
        Task,
        Bug,
        Info,
        Idea
    }

    public enum NoteImportance
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum NoteStatus
    {
        Todo,
        InProgress,
        Done
    }

    [ExecuteInEditMode]
    [AddComponentMenu("DevSuite/Scene Note")]
    public class SceneNote : MonoBehaviour
    {
        public string title = "New Note";
        [TextArea(3, 5)]
        public string description = "Enter details here...";
        
        public NoteType type = NoteType.Task;
        public NoteImportance importance = NoteImportance.Medium;
        public NoteStatus status = NoteStatus.Todo;

        // Custom color helper depending on importance
        public Color GetColor()
        {
            switch (importance)
            {
                case NoteImportance.Low:
                    return new Color(0.3f, 0.7f, 0.3f); // Green
                case NoteImportance.Medium:
                    return new Color(0.9f, 0.6f, 0.2f); // Orange
                case NoteImportance.High:
                    return new Color(0.9f, 0.3f, 0.2f); // Red
                case NoteImportance.Critical:
                    return new Color(0.6f, 0f, 0.6f);    // Purple
                default:
                    return Color.gray;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw visual indicator in scene view
            Gizmos.color = GetColor();
            Gizmos.DrawSphere(transform.position, 0.3f);
            
            // Draw a small label above
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.4f, $"[{type}] {title}");
        }
#endif
    }
}
