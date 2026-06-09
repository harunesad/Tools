using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using BehaviorFlow.Runtime;

namespace BehaviorFlow.Editor
{
    public static class BehaviorFlowCompiler
    {
        public static void CompileGraph(BehaviorFlowGraph graph)
        {
            if (graph == null) return;

            string className = MakeValidClassName(graph.name + "_Compiled");
            string path = EditorUtility.SaveFilePanel("Save Compiled FSM Class", "Assets/", className, "cs");
            if (string.IsNullOrEmpty(path)) return;
            CompileGraph(graph, path);
        }

        public static void CompileGraph(BehaviorFlowGraph graph, string path)
        {
            if (graph == null) return;
            string className = MakeValidClassName(Path.GetFileNameWithoutExtension(path));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.AI;");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace BehaviorFlow.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : MonoBehaviour");
            sb.AppendLine("    {");
            
            // Cached components
            sb.AppendLine("        private NavMeshAgent agent;");
            sb.AppendLine("        private Animator animator;");
            sb.AppendLine("        private Rigidbody rb;");
            sb.AppendLine("        private Rigidbody2D rb2d;");
            sb.AppendLine();

            sb.AppendLine("        public enum StateType");
            sb.AppendLine("        {");
            foreach (var state in graph.states)
            {
                sb.AppendLine($"            {MakeValidClassName(state.stateName)},");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        [Header(\"Active Status\")]");
            sb.AppendLine("        public StateType currentState;");
            sb.AppendLine();

            // Blackboard variables generated as native class variables (only if they exist)
            if (graph.blackboard.entries.Count > 0)
            {
                sb.AppendLine("        [Header(\"Blackboard Variables\")]");
                foreach (var entry in graph.blackboard.entries)
                {
                    string typeName = GetCSharpTypeName(entry.type);
                    string val = GetCSharpDefaultValue(entry);
                    sb.AppendLine($"        public {typeName} {entry.key} = {val};");
                }
                sb.AppendLine();
            }

            // Prefab declarations for InstantiatePrefab nodes (only if they exist)
            bool hasPrefabs = false;
            foreach (var state in graph.states)
            {
                if (state.behaviorTreeRoot != null && HasInstantiatePrefabNode(state.behaviorTreeRoot))
                {
                    hasPrefabs = true;
                    break;
                }
            }

            if (hasPrefabs)
            {
                sb.AppendLine("        [Header(\"Spawn Prefabs\")]");
                foreach (var state in graph.states)
                {
                    if (state.behaviorTreeRoot != null)
                    {
                        FindAndDeclarePrefabs(state.behaviorTreeRoot, sb);
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("        [Header(\"Animation Settings\")]");
            sb.AppendLine("        public string speedParameterName = \"Speed\";");
            sb.AppendLine("        public string isMovingParameterName = \"IsMoving\";");
            sb.AppendLine();

            sb.AppendLine("        private void Awake()");
            sb.AppendLine("        {");
            sb.AppendLine("            agent = GetComponent<NavMeshAgent>();");
            sb.AppendLine("            animator = GetComponent<Animator>();");
            sb.AppendLine("            rb = GetComponent<Rigidbody>();");
            sb.AppendLine("            rb2d = GetComponent<Rigidbody2D>();");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        private void Start()");
            sb.AppendLine("        {");
            var startState = graph.states.Find(s => s.guid == graph.startStateGuid);
            if (startState != null)
            {
                sb.AppendLine($"            TransitionToState(StateType.{MakeValidClassName(startState.stateName)});");
            }
            else if (graph.states.Count > 0)
            {
                sb.AppendLine($"            TransitionToState(StateType.{MakeValidClassName(graph.states[0].stateName)});");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        private void Update()");
            sb.AppendLine("        {");
            sb.AppendLine("            UpdateLocomotionAnimation();");
            sb.AppendLine("            switch (currentState)");
            sb.AppendLine("            {");
            foreach (var state in graph.states)
            {
                sb.AppendLine($"                case StateType.{MakeValidClassName(state.stateName)}:");
                    sb.AppendLine($"                    Update_{MakeValidClassName(state.stateName)}();");
                sb.AppendLine("                    break;");
            }
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        private void UpdateLocomotionAnimation()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (animator == null) return;");
            sb.AppendLine("            float speed = 0f;");
            sb.AppendLine("            if (agent != null) speed = agent.velocity.magnitude;");
            sb.AppendLine("            else if (rb != null) speed = rb.linearVelocity.magnitude;");
            sb.AppendLine("            else if (rb2d != null) speed = rb2d.linearVelocity.magnitude;");
            sb.AppendLine("            if (!string.IsNullOrEmpty(speedParameterName)) animator.SetFloat(speedParameterName, speed);");
            sb.AppendLine("            if (!string.IsNullOrEmpty(isMovingParameterName)) animator.SetBool(isMovingParameterName, speed > 0.1f);");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Generate state update/transition helper methods
            foreach (var state in graph.states)
            {
                sb.AppendLine($"        private void Update_{MakeValidClassName(state.stateName)}()");
                sb.AppendLine("        {");
                
                // Compile the Behavior Tree if it exists
                if (state.behaviorTreeRoot != null)
                {
                    sb.AppendLine("            // Compiled Behavior Tree Logic");
                    CompileBtNode(state.behaviorTreeRoot, sb, "            ");
                }
                else
                {
                    sb.AppendLine("            // No Behavior Tree attached");
                }

                // Check FSM Transitions
                foreach (var trans in state.transitions)
                {
                    var target = graph.states.Find(s => s.guid == trans.targetStateGuid);
                    if (target != null)
                    {
                        sb.AppendLine($"            // Example FSM transition trigger");
                        sb.AppendLine($"            // if (Condition) TransitionToState(StateType.{MakeValidClassName(target.stateName)});");
                    }
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // Transition logic
            sb.AppendLine("        public void TransitionToState(StateType newState)");
            sb.AppendLine("        {");
            sb.AppendLine("            currentState = newState;");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[BehaviorFlow Compiler] Generated class '{className}' at: {path}");
        }

        private static void FindAndDeclarePrefabs(BtNode node, StringBuilder sb)
        {
            if (node == null) return;
            if (node.nodeType == BtNodeType.Action && node.actionType == BuiltInActionType.InstantiatePrefab)
            {
                string cleanGuid = node.guid.Replace("-", "");
                sb.AppendLine($"        public GameObject prefab_{cleanGuid};");
            }
            foreach (var child in node.children)
            {
                FindAndDeclarePrefabs(child, sb);
            }
        }

        private static void CompileBtNode(BtNode node, StringBuilder sb, string indent)
        {
            if (node == null) return;

            if (node.nodeType == BtNodeType.Sequence)
            {
                sb.AppendLine($"{indent}// Sequence Start: {node.nodeName}");
                foreach (var child in node.children)
                {
                    CompileBtNode(child, sb, indent);
                }
            }
            else if (node.nodeType == BtNodeType.Selector)
            {
                sb.AppendLine($"{indent}// Selector Start: {node.nodeName}");
                if (node.children.Count > 0)
                {
                    CompileBtNode(node.children[0], sb, indent);
                }
            }
            else if (node.nodeType == BtNodeType.Action)
            {
                switch (node.actionType)
                {
                    case BuiltInActionType.MoveToTarget:
                        sb.AppendLine($"{indent}if (agent != null && {node.targetBlackboardKey} != null)");
                        sb.AppendLine($"{indent}    agent.SetDestination({node.targetBlackboardKey}.transform.position);");
                        break;

                    case BuiltInActionType.Wait:
                        sb.AppendLine($"{indent}// Wait actions require dynamic timer handles (custom C# generation)");
                        break;

                    case BuiltInActionType.PlayAnimation:
                        sb.AppendLine($"{indent}if (animator != null)");
                        sb.AppendLine($"{indent}    animator.SetTrigger(\"{node.stringParameter}\");");
                        break;

                    case BuiltInActionType.PlaySound:
                        sb.AppendLine($"{indent}var audioSource = GetComponent<AudioSource>();");
                        sb.AppendLine($"{indent}if (audioSource != null) audioSource.Play();");
                        break;

                    case BuiltInActionType.MoveDirect:
                        {
                            float spd = node.floatParameter > 0 ? node.floatParameter : 5f;
                            if (!string.IsNullOrEmpty(node.targetBlackboardKey))
                            {
                                sb.AppendLine($"{indent}transform.Translate({node.targetBlackboardKey} * {spd}f * Time.deltaTime, Space.World);");
                            }
                            else
                            {
                                sb.AppendLine($"{indent}Vector3 moveDir = new Vector3(Input.GetAxis(\"Horizontal\"), 0, Input.GetAxis(\"Vertical\"));");
                                sb.AppendLine($"{indent}transform.Translate(moveDir * {spd}f * Time.deltaTime, Space.World);");
                            }
                        }
                        break;

                    case BuiltInActionType.AddForce:
                        {
                            string vecStr = $"new Vector3({node.vectorParameter.x}f, {node.vectorParameter.y}f, {node.vectorParameter.z}f)";
                            sb.AppendLine($"{indent}if (rb != null) rb.AddForce({vecStr}, ForceMode.Impulse);");
                            sb.AppendLine($"{indent}else if (rb2d != null) rb2d.AddForce(new Vector2({node.vectorParameter.x}f, {node.vectorParameter.y}f), ForceMode2D.Impulse);");
                        }
                        break;

                    case BuiltInActionType.SetVelocity:
                        {
                            string vecStr = $"new Vector3({node.vectorParameter.x}f, {node.vectorParameter.y}f, {node.vectorParameter.z}f)";
                            sb.AppendLine($"{indent}if (rb != null) rb.velocity = {vecStr};");
                            sb.AppendLine($"{indent}else if (rb2d != null) rb2d.velocity = new Vector2({node.vectorParameter.x}f, {node.vectorParameter.y}f);");
                        }
                        break;

                    case BuiltInActionType.RotateToMouse:
                        sb.AppendLine($"{indent}if (Camera.main != null)");
                        sb.AppendLine($"{indent}{{");
                        sb.AppendLine($"{indent}    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);");
                        sb.AppendLine($"{indent}    if (Physics.Raycast(ray, out RaycastHit hit))");
                        sb.AppendLine($"{indent}    {{");
                        sb.AppendLine($"{indent}        Vector3 pt = hit.point; pt.y = transform.position.y;");
                        sb.AppendLine($"{indent}        transform.LookAt(pt);");
                        sb.AppendLine($"{indent}    }}");
                        sb.AppendLine($"{indent}}}");
                        break;

                    case BuiltInActionType.InstantiatePrefab:
                        {
                            string cleanGuid = node.guid.Replace("-", "");
                            sb.AppendLine($"{indent}if (prefab_{cleanGuid} != null)");
                            sb.AppendLine($"{indent}    Instantiate(prefab_{cleanGuid}, transform.position, transform.rotation);");
                        }
                        break;

                    case BuiltInActionType.DestroyObject:
                        if (!string.IsNullOrEmpty(node.targetBlackboardKey))
                        {
                            sb.AppendLine($"{indent}if ({node.targetBlackboardKey} != null) Destroy({node.targetBlackboardKey});");
                        }
                        break;

                    case BuiltInActionType.ModifyVariable:
                        if (!string.IsNullOrEmpty(node.targetBlackboardKey))
                        {
                            string op = node.modifyMode == ModifyMode.Set ? "=" : (node.modifyMode == ModifyMode.Add ? "+=" : "-=");
                            sb.AppendLine($"{indent}{node.targetBlackboardKey} {op} {node.floatParameter}f;");
                        }
                        break;

                    case BuiltInActionType.LogMessage:
                        sb.AppendLine($"{indent}Debug.Log(\"[BehaviorFlow Log]: \" + \"{node.stringParameter}\");");
                        break;

                    case BuiltInActionType.CallMethod:
                        if (!string.IsNullOrEmpty(node.componentNameParameter) && !string.IsNullOrEmpty(node.methodNameParameter))
                        {
                            string arg = "";
                            if (node.methodParamType == "Float") arg = $"{node.floatParameter}f";
                            else if (node.methodParamType == "Int") arg = $"{(int)node.floatParameter}";
                            else if (node.methodParamType == "Bool") arg = (node.stringParameter == "true" || node.stringParameter == "True") ? "true" : "false";
                            else if (node.methodParamType == "String") arg = $"\"{node.stringParameter}\"";

                            sb.AppendLine($"{indent}var comp_{node.guid.Replace("-", "")} = GetComponent<{node.componentNameParameter}>();");
                            sb.AppendLine($"{indent}if (comp_{node.guid.Replace("-", "")} != null) comp_{node.guid.Replace("-", "")}.{node.methodNameParameter}({arg});");
                        }
                        break;
                }
            }
            else if (node.nodeType == BtNodeType.Condition)
            {
                switch (node.conditionType)
                {
                    case BuiltInConditionType.CheckDistance:
                        sb.AppendLine($"{indent}if ({node.targetBlackboardKey} != null && Vector3.Distance(transform.position, {node.targetBlackboardKey}.transform.position) < {node.floatParameter}f)");
                        break;

                    case BuiltInConditionType.CheckKey:
                        {
                            string keyName = node.keyParameter;
                            string inputMethod = node.keyCheckMode == KeyCheckMode.Down ? "GetKeyDown" : (node.keyCheckMode == KeyCheckMode.Held ? "GetKey" : "GetKeyUp");
                            sb.AppendLine($"{indent}if (Input.{inputMethod}(KeyCode.{keyName}))");
                        }
                        break;

                    case BuiltInConditionType.CheckAxis:
                        {
                            string op = node.axisCheckMode == AxisCheckMode.GreaterThan ? ">" : "<";
                            sb.AppendLine($"{indent}if (Input.GetAxis(\"{node.axisParameter}\") {op} {node.floatParameter}f)");
                        }
                        break;

                    case BuiltInConditionType.CompareVariables:
                        if (!string.IsNullOrEmpty(node.targetBlackboardKey))
                        {
                            string op = node.compareOpParameter == "Greater" ? ">" : (node.compareOpParameter == "Less" ? "<" : "==");
                            sb.AppendLine($"{indent}if ({node.targetBlackboardKey} {op} {node.floatParameter}f)");
                        }
                        break;
                }
            }
        }

        private static string MakeValidClassName(string name)
        {
            return name.Replace(" ", "").Replace("-", "").Replace("_", "");
        }

        private static string GetCSharpTypeName(BlackboardEntry.ValueType type)
        {
            switch (type)
            {
                case BlackboardEntry.ValueType.Float: return "float";
                case BlackboardEntry.ValueType.Int: return "int";
                case BlackboardEntry.ValueType.Bool: return "bool";
                case BlackboardEntry.ValueType.String: return "string";
                case BlackboardEntry.ValueType.Vector3: return "Vector3";
                case BlackboardEntry.ValueType.GameObject: return "GameObject";
                default: return "float";
            }
        }

        private static string GetCSharpDefaultValue(BlackboardEntry entry)
        {
            switch (entry.type)
            {
                case BlackboardEntry.ValueType.Float: return $"{entry.floatVal}f";
                case BlackboardEntry.ValueType.Int: return $"{entry.intVal}";
                case BlackboardEntry.ValueType.Bool: return entry.boolVal ? "true" : "false";
                case BlackboardEntry.ValueType.String: return $"\"{entry.stringVal}\"";
                case BlackboardEntry.ValueType.Vector3: return "Vector3.zero";
                case BlackboardEntry.ValueType.GameObject: return "null";
                default: return "0f";
            }
        }

        private static bool HasInstantiatePrefabNode(BtNode node)
        {
            if (node == null) return false;
            if (node.nodeType == BtNodeType.Action && node.actionType == BuiltInActionType.InstantiatePrefab)
                return true;
            foreach (var child in node.children)
            {
                if (HasInstantiatePrefabNode(child)) return true;
            }
            return false;
        }
    }
}

