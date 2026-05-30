namespace SmartSave
{
    /// <summary>
    /// Interface for components that want custom control over how their state is saved and restored.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// A unique identifier within the scene for this saveable object.
        /// </summary>
        string SaveID { get; }

        /// <summary>
        /// Capture and return the state of the component (must be serializable).
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restore the component state from the saved object.
        /// </summary>
        void RestoreState(object state);
    }
}
