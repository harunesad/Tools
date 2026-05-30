using UnityEngine;

namespace SmartSave.Trackers
{
    /// <summary>
    /// Drag and drop component to automatically save and restore GameObject position, rotation, and scale.
    /// </summary>
    [AddComponentMenu("SmartSave/Auto Save Transform")]
    public class AutoSaveTransform : MonoBehaviour, ISaveable
    {
        [Header("Unique Save ID")]
        [Tooltip("If left empty, a unique ID will be auto-generated based on the hierarchy path. For dynamic spawned objects, it is recommended to set a custom key.")]
        [SerializeField] private string customSaveID;

        [Header("Transform Tracking Settings")]
        public bool savePosition = true;
        public bool saveRotation = true;
        public bool saveScale = false;
        public bool useLocalSpace = false;

        public string SaveID
        {
            get
            {
                if (string.IsNullOrEmpty(customSaveID))
                {
                    // Fallback to hierarchy path identifier
                    return $"Transform_{GetGameObjectPath(gameObject)}";
                }
                return customSaveID;
            }
        }

        [System.Serializable]
        public struct TransformData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
        }

        private void OnEnable()
        {
            SaveManager.Register(this);
        }

        private void OnDisable()
        {
            SaveManager.Unregister(this);
        }

        public object CaptureState()
        {
            TransformData data = new TransformData();
            data.position = useLocalSpace ? transform.localPosition : transform.position;
            data.rotation = useLocalSpace ? transform.localRotation : transform.rotation;
            data.scale = transform.localScale;
            return data;
        }

        public void RestoreState(object state)
        {
            if (state is TransformData data)
            {
                // Check if object has a Rigidbody, if so move physics position to prevent jittering
                if (TryGetComponent<Rigidbody>(out var rb))
                {
                    if (savePosition)
                    {
                        if (useLocalSpace) transform.localPosition = data.position;
                        else rb.position = data.position;
                    }
                    if (saveRotation)
                    {
                        if (useLocalSpace) transform.localRotation = data.rotation;
                        else rb.rotation = data.rotation;
                    }
                }
                else
                {
                    if (savePosition)
                    {
                        if (useLocalSpace) transform.localPosition = data.position;
                        else transform.position = data.position;
                    }
                    if (saveRotation)
                    {
                        if (useLocalSpace) transform.localRotation = data.rotation;
                        else transform.rotation = data.rotation;
                    }
                }

                if (saveScale)
                {
                    transform.localScale = data.scale;
                }
            }
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }
    }
}
