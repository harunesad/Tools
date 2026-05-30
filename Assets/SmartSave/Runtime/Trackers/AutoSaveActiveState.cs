using UnityEngine;

namespace SmartSave.Trackers
{
    /// <summary>
    /// Drag and drop component to automatically track if a GameObject is active/deactive in the scene.
    /// Perfect for collectable coins, opened chests, or persistent enemy spawns.
    /// </summary>
    [AddComponentMenu("SmartSave/Auto Save Active State")]
    public class AutoSaveActiveState : MonoBehaviour, ISaveable
    {
        [Header("Unique Save ID")]
        [SerializeField] private string customSaveID;

        public string SaveID
        {
            get
            {
                if (string.IsNullOrEmpty(customSaveID))
                {
                    return $"ActiveState_{GetGameObjectPath(gameObject)}";
                }
                return customSaveID;
            }
        }

        private void OnEnable()
        {
            SaveManager.Register(this);
        }

        private void OnDisable()
        {
            // Note: If object is disabled, we still want it to stay registered for saves!
            // To ensure disabled objects are saved properly, SaveManager will fall back and scan them,
            // but registering them on Start/Awake is also helpful.
        }

        private void Start()
        {
            SaveManager.Register(this);
        }

        public object CaptureState()
        {
            return gameObject.activeSelf;
        }

        public void RestoreState(object state)
        {
            if (state is bool activeState)
            {
                gameObject.SetActive(activeState);
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
