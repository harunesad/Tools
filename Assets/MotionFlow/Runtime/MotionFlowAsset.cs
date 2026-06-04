using System;
using System.Collections.Generic;
using UnityEngine;

namespace MotionFlow.Runtime
{
    public enum TargetResolverMode
    {
        DirectReference,
        ByName,
        ByTag,
        UIElementSelector
    }

    [System.Serializable]
    public class MotionFlowStep
    {
        public MotionProperty Property = MotionProperty.Position;
        
        // Target values
        public bool IsVector = true;
        public Vector3 StartVec = Vector3.zero;
        public Vector3 EndVec = Vector3.forward;

        public bool IsColor = false;
        public Color StartColor = Color.white;
        public Color EndColor = Color.white;

        public bool IsFloat = false;
        public float StartFloat = 0f;
        public float EndFloat = 1f;

        // Easing timings
        public float Duration = 0.5f;
        public float Delay = 0f;
        public MotionFlowEase Easing = MotionFlowEase.EaseOutCubic;
        public AnimationCurve CustomCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // Path target movement (Spline movement option)
        public bool IsPathMovement = false;
        public string TargetPathName = "";

        // Shader custom variable target
        public string ShaderVarName = "_Color";
    }

    [System.Serializable]
    public class MotionFlowTrack
    {
        public string TrackName = "New Track";
        public TargetResolverMode Resolver = TargetResolverMode.DirectReference;
        public string ResolverQuery = "MyTargetObjectName";
        public MotionTargetType TargetType = MotionTargetType.Transform;
        
        public List<MotionFlowStep> Steps = new List<MotionFlowStep>();

        public bool EnableStagger = false;
        public float StaggerDelay = 0.05f;
    }

    [System.Serializable]
    public class MotionFlowSequence
    {
        public string SequenceName = "Play Sequence";
        public List<MotionFlowTrack> Tracks = new List<MotionFlowTrack>();
    }

    [CreateAssetMenu(fileName = "MotionFlowSequence", menuName = "MotionFlow/Animation Sequence", order = 1)]
    public class MotionFlowAsset : ScriptableObject
    {
        public List<MotionFlowSequence> Sequences = new List<MotionFlowSequence>();

        public void AddDefaultTransformTrack(string trackName)
        {
            var seq = new MotionFlowSequence { SequenceName = "Default Move" };
            var track = new MotionFlowTrack
            {
                TrackName = trackName,
                Resolver = TargetResolverMode.DirectReference,
                TargetType = MotionTargetType.Transform,
                Steps = new List<MotionFlowStep>
                {
                    new MotionFlowStep { Property = MotionProperty.Position, StartVec = Vector3.zero, EndVec = Vector3.up * 3f, Duration = 0.8f }
                }
            };
            seq.Tracks.Add(track);
            Sequences.Add(seq);
        }
    }
}
