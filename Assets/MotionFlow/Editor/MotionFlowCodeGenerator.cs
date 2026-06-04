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
            sb.AppendLine("using UnityEngine.UIElements;");
            sb.AppendLine();
            sb.AppendLine("namespace MotionFlow.Compiled");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : MonoBehaviour");
            sb.AppendLine("    {");

            // Output fields for variables
            sb.AppendLine("        [Header(\"Targets\")]");
            foreach (var track in sequence.Tracks)
            {
                string targetTypeStr = GetComponentTypeString(track.TargetType);
                string cleanName = MakeValidIdentifier(track.TrackName);
                sb.AppendLine($"        public {targetTypeStr} {cleanName}Target;");
            }
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

                for (int stepIndex = 0; stepIndex < track.Steps.Count; stepIndex++)
                {
                    var step = track.Steps[stepIndex];
                    
                    // Delay
                    if (step.Delay > 0f)
                    {
                        sb.AppendLine($"            yield return new WaitForSeconds({step.Delay}f);");
                    }

                    // Float timer loop
                    sb.AppendLine($"            // Step {stepIndex}: {step.Property}");
                    sb.AppendLine("            {");
                    sb.AppendLine("                float elapsed = 0f;");
                    sb.AppendLine($"                float duration = {step.Duration}f;");
                    sb.AppendLine("                while (elapsed < duration)");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    elapsed += Time.deltaTime;");
                    sb.AppendLine("                    float progress = Mathf.Clamp01(elapsed / duration);");
                    
                    // Easing calculations in code
                    string easeExpr = GetEaseExpression(step.Easing, "progress");
                    sb.AppendLine($"                    float t = {easeExpr};");

                    // Set target properties
                    string targetStr = $"{cleanName}Target";
                    WritePropertyAssignment(sb, targetStr, track.TargetType, step, "t");

                    sb.AppendLine("                    yield return null;");
                    sb.AppendLine("                }");
                    
                    // Ensure final values are strictly set at end
                    WritePropertyAssignment(sb, targetStr, track.TargetType, step, "1f");
                    sb.AppendLine("            }");
                    sb.AppendLine();
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
                    return $"- (Mathf.Cos(Mathf.PI * {varName}) - 1f) * 0.5f";
                case MotionFlowEase.EaseInCubic:
                    return $"{varName} * {varName} * {varName}";
                case MotionFlowEase.EaseOutCubic:
                    return $"1f - Mathf.Pow(1f - {varName}, 3f)";
                default:
                    return varName; // Falls back to Linear in compiled form for safety/simplicity
            }
        }

        private static void WritePropertyAssignment(StringBuilder sb, string target, MotionTargetType targetType, MotionFlowStep step, string tVar)
        {
            switch (targetType)
            {
                case MotionTargetType.Transform:
                    if (step.Property == MotionProperty.Position)
                        sb.AppendLine($"                    {target}.position = Vector3.LerpUnclamped(new Vector3({step.StartVec.x}f, {step.StartVec.y}f, {step.StartVec.z}f), new Vector3({step.EndVec.x}f, {step.EndVec.y}f, {step.EndVec.z}f), {tVar});");
                    else if (step.Property == MotionProperty.LocalPosition)
                        sb.AppendLine($"                    {target}.localPosition = Vector3.LerpUnclamped(new Vector3({step.StartVec.x}f, {step.StartVec.y}f, {step.StartVec.z}f), new Vector3({step.EndVec.x}f, {step.EndVec.y}f, {step.EndVec.z}f), {tVar});");
                    else if (step.Property == MotionProperty.Scale)
                        sb.AppendLine($"                    {target}.localScale = Vector3.LerpUnclamped(new Vector3({step.StartVec.x}f, {step.StartVec.y}f, {step.StartVec.z}f), new Vector3({step.EndVec.x}f, {step.EndVec.y}f, {step.EndVec.z}f), {tVar});");
                    break;

                case MotionTargetType.Light:
                    if (step.Property == MotionProperty.Intensity)
                        sb.AppendLine($"                    {target}.intensity = Mathf.Lerp({step.StartFloat}f, {step.EndFloat}f, {tVar});");
                    else if (step.Property == MotionProperty.Range)
                        sb.AppendLine($"                    {target}.range = Mathf.Lerp({step.StartFloat}f, {step.EndFloat}f, {tVar});");
                    break;

                case MotionTargetType.AudioSource:
                    if (step.Property == MotionProperty.Volume)
                        sb.AppendLine($"                    {target}.volume = Mathf.Lerp({step.StartFloat}f, {step.EndFloat}f, {tVar});");
                    break;
            }
        }

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
