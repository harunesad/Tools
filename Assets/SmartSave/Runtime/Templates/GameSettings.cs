using UnityEngine;

namespace SmartSave.Templates
{
    /// <summary>
    /// Out-of-the-box template script to manage and persist Game Settings (Graphic Quality, Volume, Language).
    /// </summary>
    [AddComponentMenu("SmartSave/Templates/Game Settings")]
    public class GameSettings : SaveableBehaviour
    {
        [Header("Audio Levels")]
        [Range(0f, 1f)] [Saveable] public float masterVolume = 1f;
        [Range(0f, 1f)] [Saveable] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] [Saveable] public float musicVolume = 0.8f;

        [Header("Graphics Settings")]
        [Saveable] public int qualityIndex = 2; // Low, Medium, High
        [Saveable] public bool isFullscreen = true;

        [Header("Localization")]
        [Saveable] public string activeLanguage = "en";

        public void ApplySettings()
        {
            // Apply volume levels
            AudioListener.volume = masterVolume;

            // Apply graphics quality
            QualitySettings.SetQualityLevel(qualityIndex);

            // Apply screen mode
            Screen.fullScreen = isFullscreen;

            Debug.Log($"[SmartSave Template] Settings Applied. Master Volume: {masterVolume}, Quality Level: {qualityIndex}, Fullscreen: {isFullscreen}");
        }

        // Apply loaded settings automatically after SaveManager loads them
        public void RestoreAndApply()
        {
            ApplySettings();
        }
    }
}
