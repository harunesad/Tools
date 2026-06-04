using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using MotionFlow.Runtime;

namespace MotionFlow.Editor
{
    public static class MotionFlowCodeGenerator
    {
        public static void GenerateScript(MotionFlowSequence sequence, string className, string folderPath)
        {
            if (sequence == null || string.IsNullOrEmpty(className)) return;

            string fullPath = Path.Combine(folderPath, className + ".cs");
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using MotionFlow.Runtime;");
            sb.AppendLine();
            sb.AppendLine("namespace MotionFlow.Compiled");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : MonoBehaviour");
            sb.AppendLine("    {");

            // Output fields for target references
            sb.AppendLine("        [Header(\"Targets\")]");
            foreach (var track in sequence.Tracks)
            {
                string targetTypeStr = GetComponentTypeString(track.TargetType);
                string cleanName = MakeValidIdentifier(track.TrackName);
                sb.AppendLine($"        public {targetTypeStr} {cleanName}Target;");

                // For any path-movement steps, expose the path reference as a field too
                foreach (var step in track.Steps)
                {
                    if (step.IsPathMovement && !string.IsNullOrEmpty(step.TargetPathName))
                    {
                        string pathFieldName = MakeValidIdentifier(step.TargetPathName) + "Path";
                        sb.AppendLine($"        public MotionFlowPath {pathFieldName}; // '{step.TargetPathName}'");
                    }
                }
            }
            sb.AppendLine();
            sb.AppendLine("        [Header(\"Settings\")]");
            sb.AppendLine("        public bool playOnStart = false;");
            sb.AppendLine();

            // Start method
            sb.AppendLine("        private void Start()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (playOnStart) PlaySequence();");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Play method
            sb.AppendLine("        public void PlaySequence()");
            sb.AppendLine("        {");
            sb.AppendLine("            StopAllCoroutines();");
            foreach (var track in sequence.Tracks)
            {
                string cleanName = MakeValidIdentifier(track.TrackName);
                sb.AppendLine($"            if ({cleanName}Target != null) StartCoroutine(Run{cleanName}());");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate Coroutines for each track
            foreach (var track in sequence.Tracks)
            {
                string cleanName = MakeValidIdentifier(track.TrackName);
                sb.AppendLine($"        private IEnumerator Run{cleanName}()");
                sb.AppendLine("        {");
                sb.AppendLine($"            string trackName = \"{track.TrackName}\";");
                sb.AppendLine();

                for (int stepIndex = 0; stepIndex < track.Steps.Count; stepIndex++)
                {
                    var step = track.Steps[stepIndex];

                    // Delay before this step
                    if (step.Delay > 0f)
                    {
                        sb.AppendLine($"            // Step {stepIndex} delay");
                        sb.AppendLine($"            yield return new WaitForSeconds({step.Delay.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}f);");
                        sb.AppendLine();
                    }

                    if (step.IsPathMovement)
                    {
                        // ---- PATH SPLINE MOVEMENT ----
                        string pathFieldName = MakeValidIdentifier(step.TargetPathName) + "Path";
                        string easeExpr = GetEaseExpression(step.Easing, "progress");

                        sb.AppendLine($"            // Step {stepIndex}: Path Spline Movement along '{step.TargetPathName}'");
                        sb.AppendLine("            {");
                        sb.AppendLine($"                MotionFlowPath path = {pathFieldName};");
                        sb.AppendLine("                if (path == null)");
                        sb.AppendLine("                {");
                        sb.AppendLine($"                    // Fallback: try to find by name at runtime");
                        sb.AppendLine($"                    var go = GameObject.Find(\"{step.TargetPathName}\");");
                        sb.AppendLine("                    if (go != null) path = go.GetComponent<MotionFlowPath>();");
                        sb.AppendLine("                }");
                        sb.AppendLine("                if (path != null)");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    float elapsed = 0f;");
                        sb.AppendLine($"                    float duration = {step.Duration.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}f;");
                        sb.AppendLine("                    while (elapsed < duration)");
                        sb.AppendLine("                    {");
                        sb.AppendLine("                        elapsed += Time.deltaTime;");
                        sb.AppendLine("                        float progress = Mathf.Clamp01(elapsed / duration);");
                        sb.AppendLine($"                        float t = {easeExpr};");
                        sb.AppendLine($"                        {cleanName}Target.position = path.GetPoint(t);");
                        sb.AppendLine("                        yield return null;");
                        sb.AppendLine("                    }");
                        sb.AppendLine($"                    {cleanName}Target.position = path.GetPoint(1f);");
                        sb.AppendLine("                }");
                        sb.AppendLine("            }");
                        sb.AppendLine();
                    }
                    else
                    {
                        // ---- REGULAR PROPERTY ANIMATION ----
                        string easeExpr = GetEaseExpression(step.Easing, "progress");
                        string targetStr = $"{cleanName}Target";

                        sb.AppendLine($"            // Step {stepIndex}: {step.Property}");
                        sb.AppendLine("            {");
                        sb.AppendLine("                float elapsed = 0f;");
                        sb.AppendLine($"                float duration = {step.Duration.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}f;");
                        sb.AppendLine("                while (elapsed < duration)");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    elapsed += Time.deltaTime;");
                        sb.AppendLine("                    float progress = Mathf.Clamp01(elapsed / duration);");
                        sb.AppendLine($"                    float t = {easeExpr};");

                        WritePropertyAssignment(sb, targetStr, track.TargetType, step, "t", "                    ");

                        sb.AppendLine("                    yield return null;");
                        sb.AppendLine("                }");

                        // Snap to final value
                        WritePropertyAssignment(sb, targetStr, track.TargetType, step, "1f", "                ");
                        sb.AppendLine("            }");
                        sb.AppendLine();
                    }
                }

                sb.AppendLine("            yield break;");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(fullPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[MotionFlow] Successfully exported sequence code to: {fullPath}");
        }

        private static string GetComponentTypeString(MotionTargetType type)
        {
            switch (type)
            {
                case MotionTargetType.Transform: return "Transform";
                case MotionTargetType.RectTransform: return "RectTransform";
                case MotionTargetType.Material: return "Material";
                case MotionTargetType.Light: return "Light";
                case MotionTargetType.AudioSource: return "AudioSource";
                case MotionTargetType.UIToolkit: return "VisualElement";
            }
            return "GameObject";
        }

        private static string GetEaseExpression(MotionFlowEase ease, string varName)
        {
            switch (ease)
            {
                case MotionFlowEase.Linear:
                    return varName;
                case MotionFlowEase.EaseInSine:
                    return $"1f - Mathf.Cos({varName} * Mathf.PI * 0.5f)";
                case MotionFlowEase.EaseOutSine:
                    return $"Mathf.Sin({varName} * Mathf.PI * 0.5f)";
                case MotionFlowEase.EaseInOutSine:
                    return $"-(Mathf.Cos(Mathf.PI * {varName}) - 1f) * 0.5f";
                case MotionFlowEase.EaseInCubic:
                    return $"{varName} * {varName} * {varName}";
                case MotionFlowEase.EaseOutCubic:
                    return $"1f - Mathf.Pow(1f - {varName}, 3f)";
                case MotionFlowEase.EaseInOutCubic:
                    return $"({varName} < 0.5f ? 4f * {varName} * {varName} * {varName} : 1f - Mathf.Pow(-2f * {varName} + 2f, 3f) * 0.5f)";
                case MotionFlowEase.EaseInBack:
                    return $"(1.70158f + 1f) * {varName} * {varName} * {varName} - 1.70158f * {varName} * {varName}";
                case MotionFlowEase.EaseOutBack:
                    return $"1f + (1.70158f + 1f) * Mathf.Pow({varName} - 1f, 3f) + 1.70158f * Mathf.Pow({varName} - 1f, 2f)";
                case MotionFlowEase.EaseInBounce:
                    return $"(1f - EaseOutBounce(1f - {varName}))";
                case MotionFlowEase.EaseOutBounce:
                    return $"EaseOutBounce({varName})";
                case MotionFlowEase.EaseInElastic:
                    return $"({varName} == 0f ? 0f : {varName} == 1f ? 1f : -Mathf.Pow(2f, 10f * {varName} - 10f) * Mathf.Sin(({varName} * 10f - 10.75f) * (2f * Mathf.PI / 3f)))";
                case MotionFlowEase.EaseOutElastic:
                    return $"({varName} == 0f ? 0f : {varName} == 1f ? 1f : Mathf.Pow(2f, -10f * {varName}) * Mathf.Sin(({varName} * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f)";
                default:
                    return varName;
            }
        }

        private static void WritePropertyAssignment(StringBuilder sb, string target, MotionTargetType targetType, MotionFlowStep step, string tVar, string indent)
        {
            switch (targetType)
            {
                case MotionTargetType.Transform:
                    if (step.Property == MotionProperty.Position)
                        sb.AppendLine($"{indent}{target}.position = Vector3.LerpUnclamped(new Vector3({F(step.StartVec.x)}, {F(step.StartVec.y)}, {F(step.StartVec.z)}), new Vector3({F(step.EndVec.x)}, {F(step.EndVec.y)}, {F(step.EndVec.z)}), {tVar});");
                    else if (step.Property == MotionProperty.LocalPosition)
                        sb.AppendLine($"{indent}{target}.localPosition = Vector3.LerpUnclamped(new Vector3({F(step.StartVec.x)}, {F(step.StartVec.y)}, {F(step.StartVec.z)}), new Vector3({F(step.EndVec.x)}, {F(step.EndVec.y)}, {F(step.EndVec.z)}), {tVar});");
                    else if (step.Property == MotionProperty.Rotation)
                        sb.AppendLine($"{indent}{target}.rotation = Quaternion.Euler(Vector3.LerpUnclamped(new Vector3({F(step.StartVec.x)}, {F(step.StartVec.y)}, {F(step.StartVec.z)}), new Vector3({F(step.EndVec.x)}, {F(step.EndVec.y)}, {F(step.EndVec.z)}), {tVar}));");
                    else if (step.Property == MotionProperty.LocalRotation)
                        sb.AppendLine($"{indent}{target}.localRotation = Quaternion.Euler(Vector3.LerpUnclamped(new Vector3({F(step.StartVec.x)}, {F(step.StartVec.y)}, {F(step.StartVec.z)}), new Vector3({F(step.EndVec.x)}, {F(step.EndVec.y)}, {F(step.EndVec.z)}), {tVar}));");
                    else if (step.Property == MotionProperty.Scale)
                        sb.AppendLine($"{indent}{target}.localScale = Vector3.LerpUnclamped(new Vector3({F(step.StartVec.x)}, {F(step.StartVec.y)}, {F(step.StartVec.z)}), new Vector3({F(step.EndVec.x)}, {F(step.EndVec.y)}, {F(step.EndVec.z)}), {tVar});");
                    break;

                case MotionTargetType.RectTransform:
                    if (step.Property == MotionProperty.AnchoredPosition)
                        sb.AppendLine($"{indent}{target}.anchoredPosition = Vector2.LerpUnclamped(new Vector2({F(step.StartVec.x)}, {F(step.StartVec.y)}), new Vector2({F(step.EndVec.x)}, {F(step.EndVec.y)}), {tVar});");
                    else if (step.Property == MotionProperty.SizeDelta)
                        sb.AppendLine($"{indent}{target}.sizeDelta = Vector2.LerpUnclamped(new Vector2({F(step.StartVec.x)}, {F(step.StartVec.y)}), new Vector2({F(step.EndVec.x)}, {F(step.EndVec.y)}), {tVar});");
                    else if (step.Property == MotionProperty.Scale)
                        sb.AppendLine($"{indent}{target}.localScale = Vector3.LerpUnclamped(new Vector3({F(step.StartVec.x)}, {F(step.StartVec.y)}, {F(step.StartVec.z)}), new Vector3({F(step.EndVec.x)}, {F(step.EndVec.y)}, {F(step.EndVec.z)}), {tVar});");
                    break;

                case MotionTargetType.Light:
                    if (step.Property == MotionProperty.Intensity)
                        sb.AppendLine($"{indent}{target}.intensity = Mathf.Lerp({F(step.StartFloat)}, {F(step.EndFloat)}, {tVar});");
                    else if (step.Property == MotionProperty.Range)
                        sb.AppendLine($"{indent}{target}.range = Mathf.Lerp({F(step.StartFloat)}, {F(step.EndFloat)}, {tVar});");
                    else if (step.Property == MotionProperty.Color)
                        sb.AppendLine($"{indent}{target}.color = Color.Lerp(new Color({F(step.StartColor.r)}, {F(step.StartColor.g)}, {F(step.StartColor.b)}, {F(step.StartColor.a)}), new Color({F(step.EndColor.r)}, {F(step.EndColor.g)}, {F(step.EndColor.b)}, {F(step.EndColor.a)}), {tVar});");
                    break;

                case MotionTargetType.AudioSource:
                    if (step.Property == MotionProperty.Volume)
                        sb.AppendLine($"{indent}{target}.volume = Mathf.Lerp({F(step.StartFloat)}, {F(step.EndFloat)}, {tVar});");
                    else if (step.Property == MotionProperty.Pitch)
                        sb.AppendLine($"{indent}{target}.pitch = Mathf.Lerp({F(step.StartFloat)}, {F(step.EndFloat)}, {tVar});");
                    break;

                case MotionTargetType.Material:
                    if (step.Property == MotionProperty.Color)
                    {
                        if (string.IsNullOrEmpty(step.ShaderVarName))
                            sb.AppendLine($"{indent}{target}.color = Color.Lerp(new Color({F(step.StartColor.r)}, {F(step.StartColor.g)}, {F(step.StartColor.b)}, {F(step.StartColor.a)}), new Color({F(step.EndColor.r)}, {F(step.EndColor.g)}, {F(step.EndColor.b)}, {F(step.EndColor.a)}), {tVar});");
                        else
                            sb.AppendLine($"{indent}{target}.SetColor(\"{step.ShaderVarName}\", Color.Lerp(new Color({F(step.StartColor.r)}, {F(step.StartColor.g)}, {F(step.StartColor.b)}, {F(step.StartColor.a)}), new Color({F(step.EndColor.r)}, {F(step.EndColor.g)}, {F(step.EndColor.b)}, {F(step.EndColor.a)}), {tVar}));");
                    }
                    else if (step.Property == MotionProperty.Opacity)
                    {
                        sb.AppendLine($"{indent}{{ var c = {target}.color; c.a = Mathf.Lerp({F(step.StartFloat)}, {F(step.EndFloat)}, {tVar}); {target}.color = c; }}");
                    }
                    else if (step.Property == MotionProperty.FloatValue)
                        sb.AppendLine($"{indent}{target}.SetFloat(\"{step.ShaderVarName}\", Mathf.Lerp({F(step.StartFloat)}, {F(step.EndFloat)}, {tVar}));");
                    break;
            }
        }

        private static string F(float v) =>
            v.ToString("F6", System.Globalization.CultureInfo.InvariantCulture) + "f";

        private static string MakeValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Track";
            var clean = new StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c)) clean.Append(c);
            }
            return clean.ToString();
        }
    }
}
