using UnityEngine;
using UnityEngine.AI;

namespace BehaviorFlow.Runtime
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class DemoEnemyAgent : MonoBehaviour
    {
        public GameObject TargetPlayer;
        private NavMeshAgent agent;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (agent != null && TargetPlayer != null)
            {
                agent.SetDestination(TargetPlayer.transform.position);

                // Procedural Squash & Stretch Animation
                if (agent.velocity.magnitude > 0.2f)
                {
                    // Walk Cycle: bounce scale using speed
                    float cycle = Mathf.Sin(Time.time * 14f);
                    transform.localScale = new Vector3(
                        1f + cycle * 0.08f,
                        1f - cycle * 0.12f,
                        1f + cycle * 0.08f
                    );
                }
                else
                {
                    // Idle Breathing Cycle
                    float cycle = Mathf.Sin(Time.time * 2.5f);
                    transform.localScale = new Vector3(
                        1f - cycle * 0.02f,
                        1f + cycle * 0.03f,
                        1f - cycle * 0.02f
                    );
                }
            }
        }
    }
}
