using UnityEngine;

namespace SmartSave.Trackers
{
    /// <summary>
    /// Helper component to handle loading the last saved scene automatically on start.
    /// Place this in your entry scene or main menu script.
    /// </summary>
    [AddComponentMenu("SmartSave/Auto Save Scene Loader")]
    public class AutoSaveScene : MonoBehaviour
    {
        public bool loadOnStart = false;

        private void Start()
        {
            if (loadOnStart)
            {
                SaveManager.LoadSavedScene();
            }
        }

        /// <summary>
        /// Manually trigger scene load to last saved scene.
        /// </summary>
        public void LoadLastSavedScene()
        {
            SaveManager.LoadSavedScene();
        }
    }
}
