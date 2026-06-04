using System;
using System.Collections;
using UnityEngine;
using MotionFlow.Runtime;

namespace MotionFlow.Compiled
{
    public class CubeMove : MonoBehaviour
    {
        [Header("Targets")]
        public Transform cubemoveTarget;

        [Header("Settings")]
        public bool playOnStart = false;

        private void Start()
        {
            if (playOnStart) PlaySequence();
        }

        public void PlaySequence()
        {
            StopAllCoroutines();
            if (cubemoveTarget != null) StartCoroutine(Runcubemove());
        }

        private IEnumerator Runcubemove()
        {
            string trackName = "cube_move";

            // Step 0 delay
            yield return new WaitForSeconds(2.0000f);

            // Step 0: Position
            {
                float elapsed = 0f;
                float duration = 2.0000f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float t = 1f - Mathf.Cos(progress * Mathf.PI * 0.5f);
                    cubemoveTarget.position = Vector3.LerpUnclamped(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.000000f, 3.000000f, 0.000000f), t);
                    yield return null;
                }
                cubemoveTarget.position = Vector3.LerpUnclamped(new Vector3(0.000000f, 0.000000f, 0.000000f), new Vector3(0.000000f, 3.000000f, 0.000000f), 1f);
            }

            // Step 1 delay
            yield return new WaitForSeconds(1.0000f);

            // Step 1: Position
            {
                float elapsed = 0f;
                float duration = 1.0000f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float t = progress;
                    cubemoveTarget.position = Vector3.LerpUnclamped(new Vector3(0.000000f, 3.000000f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f), t);
                    yield return null;
                }
                cubemoveTarget.position = Vector3.LerpUnclamped(new Vector3(0.000000f, 3.000000f, 0.000000f), new Vector3(0.000000f, 0.000000f, 0.000000f), 1f);
            }

            yield break;
        }

    }
}
