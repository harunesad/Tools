using UnityEngine;

namespace SmartSave
{
    /// <summary>
    /// Optional base class that automatically registers/unregisters components with SaveManager.
    /// Inherit from this for automatic registration to avoid full-scene performance overhead when saving.
    /// </summary>
    public abstract class SaveableBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            SaveManager.Register(this);
        }

        protected virtual void OnDisable()
        {
            SaveManager.Unregister(this);
        }
    }
}
