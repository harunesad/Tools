using UnityEngine;

namespace BehaviorFlow.Runtime
{
    public class TriggerScoreOnContact : MonoBehaviour
    {
        private ScoreSystem _scoreSystem;

        private void Start()
        {
            _scoreSystem = GetComponent<ScoreSystem>();
        }

        private void OnTriggerEnter(Collider other)
        {
            // If the player hits the trigger, add score visually
            if (other.CompareTag("Player") || other.name.Contains("Player"))
            {
                if (_scoreSystem != null)
                {
                    _scoreSystem.AddScore(100f);
                }
            }
        }
    }
}
