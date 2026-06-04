using System;
using System.Collections;
using UnityEngine;
using MotionFlow.Runtime;

namespace MotionFlow.Compiled
{
    public class PathMove : MonoBehaviour
    {
        [Header("Targets")]
        public Transform pathmoveTarget;
        public MotionFlowPath PathPath; // 'Path'

        [Header("Settings")]
        public bool playOnStart = false;

        private void Start()
        {
            if (playOnStart) PlaySequence();
        }

        public void PlaySequence()
        {
            StopAllCoroutines();
            if (pathmoveTarget != null) StartCoroutine(Runpathmove());
        }

        private IEnumerator Runpathmove()
        {
            string trackName = "path_move";

            // Step 0 delay
            yield return new WaitForSeconds(1.0000f);

            // Step 0: Path Spline Movement along 'Path'
            {
                MotionFlowPath path = PathPath;
                if (path == null)
                {
                    // Fallback: try to find by name at runtime
                    var go = GameObject.Find("Path");
                    if (go != null) path = go.GetComponent<MotionFlowPath>();
                }
                if (path != null)
                {
                    float elapsed = 0f;
                    float duration = 2.0000f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsed / duration);
                        float t = Mathf.Sin(progress * Mathf.PI * 0.5f);
                        pathmoveTarget.position = path.GetPoint(t);
                        yield return null;
                    }
                    pathmoveTarget.position = path.GetPoint(1f);
                }
            }

            yield break;
        }

    }
}
