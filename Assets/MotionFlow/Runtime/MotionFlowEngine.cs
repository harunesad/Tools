using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MotionFlow.Runtime
{
    public enum MotionFlowEase
    {
        Linear,
        EaseInSine, EaseOutSine, EaseInOutSine,
        EaseInCubic, EaseOutCubic, EaseInOutCubic,
        EaseInBack, EaseOutBack, EaseInOutBack,
        EaseInElastic, EaseOutElastic, EaseInOutElastic,
        EaseInBounce, EaseOutBounce, EaseInOutBounce,
        CustomCurve
    }

    public enum MotionTargetType
    {
        Transform,
        RectTransform,
        Material,
        Light,
        AudioSource,
        UIToolkit
    }

    public enum MotionProperty
    {
        Position, LocalPosition, Rotation, LocalRotation, Scale,
        AnchoredPosition, SizeDelta, // RectTransform
        Color, Opacity, FloatValue, // Material / Shader
        Intensity, Range, // Light
        Volume, Pitch, // Audio
        UIPositionX, UIPositionY, UIScaleX, UIScaleY, UIRotation, UIOpacity, UIBackgroundColor // UI Toolkit
    }

    public class MotionFlowEngine : MonoBehaviour
    {
        private class ActiveTween
        {
            public object TargetObject; // Can be GameObject, Transform, Material, Light, AudioSource, VisualElement, etc.
            public MotionTargetType TargetType;
            public MotionProperty Property;
            
            // Value ranges
            public Vector3 StartVec;
            public Vector3 EndVec;
            public Color StartColor;
            public Color EndColor;
            public float StartFloat;
            public float EndFloat;
            
            public float Duration;
            public float Delay;
            public MotionFlowEase Easing;
            public AnimationCurve CustomCurve;
            public Action OnComplete;

            public float ElapsedTime;
            public float CurrentDelay;
            public bool IsCompleted;

            // Shader target property name
            public string ShaderPropertyName;
        }

        private static readonly List<ActiveTween> _activeTweens = new List<ActiveTween>();
        private static MotionFlowEngine _runtimeInstance;

        public static bool IsTweening => _activeTweens.Count > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeRuntime()
        {
            if (_runtimeInstance == null && Application.isPlaying)
            {
                var go = new GameObject("[MotionFlow Engine]");
                go.hideFlags = HideFlags.HideInHierarchy;
                _runtimeInstance = go.AddComponent<MotionFlowEngine>();
                DontDestroyOnLoad(go);
            }
        }

        private void Update()
        {
            UpdateTweens(Time.unscaledDeltaTime);
        }

        public static void UpdateTweens(float deltaTime)
        {
            if (_activeTweens.Count > 0)
            {
                Debug.Log($"[MotionFlowEngine] UpdateTweens ticking for {_activeTweens.Count} active tweens. DeltaTime: {deltaTime}");
            }

            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                var tween = _activeTweens[i];
                if (tween.TargetObject == null || (tween.TargetObject is UnityEngine.Object obj && obj == null))
                {
                    Debug.Log("[MotionFlowEngine] Tween target is null, removing.");
                    _activeTweens.RemoveAt(i);
                    continue;
                }

                if (tween.CurrentDelay < tween.Delay)
                {
                    tween.CurrentDelay += deltaTime;
                    continue;
                }

                tween.ElapsedTime += deltaTime;
                float progress = Mathf.Clamp01(tween.ElapsedTime / tween.Duration);
                
                float t = 0f;
                if (tween.Easing == MotionFlowEase.CustomCurve && tween.CustomCurve != null)
                {
                    t = tween.CustomCurve.Evaluate(progress);
                }
                else
                {
                    t = EvaluateEase(tween.Easing, progress);
                }

                ApplyTweenValue(tween, t);

                if (progress >= 1.0f)
                {
                    Debug.Log($"[MotionFlowEngine] Tween completed for target {tween.TargetObject}");
                    tween.IsCompleted = true;
                    _activeTweens.RemoveAt(i);
                    tween.OnComplete?.Invoke();
                }
            }
        }

        public static void StartVectorTween(
            object target,
            MotionTargetType targetType,
            MotionProperty property,
            Vector3 startVal,
            Vector3 endVal,
            float duration,
            float delay,
            MotionFlowEase ease,
            AnimationCurve customCurve = null,
            Action onComplete = null)
        {
            Debug.Log($"[MotionFlowEngine] StartVectorTween: Target={target}, Type={targetType}, Prop={property}, Start={startVal}, End={endVal}, Dur={duration}, Dly={delay}");

            _activeTweens.Add(new ActiveTween
            {
                TargetObject = target,
                TargetType = targetType,
                Property = property,
                StartVec = startVal,
                EndVec = endVal,
                Duration = Mathf.Max(0.01f, duration),
                Delay = delay,
                Easing = ease,
                CustomCurve = customCurve,
                OnComplete = onComplete,
                CurrentDelay = 0f,
                ElapsedTime = 0f
            });
        }

        public static void StartFloatTween(
            object target,
            MotionTargetType targetType,
            MotionProperty property,
            float startVal,
            float endVal,
            float duration,
            float delay,
            MotionFlowEase ease,
            AnimationCurve customCurve = null,
            string shaderPropName = "",
            Action onComplete = null)
        {
            _activeTweens.Add(new ActiveTween
            {
                TargetObject = target,
                TargetType = targetType,
                Property = property,
                StartFloat = startVal,
                EndFloat = endVal,
                Duration = Mathf.Max(0.01f, duration),
                Delay = delay,
                Easing = ease,
                CustomCurve = customCurve,
                ShaderPropertyName = shaderPropName,
                OnComplete = onComplete,
                CurrentDelay = 0f,
                ElapsedTime = 0f
            });
        }

        public static void StartColorTween(
            object target,
            MotionTargetType targetType,
            MotionProperty property,
            Color startVal,
            Color endVal,
            float duration,
            float delay,
            MotionFlowEase ease,
            AnimationCurve customCurve = null,
            string shaderPropName = "",
            Action onComplete = null)
        {
            _activeTweens.Add(new ActiveTween
            {
                TargetObject = target,
                TargetType = targetType,
                Property = property,
                StartColor = startVal,
                EndColor = endVal,
                Duration = Mathf.Max(0.01f, duration),
                Delay = delay,
                Easing = ease,
                CustomCurve = customCurve,
                ShaderPropertyName = shaderPropName,
                OnComplete = onComplete,
                CurrentDelay = 0f,
                ElapsedTime = 0f
            });
        }

        public static void CancelTween(object target, MotionProperty property)
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                if (_activeTweens[i].TargetObject == target && _activeTweens[i].Property == property)
                {
                    _activeTweens.RemoveAt(i);
                }
            }
        }

        public static void CancelAllTweens(object target)
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                if (_activeTweens[i].TargetObject == target)
                {
                    _activeTweens.RemoveAt(i);
                }
            }
        }

        private static void ApplyTweenValue(ActiveTween tween, float t)
        {
            switch (tween.TargetType)
            {
                case MotionTargetType.Transform:
                    var tr = (Transform)tween.TargetObject;
                    if (tween.Property == MotionProperty.Position)
                    {
                        if (!string.IsNullOrEmpty(tween.ShaderPropertyName))
                        {
                            var pathGo = GameObject.Find(tween.ShaderPropertyName);
                            if (pathGo != null)
                            {
                                var path = pathGo.GetComponent<MotionFlowPath>();
                                if (path != null)
                                {
                                    float localT = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                                    tr.position = path.GetPoint(localT);
                                    break;
                                }
                            }
                        }
                        tr.position = Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t);
                    }
                    else if (tween.Property == MotionProperty.LocalPosition) tr.localPosition = Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t);
                    else if (tween.Property == MotionProperty.Rotation) tr.rotation = Quaternion.Euler(Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t));
                    else if (tween.Property == MotionProperty.LocalRotation) tr.localRotation = Quaternion.Euler(Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t));
                    else if (tween.Property == MotionProperty.Scale) tr.localScale = Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t);
                    break;

                case MotionTargetType.RectTransform:
                    var rt = (RectTransform)tween.TargetObject;
                    if (tween.Property == MotionProperty.AnchoredPosition) rt.anchoredPosition = Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t);
                    else if (tween.Property == MotionProperty.SizeDelta) rt.sizeDelta = Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t);
                    else if (tween.Property == MotionProperty.Scale) rt.localScale = Vector3.LerpUnclamped(tween.StartVec, tween.EndVec, t);
                    break;

                case MotionTargetType.Material:
                    var mat = (Material)tween.TargetObject;
                    if (tween.Property == MotionProperty.Color)
                    {
                        if (string.IsNullOrEmpty(tween.ShaderPropertyName)) mat.color = Color.Lerp(tween.StartColor, tween.EndColor, t);
                        else mat.SetColor(tween.ShaderPropertyName, Color.Lerp(tween.StartColor, tween.EndColor, t));
                    }
                    else if (tween.Property == MotionProperty.Opacity)
                    {
                        Color c = mat.color;
                        c.a = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                        mat.color = c;
                    }
                    else if (tween.Property == MotionProperty.FloatValue)
                    {
                        mat.SetFloat(tween.ShaderPropertyName, Mathf.Lerp(tween.StartFloat, tween.EndFloat, t));
                    }
                    break;

                case MotionTargetType.Light:
                    var light = (Light)tween.TargetObject;
                    if (tween.Property == MotionProperty.Intensity) light.intensity = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                    else if (tween.Property == MotionProperty.Range) light.range = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                    else if (tween.Property == MotionProperty.Color) light.color = Color.Lerp(tween.StartColor, tween.EndColor, t);
                    break;

                case MotionTargetType.AudioSource:
                    var audio = (AudioSource)tween.TargetObject;
                    if (tween.Property == MotionProperty.Volume) audio.volume = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                    else if (tween.Property == MotionProperty.Pitch) audio.pitch = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                    break;

                case MotionTargetType.UIToolkit:
                    var ve = (VisualElement)tween.TargetObject;
                    switch (tween.Property)
                    {
                        case MotionProperty.UIPositionX:
                            var currTransX = ve.style.translate.value;
                            ve.style.translate = new Translate(Mathf.LerpUnclamped(tween.StartFloat, tween.EndFloat, t), currTransX.y.value, 0);
                            break;
                        case MotionProperty.UIPositionY:
                            var currTransY = ve.style.translate.value;
                            ve.style.translate = new Translate(currTransY.x.value, Mathf.LerpUnclamped(tween.StartFloat, tween.EndFloat, t), 0);
                            break;
                        case MotionProperty.UIScaleX:
                            var currScaleX = ve.style.scale.value;
                            ve.style.scale = new StyleScale(new Scale(new Vector3(Mathf.LerpUnclamped(tween.StartFloat, tween.EndFloat, t), currScaleX.value.y, 1)));
                            break;
                        case MotionProperty.UIScaleY:
                            var currScaleY = ve.style.scale.value;
                            ve.style.scale = new StyleScale(new Scale(new Vector3(currScaleY.value.x, Mathf.LerpUnclamped(tween.StartFloat, tween.EndFloat, t), 1)));
                            break;
                        case MotionProperty.UIRotation:
                            ve.style.rotate = new Rotate(Angle.Degrees(Mathf.LerpUnclamped(tween.StartFloat, tween.EndFloat, t)));
                            break;
                        case MotionProperty.UIOpacity:
                            ve.style.opacity = Mathf.Lerp(tween.StartFloat, tween.EndFloat, t);
                            break;
                        case MotionProperty.UIBackgroundColor:
                            ve.style.backgroundColor = Color.Lerp(tween.StartColor, tween.EndColor, t);
                            break;
                    }
                    break;
            }
        }

        public static float EvaluateEase(MotionFlowEase ease, float t)
        {
            switch (ease)
            {
                case MotionFlowEase.Linear: return t;
                case MotionFlowEase.EaseInSine: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case MotionFlowEase.EaseOutSine: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case MotionFlowEase.EaseInOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case MotionFlowEase.EaseInCubic: return t * t * t;
                case MotionFlowEase.EaseOutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case MotionFlowEase.EaseInOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                
                case MotionFlowEase.EaseInBack:
                    const float s1 = 1.70158f;
                    return t * t * ((s1 + 1f) * t - s1);
                case MotionFlowEase.EaseOutBack:
                    const float s2 = 1.70158f;
                    return 1f + (t - 1f) * (t - 1f) * ((s2 + 1f) * (t - 1f) + s2);
                case MotionFlowEase.EaseInOutBack:
                    const float s3 = 1.70158f * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((s3 + 1f) * 2f * t - s3)) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((s3 + 1f) * (2f * t - 2f) + s3) + 2f) * 0.5f;

                case MotionFlowEase.EaseInElastic:
                    const float c4 = (2f * Mathf.PI) / 3f;
                    return t == 0f ? 0f : t == 1f ? 1f : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
                case MotionFlowEase.EaseOutElastic:
                    const float c5 = (2f * Mathf.PI) / 3f;
                    return t == 0f ? 0f : t == 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c5) + 1f;
                case MotionFlowEase.EaseInOutElastic:
                    const float c6 = (2f * Mathf.PI) / 4.5f;
                    return t == 0f ? 0f : t == 1f ? 1f : t < 0.5f
                        ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c6)) * 0.5f
                        : (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c6)) * 0.5f + 1f;

                case MotionFlowEase.EaseInBounce:
                    return 1f - EvaluateEase(MotionFlowEase.EaseOutBounce, 1f - t);
                case MotionFlowEase.EaseOutBounce:
                    const float n1 = 7.5625f;
                    const float d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
                    if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
                    return n1 * (t -= 2.625f / d1) * t + 0.984375f;
                case MotionFlowEase.EaseInOutBounce:
                    return t < 0.5f
                        ? (1f - EvaluateEase(MotionFlowEase.EaseOutBounce, 1f - 2f * t)) * 0.5f
                        : (1f + EvaluateEase(MotionFlowEase.EaseOutBounce, 2f * t - 1f)) * 0.5f;

                default:
                    return t;
            }
        }
    }
}
