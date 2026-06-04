using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MotionFlow.Runtime
{
    public enum TriggerCondition
    {
        OnStart,
        OnEnable,
        OnDisable,
        OnClick3D,
        OnTriggerEnter3D,
        OnCollisionEnter3D,
        OnCustomCall
    }

    [AddComponentMenu("MotionFlow/MotionFlow Trigger")]
    public class MotionFlowTrigger : MonoBehaviour
    {
        [SerializeField] private MotionFlowAsset animationAsset;
        [SerializeField] private string targetSequenceName = "Play Sequence";
        [SerializeField] private TriggerCondition condition = TriggerCondition.OnStart;
        
        [Header("Physics Settings")]
        [SerializeField] private string targetTagFilter = "Player";

        private void Start()
        {
            if (condition == TriggerCondition.OnStart)
            {
                PlaySequence();
            }
        }

        private void OnEnable()
        {
            if (condition == TriggerCondition.OnEnable)
            {
                PlaySequence();
            }
        }

        private void OnDisable()
        {
            if (condition == TriggerCondition.OnDisable)
            {
                PlaySequence();
            }
        }

        private void OnMouseDown()
        {
            if (condition == TriggerCondition.OnClick3D)
            {
                PlaySequence();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (condition == TriggerCondition.OnTriggerEnter3D)
            {
                if (string.IsNullOrEmpty(targetTagFilter) || other.CompareTag(targetTagFilter))
                {
                    PlaySequence();
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (condition == TriggerCondition.OnCollisionEnter3D)
            {
                if (string.IsNullOrEmpty(targetTagFilter) || collision.gameObject.CompareTag(targetTagFilter))
                {
                    PlaySequence();
                }
            }
        }

        public void PlaySequence()
        {
            if (animationAsset == null)
            {
                Debug.LogWarning("[MotionFlow] PlaySequence failed: animationAsset is null!");
                return;
            }

            Debug.Log($"[MotionFlow] PlaySequence starting for: {targetSequenceName}");

            // Find the sequence
            MotionFlowSequence selectedSeq = null;
            foreach (var seq in animationAsset.Sequences)
            {
                if (seq.SequenceName.Equals(targetSequenceName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedSeq = seq;
                    break;
                }
            }

            if (selectedSeq == null)
            {
                Debug.LogWarning($"[MotionFlow] PlaySequence failed: Sequence '{targetSequenceName}' not found in asset '{animationAsset.name}'!");
                return;
            }

            Debug.Log($"[MotionFlow] Sequence '{selectedSeq.SequenceName}' found with {selectedSeq.Tracks.Count} tracks.");

            foreach (var track in selectedSeq.Tracks)
            {
                var targets = ResolveTrackTargets(track);
                if (targets == null || targets.Count == 0)
                {
                    Debug.LogWarning($"[MotionFlow] Track '{track.TrackName}' could not resolve any target using '{track.Resolver}' with query '{track.ResolverQuery}'!");
                    continue;
                }

                Debug.Log($"[MotionFlow] Track '{track.TrackName}' resolved {targets.Count} targets. Starting steps...");

                for (int i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    MotionFlowEngine.CancelAllTweens(target);
                    float staggerOffset = track.EnableStagger ? (i * track.StaggerDelay) : 0f;
                    
                    PlayTrackSteps(target, track, staggerOffset);
                }
            }
        }

        private List<object> ResolveTrackTargets(MotionFlowTrack track)
        {
            var list = new List<object>();

            if (track.Resolver == TargetResolverMode.DirectReference)
            {
                // In direct reference, we target this GameObject or look it up
                var go = GameObject.Find(track.ResolverQuery);
                if (go == null) go = this.gameObject;
                
                var resolved = ResolveComponent(go, track.TargetType);
                if (resolved != null) list.Add(resolved);
            }
            else if (track.Resolver == TargetResolverMode.ByName)
            {
                var go = GameObject.Find(track.ResolverQuery);
                if (go != null)
                {
                    var resolved = ResolveComponent(go, track.TargetType);
                    if (resolved != null) list.Add(resolved);
                }
            }
            else if (track.Resolver == TargetResolverMode.ByTag)
            {
                var tagObjects = GameObject.FindGameObjectsWithTag(track.ResolverQuery);
                foreach (var go in tagObjects)
                {
                    var resolved = ResolveComponent(go, track.TargetType);
                    if (resolved != null) list.Add(resolved);
                }
            }
            else if (track.Resolver == TargetResolverMode.UIElementSelector)
            {
                // Resolve visual elements from a UIDocument in the scene
                var doc = FindFirstObjectByType<UIDocument>();
                if (doc != null && doc.rootVisualElement != null)
                {
                    var ve = doc.rootVisualElement.Q(track.ResolverQuery);
                    if (ve != null) list.Add(ve);
                }
            }

            return list;
        }

        private object ResolveComponent(GameObject go, MotionTargetType type)
        {
            switch (type)
            {
                case MotionTargetType.Transform:
                    return go.transform;
                case MotionTargetType.RectTransform:
                    return go.GetComponent<RectTransform>();
                case MotionTargetType.Material:
                    var renderer = go.GetComponent<Renderer>();
                    return renderer != null ? renderer.material : null;
                case MotionTargetType.Light:
                    return go.GetComponent<Light>();
                case MotionTargetType.AudioSource:
                    return go.GetComponent<AudioSource>();
            }
            return null;
        }

        private void PlayTrackSteps(object target, MotionFlowTrack track, float staggerOffset)
        {
            float accumulatedTime = staggerOffset;

            foreach (var step in track.Steps)
            {
                accumulatedTime += step.Delay;
                float totalDelay = accumulatedTime;

                if (step.IsPathMovement)
                {
                    // Animate position along Bezier spline
                    var pathGo = GameObject.Find(step.TargetPathName);
                    if (pathGo != null)
                    {
                        var path = pathGo.GetComponent<MotionFlowPath>();
                        if (path != null && target is Transform tr)
                        {
                            MotionFlowEngine.StartFloatTween(
                                tr,
                                MotionTargetType.Transform,
                                MotionProperty.Position,
                                0f,
                                1f,
                                step.Duration,
                                totalDelay,
                                step.Easing,
                                step.CustomCurve,
                                step.TargetPathName, // Pass path name to resolve in engine
                                null
                            );
                        }
                    }
                }
                else if (step.IsVector)
                {
                    MotionFlowEngine.StartVectorTween(
                        target,
                        track.TargetType,
                        step.Property,
                        step.StartVec,
                        step.EndVec,
                        step.Duration,
                        totalDelay,
                        step.Easing,
                        step.CustomCurve,
                        null
                    );
                }
                else if (step.IsColor)
                {
                    MotionFlowEngine.StartColorTween(
                        target,
                        track.TargetType,
                        step.Property,
                        step.StartColor,
                        step.EndColor,
                        step.Duration,
                        totalDelay,
                        step.Easing,
                        step.CustomCurve,
                        step.ShaderVarName,
                        null
                    );
                }
                else if (step.IsFloat)
                {
                    MotionFlowEngine.StartFloatTween(
                        target,
                        track.TargetType,
                        step.Property,
                        step.StartFloat,
                        step.EndFloat,
                        step.Duration,
                        totalDelay,
                        step.Easing,
                        step.CustomCurve,
                        step.ShaderVarName,
                        null
                    );
                }

                accumulatedTime += step.Duration;
            }
        }
    }
}
