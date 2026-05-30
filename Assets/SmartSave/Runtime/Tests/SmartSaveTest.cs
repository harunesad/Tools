using UnityEngine;

namespace SmartSave.Tests
{
    /// <summary>
    /// A simple test script demonstrating how to use the [Saveable] attribute.
    /// Just inherit from SaveableBehaviour for automatic scene registration.
    /// </summary>
    public class SmartSaveTest : SaveableBehaviour
    {
        [Header("Player Details")]
        [Saveable] public string playerName = "Sir Galahad";
        [Saveable] public int playerLevel = 1;
        [Saveable] public float playerHealth = 100f;

        [Header("Position Tracking")]
        [Saveable] public Vector3 playerPosition;

        private void Start()
        {
            playerPosition = transform.position;
        }

        private void Update()
        {
            // Update position to demonstrate real-time inspector updates
            playerPosition = transform.position;
        }
    }
}
