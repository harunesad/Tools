using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using BehaviorFlow.Runtime;

namespace BehaviorFlow.Editor
{
    public enum EditorViewMode { FSM, BehaviorTree }

    public class BehaviorFlowWindow : EditorWindow
    {
        private BehaviorFlowGraph _targetGraph;
        public BehaviorFlowGraph GetTargetGraph() => _targetGraph;
        private BehaviorFlowGraphView _graphView;

        private BlackboardSection _blackboardSection;
        private Label _breadcrumbsLabel;

        public EditorViewMode viewMode = EditorViewMode.FSM;
        public FsmState activeFsmState;

        [MenuItem("Tools/BehaviorFlow/Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<BehaviorFlowWindow>("BehaviorFlow Editor");
            window.Show();
        }

        private void OnEnable()
        {
            ConstructWindow();
        }

        private void ConstructWindow()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            // TOOLBAR
            var toolbar = new IMGUIContainer(() =>
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                if (GUILayout.Button("+ New Graph", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    CreateNewGraph();

                var oldGraph = _targetGraph;
                _targetGraph = (BehaviorFlowGraph)EditorGUILayout.ObjectField(
                    _targetGraph, typeof(BehaviorFlowGraph), false, GUILayout.Width(200));

                if (oldGraph != _targetGraph && _targetGraph != null)
                    LoadGraph();

                if (_targetGraph != null)
                {
                    if (GUILayout.Button("Save Graph", EditorStyles.toolbarButton, GUILayout.Width(80)))
                        SaveGraph();
                    if (GUILayout.Button("Compile to C#", EditorStyles.toolbarButton, GUILayout.Width(100)))
                        CompileToCS();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            });
            toolbar.style.height = 20;
            rootVisualElement.Add(toolbar);

            // BREADCRUMBS
            var breadcrumbsContainer = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 26,
                    paddingLeft = 10,
                    backgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f),
                    borderBottomColor = new Color(0.08f, 0.08f, 0.08f, 1f),
                    borderBottomWidth = 1
                }
            };
            _breadcrumbsLabel = new Label("Graph  >  FSM View")
            {
                style = { color = new Color(0.85f, 0.85f, 0.85f), unityFontStyleAndWeight = FontStyle.Bold }
            };
            breadcrumbsContainer.Add(_breadcrumbsLabel);
            breadcrumbsContainer.Add(new Button(GoBackToFsm)
            {
                text = "< Back to FSM",
                style = { marginLeft = 10, fontSize = 10, height = 18 }
            });
            rootVisualElement.Add(breadcrumbsContainer);

            // MAIN BODY - horizontal split
            var body = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1
                }
            };
            rootVisualElement.Add(body);

            // Left: Graph Canvas (grows to fill remaining space)
            _graphView = new BehaviorFlowGraphView(this)
            {
                style = { flexGrow = 1 }
            };
            body.Add(_graphView);

            // Right: Blackboard panel (fixed 240px column)
            _blackboardSection = new BlackboardSection(this);
            _blackboardSection.style.width = 240;
            _blackboardSection.style.flexShrink = 0;
            body.Add(_blackboardSection);
        }

        public void GoBackToFsm()
        {
            if (viewMode == EditorViewMode.BehaviorTree)
            {
                // Save current BT changes to target FsmState
                if (activeFsmState != null && _targetGraph != null)
                {
                    _graphView.SaveBtToState(activeFsmState);
                }

                viewMode = EditorViewMode.FSM;
                activeFsmState = null;
                _breadcrumbsLabel.text = "Graph > FSM View";
                _graphView.PopulateFromGraph(_targetGraph);
            }
        }

        public void OpenBehaviorTree(FsmState state)
        {
            if (state == null) return;
            viewMode = EditorViewMode.BehaviorTree;
            activeFsmState = state;
            _breadcrumbsLabel.text = $"Graph > FSM > State: {state.stateName} (Sub-Behavior Tree)";
            _graphView.PopulateBehaviorTree(state);
        }

        private void LoadGraph()
        {
            viewMode = EditorViewMode.FSM;
            activeFsmState = null;
            _breadcrumbsLabel.text = "Graph > FSM View";
            _graphView.PopulateFromGraph(_targetGraph);
            _blackboardSection.Refresh(_targetGraph);
        }

        private void SaveGraph()
        {
            if (_targetGraph == null) return;
            if (viewMode == EditorViewMode.FSM)
            {
                _graphView.SaveToGraph(_targetGraph);
            }
            else if (viewMode == EditorViewMode.BehaviorTree && activeFsmState != null)
            {
                _graphView.SaveBtToState(activeFsmState);
            }

            EditorUtility.SetDirty(_targetGraph);
            AssetDatabase.SaveAssets();
            Debug.Log("[BehaviorFlow] Graph saved successfully.");
        }

        private void CompileToCS()
        {
            if (_targetGraph == null) return;
            SaveGraph();
            BehaviorFlowCompiler.CompileGraph(_targetGraph);
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Behavior Graph",
                "NewBehaviorGraph",
                "asset",
                "Choose where to save the new BehaviorFlow Graph asset."
            );

            if (string.IsNullOrEmpty(path)) return;

            var newGraph = ScriptableObject.CreateInstance<BehaviorFlowGraph>();
            AssetDatabase.CreateAsset(newGraph, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _targetGraph = newGraph;
            LoadGraph();

            Debug.Log($"[BehaviorFlow] New graph created at: {path}");
        }
    }

    public class BehaviorFlowGraphView : GraphView
    {
        private BehaviorFlowWindow _window;
        private List<FsmStateNode> _fsmNodes = new List<FsmStateNode>();
        private List<BtNodeView> _btNodes = new List<BtNodeView>();

        public BehaviorFlowGraphView(BehaviorFlowWindow window)
        {
            _window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenu);
        }

        private void OnContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_window.viewMode == EditorViewMode.FSM)
            {
                evt.menu.AppendAction("Add FSM State", action => CreateStateNode("Idle", action.eventInfo.localMousePosition));
            }
            else
            {
                evt.menu.AppendAction("Add Composite: Sequence", action => CreateBtNode("Sequence", BtNodeType.Sequence, action.eventInfo.localMousePosition));
                evt.menu.AppendAction("Add Composite: Selector", action => CreateBtNode("Selector", BtNodeType.Selector, action.eventInfo.localMousePosition));
                evt.menu.AppendAction("Add Action: MoveToTarget", action => CreateBtActionNode("MoveToTarget", BuiltInActionType.MoveToTarget, action.eventInfo.localMousePosition));
                evt.menu.AppendAction("Add Action: PlayAnimation", action => CreateBtActionNode("PlayAnimation", BuiltInActionType.PlayAnimation, action.eventInfo.localMousePosition));
                evt.menu.AppendAction("Add Condition: CheckDistance", action => CreateBtConditionNode("CheckDistance", BuiltInConditionType.CheckDistance, action.eventInfo.localMousePosition));
            }
        }

        public void CreateStateNode(string stateName, Vector2 position)
        {
            var fsmState = new FsmState
            {
                stateName = stateName,
                graphPosition = position
            };
            var node = new FsmStateNode(_window, fsmState);
            node.SetPosition(new Rect(position, new Vector2(150, 100)));
            AddElement(node);
            _fsmNodes.Add(node);
        }

        public void CreateBtNode(string nodeName, BtNodeType type, Vector2 position)
        {
            var btNode = new BtNode
            {
                nodeName = nodeName,
                nodeType = type,
                graphPosition = position
            };
            var nodeView = new BtNodeView(btNode, _window);
            nodeView.SetPosition(new Rect(position, new Vector2(140, 80)));
            AddElement(nodeView);
            _btNodes.Add(nodeView);
        }

        public void CreateBtActionNode(string name, BuiltInActionType type, Vector2 position)
        {
            var btNode = new BtNode
            {
                nodeName = name,
                nodeType = BtNodeType.Action,
                actionType = type,
                graphPosition = position
            };
            var nodeView = new BtNodeView(btNode, _window);
            nodeView.SetPosition(new Rect(position, new Vector2(140, 80)));
            AddElement(nodeView);
            _btNodes.Add(nodeView);
        }

        public void CreateBtConditionNode(string name, BuiltInConditionType type, Vector2 position)
        {
            var btNode = new BtNode
            {
                nodeName = name,
                nodeType = BtNodeType.Condition,
                conditionType = type,
                graphPosition = position
            };
            var nodeView = new BtNodeView(btNode, _window);
            nodeView.SetPosition(new Rect(position, new Vector2(140, 80)));
            AddElement(nodeView);
            _btNodes.Add(nodeView);
        }


        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            ports.ToList().ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        public void PopulateFromGraph(BehaviorFlowGraph graph)
        {
            graphElements.ToList().ForEach(RemoveElement);
            _fsmNodes.Clear();
            _btNodes.Clear();

            if (graph == null) return;

            Dictionary<string, FsmStateNode> nodeMap = new Dictionary<string, FsmStateNode>();
            foreach (var state in graph.states)
            {
                var node = new FsmStateNode(_window, state);
                node.SetPosition(new Rect(state.graphPosition, new Vector2(150, 100)));
                AddElement(node);
                _fsmNodes.Add(node);
                nodeMap[state.guid] = node;
            }

            foreach (var state in graph.states)
            {
                if (!nodeMap.ContainsKey(state.guid)) continue;
                var sourceNode = nodeMap[state.guid];

                foreach (var trans in state.transitions)
                {
                    if (!nodeMap.ContainsKey(trans.targetStateGuid)) continue;
                    var targetNode = nodeMap[trans.targetStateGuid];

                    var edge = sourceNode.outPort.ConnectTo(targetNode.inPort);
                    AddElement(edge);
                }
            }
        }

        public void PopulateBehaviorTree(FsmState state)
        {
            graphElements.ToList().ForEach(RemoveElement);
            _fsmNodes.Clear();
            _btNodes.Clear();

            if (state == null || state.behaviorTreeRoot == null) return;

            // Simple visualizer of flat tree list
            List<BtNode> flatList = new List<BtNode>();
            CollectBtNodes(state.behaviorTreeRoot, flatList);

            Dictionary<string, BtNodeView> viewMap = new Dictionary<string, BtNodeView>();
            foreach (var node in flatList)
            {
                var nodeView = new BtNodeView(node, _window);
                nodeView.SetPosition(new Rect(node.graphPosition, new Vector2(140, 80)));
                AddElement(nodeView);
                _btNodes.Add(nodeView);
                viewMap[node.guid] = nodeView;
            }

            // Draw connections
            foreach (var parentNode in flatList)
            {
                if (!viewMap.ContainsKey(parentNode.guid)) continue;
                var parentView = viewMap[parentNode.guid];

                foreach (var child in parentNode.children)
                {
                    if (child == null || !viewMap.ContainsKey(child.guid)) continue;
                    var childView = viewMap[child.guid];

                    var edge = parentView.outPort.ConnectTo(childView.inPort);
                    AddElement(edge);
                }
            }
        }

        private void CollectBtNodes(BtNode node, List<BtNode> list)
        {
            if (node == null || list.Contains(node)) return;
            list.Add(node);
            foreach (var child in node.children)
            {
                CollectBtNodes(child, list);
            }
        }

        public void SaveToGraph(BehaviorFlowGraph graph)
        {
            if (graph == null) return;

            graph.states.Clear();

            foreach (var node in _fsmNodes)
            {
                var state = node.stateData;
                state.graphPosition = node.GetPosition().position;
                state.transitions.Clear();

                foreach (var edge in node.outPort.connections)
                {
                    var targetNode = edge.input.node as FsmStateNode;
                    if (targetNode != null)
                    {
                        state.transitions.Add(new FsmTransition
                        {
                            eventName = "OnEvent",
                            targetStateGuid = targetNode.stateData.guid
                        });
                    }
                }

                graph.states.Add(state);
            }
        }

        public void SaveBtToState(FsmState state)
        {
            if (state == null || _btNodes.Count == 0) return;

            // Find root (node with no inputs)
            BtNodeView rootNodeView = null;
            foreach (var view in _btNodes)
            {
                if (!view.inPort.connected)
                {
                    rootNodeView = view;
                    break;
                }
            }

            if (rootNodeView != null)
            {
                state.behaviorTreeRoot = rootNodeView.nodeData;
                BuildTreeHierarchy(rootNodeView);
            }
        }

        private void BuildTreeHierarchy(BtNodeView view)
        {
            view.nodeData.children.Clear();
            view.nodeData.graphPosition = view.GetPosition().position;

            foreach (var edge in view.outPort.connections)
            {
                var childView = edge.input.node as BtNodeView;
                if (childView != null)
                {
                    view.nodeData.children.Add(childView.nodeData);
                    BuildTreeHierarchy(childView);
                }
            }
        }
    }

    public class FsmStateNode : Node
    {
        public FsmState stateData;
        public Port inPort;
        public Port outPort;

        public static string SanitizeStateName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "State";
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
            string clean = sb.ToString();
            if (clean.Length == 0) return "State";
            
            if (char.IsDigit(clean[0]))
            {
                clean = "State" + clean;
            }
            return clean;
        }

        public FsmStateNode(BehaviorFlowWindow window, FsmState state)
        {
            stateData = state;
            title = state.stateName;

            inPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inPort.portName = "Enter";
            inputContainer.Add(inPort);

            outPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outPort.portName = "Transition";
            outputContainer.Add(outPort);

            var predefinedStates = new List<string> { "Idle", "Patrol", "Chase", "Attack", "Move", "Jump", "Interact", "Custom..." };
            string initialChoice = predefinedStates.Contains(state.stateName) ? state.stateName : "Custom...";

            var stateDropdown = new DropdownField("", predefinedStates, initialChoice);
            stateDropdown.style.marginLeft = 4;
            stateDropdown.style.marginRight = 4;
            stateDropdown.style.marginBottom = 2;
            extensionContainer.Add(stateDropdown);

            var customNameField = new TextField() { value = state.stateName };
            customNameField.style.marginLeft = 4;
            customNameField.style.marginRight = 4;
            customNameField.style.marginBottom = 4;
            customNameField.style.display = (initialChoice == "Custom...") ? DisplayStyle.Flex : DisplayStyle.None;
            extensionContainer.Add(customNameField);

            stateDropdown.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != "Custom...")
                {
                    customNameField.style.display = DisplayStyle.None;
                    string sanitized = SanitizeStateName(evt.newValue);
                    stateData.stateName = sanitized;
                    title = sanitized;
                }
                else
                {
                    customNameField.style.display = DisplayStyle.Flex;
                    string sanitized = SanitizeStateName(customNameField.value);
                    stateData.stateName = sanitized;
                    title = sanitized;
                }
            });

            customNameField.RegisterValueChangedCallback(evt =>
            {
                string sanitized = SanitizeStateName(evt.newValue);
                stateData.stateName = sanitized;
                title = sanitized;
            });

            // Double click to open BT
            this.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    window.OpenBehaviorTree(stateData);
                }
            });

            RefreshExpandedState();
            RefreshPorts();
        }
    }

    public class BtNodeView : Node
    {
        public BtNode nodeData;
        public Port inPort;
        public Port outPort;

        private VisualElement CreateBlackboardKeyField(string label, string currentValue, Action<string> onValueChanged, BehaviorFlowWindow window)
        {
            var keys = new List<string>();
            if (window != null && window.GetTargetGraph() != null && window.GetTargetGraph().blackboard != null)
            {
                keys = window.GetTargetGraph().blackboard.entries.Select(e => e.key).ToList();
            }

            if (keys.Count > 0)
            {
                // Create a list with default blank option to allow custom strings if needed
                var choices = new List<string>(keys);
                var dropdown = new DropdownField(label, choices, choices.Contains(currentValue) ? currentValue : choices[0]);
                onValueChanged(dropdown.value); // set default value initially
                dropdown.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                return dropdown;
            }
            else
            {
                var textField = new TextField(label + " (Type manually)") { value = currentValue };
                textField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue));
                return textField;
            }
        }

        public BtNodeView(BtNode node, BehaviorFlowWindow window)
        {
            nodeData = node;
            title = $"{node.nodeType}: {node.nodeName}";

            inPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            inPort.portName = "In";
            inputContainer.Add(inPort);

            outPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outPort.portName = "Out";
            outputContainer.Add(outPort);

            // Draw fields based on actions/conditions
            if (node.nodeType == BtNodeType.Action)
            {
                var actionEnumField = new EnumField("Type", node.actionType);
                actionEnumField.RegisterValueChangedCallback(evt => {
                    node.actionType = (BuiltInActionType)evt.newValue;
                    node.nodeName = node.actionType.ToString();
                    title = $"{node.nodeType}: {node.nodeName}";
                });
                extensionContainer.Add(actionEnumField);

                var actionLabel = new Label($"Action: {node.actionType}") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                extensionContainer.Add(actionLabel);

                if (node.actionType == BuiltInActionType.MoveToTarget)
                {
                    var keyField = CreateBlackboardKeyField("Target Key", node.targetBlackboardKey, val => node.targetBlackboardKey = val, window);
                    extensionContainer.Add(keyField);
                }
                else if (node.actionType == BuiltInActionType.Wait)
                {
                    var durationFloat = new FloatField("Duration") { value = node.floatParameter };
                    durationFloat.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                    extensionContainer.Add(durationFloat);
                }
                else if (node.actionType == BuiltInActionType.PlayAnimation)
                {
                    var animTrigger = new TextField("Trigger Name") { value = node.stringParameter };
                    animTrigger.RegisterValueChangedCallback(evt => node.stringParameter = evt.newValue);
                    extensionContainer.Add(animTrigger);
                }
                else if (node.actionType == BuiltInActionType.MoveDirect)
                {
                    var speedFloat = new FloatField("Speed") { value = node.floatParameter };
                    speedFloat.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                    extensionContainer.Add(speedFloat);

                    var keyField = CreateBlackboardKeyField("Dir Key (Opt)", node.targetBlackboardKey, val => node.targetBlackboardKey = val, window);
                    extensionContainer.Add(keyField);
                }
                else if (node.actionType == BuiltInActionType.AddForce || node.actionType == BuiltInActionType.SetVelocity)
                {
                    var vecField = new IMGUIContainer(() =>
                    {
                        node.vectorParameter = EditorGUILayout.Vector3Field("Force Vector", node.vectorParameter);
                    });
                    extensionContainer.Add(vecField);
                }
                else if (node.actionType == BuiltInActionType.InstantiatePrefab)
                {
                    var prefabField = new IMGUIContainer(() =>
                    {
                        node.prefabParameter = (GameObject)EditorGUILayout.ObjectField("Prefab", node.prefabParameter, typeof(GameObject), false);
                    });
                    extensionContainer.Add(prefabField);

                    var keyField = CreateBlackboardKeyField("Spawn Point Key", node.spawnPointKey, val => node.spawnPointKey = val, window);
                    extensionContainer.Add(keyField);
                }
                else if (node.actionType == BuiltInActionType.DestroyObject)
                {
                    var keyField = CreateBlackboardKeyField("Target Key", node.targetBlackboardKey, val => node.targetBlackboardKey = val, window);
                    extensionContainer.Add(keyField);
                }
                else if (node.actionType == BuiltInActionType.ModifyVariable)
                {
                    var keyField = CreateBlackboardKeyField("Variable Key", node.targetBlackboardKey, val => node.targetBlackboardKey = val, window);
                    extensionContainer.Add(keyField);

                    var modeField = new EnumField("Mode", node.modifyMode);
                    modeField.RegisterValueChangedCallback(evt => node.modifyMode = (ModifyMode)evt.newValue);
                    extensionContainer.Add(modeField);

                    var amountFloat = new FloatField("Amount / Value") { value = node.floatParameter };
                    amountFloat.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                    extensionContainer.Add(amountFloat);
                }
                else if (node.actionType == BuiltInActionType.LogMessage)
                {
                    var msgText = new TextField("Message") { value = node.stringParameter };
                    msgText.RegisterValueChangedCallback(evt => node.stringParameter = evt.newValue);
                    extensionContainer.Add(msgText);
                }
                else if (node.actionType == BuiltInActionType.CallMethod)
                {
                    var components = new List<string>();
                    try
                    {
                        components = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => a.GetName().Name.StartsWith("Assembly-CSharp") || a.GetName().Name == "Assembly-CSharp-Editor")
                            .SelectMany(a => a.GetTypes())
                            .Where(t => t != null && t.IsSubclassOf(typeof(MonoBehaviour)))
                            .Select(t => t.FullName)
                            .OrderBy(name => name)
                            .ToList();
                    }
                    catch (Exception)
                    {
                        components = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .Where(t => t != null && t.IsSubclassOf(typeof(MonoBehaviour)))
                            .Select(t => t.FullName)
                            .OrderBy(name => name)
                            .ToList();
                    }

                    if (components.Count == 0)
                    {
                        components.Add("No Custom MonoBehaviours Found");
                    }

                    string initialComp = components.Contains(node.componentNameParameter) ? node.componentNameParameter : components[0];
                    if (string.IsNullOrEmpty(node.componentNameParameter))
                    {
                        node.componentNameParameter = initialComp;
                    }

                    var compDropdown = new DropdownField("Component", components, node.componentNameParameter);
                    extensionContainer.Add(compDropdown);

                    var methodContainer = new VisualElement();
                    extensionContainer.Add(methodContainer);

                    var paramContainer = new VisualElement();
                    extensionContainer.Add(paramContainer);

                    Action<string> updateMethods = (compName) =>
                    {
                        methodContainer.Clear();
                        paramContainer.Clear();

                        Type targetType = null;
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            targetType = assembly.GetType(compName);
                            if (targetType != null) break;
                        }

                        if (targetType == null)
                        {
                            methodContainer.Add(new Label("Select a valid component."));
                            return;
                        }

                        var methods = targetType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
                            .Where(m => !m.IsSpecialName)
                            .ToList();

                        var methodNames = methods.Select(m => m.Name).Distinct().ToList();
                        if (methodNames.Count == 0)
                        {
                            methodContainer.Add(new Label("No public methods found."));
                            return;
                        }

                        string initialMethod = methodNames.Contains(node.methodNameParameter) ? node.methodNameParameter : methodNames[0];
                        node.methodNameParameter = initialMethod;

                        var methodDropdown = new DropdownField("Method", methodNames, initialMethod);
                        methodContainer.Add(methodDropdown);

                        Action<string> updateParams = (methodName) =>
                        {
                            paramContainer.Clear();
                            var mInfo = methods.FirstOrDefault(m => m.Name == methodName);
                            if (mInfo == null) return;

                            var parameters = mInfo.GetParameters();
                            if (parameters.Length == 0)
                            {
                                node.methodParamType = "None";
                                paramContainer.Add(new Label("Params: None"));
                            }
                            else
                            {
                                var p = parameters[0];
                                if (p.ParameterType == typeof(float))
                                {
                                    node.methodParamType = "Float";
                                    var fField = new FloatField($"Param ({p.Name})") { value = node.floatParameter };
                                    fField.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                                    paramContainer.Add(fField);
                                }
                                else if (p.ParameterType == typeof(int))
                                {
                                    node.methodParamType = "Int";
                                    var iField = new IntegerField($"Param ({p.Name})") { value = (int)node.floatParameter };
                                    iField.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                                    paramContainer.Add(iField);
                                }
                                else if (p.ParameterType == typeof(bool))
                                {
                                    node.methodParamType = "Bool";
                                    var bField = new Toggle($"Param ({p.Name})") { value = (node.stringParameter == "true" || node.stringParameter == "True") };
                                    bField.RegisterValueChangedCallback(evt => node.stringParameter = evt.newValue ? "true" : "false");
                                    paramContainer.Add(bField);
                                }
                                else if (p.ParameterType == typeof(string))
                                {
                                    node.methodParamType = "String";
                                    var sField = new TextField($"Param ({p.Name})") { value = node.stringParameter };
                                    sField.RegisterValueChangedCallback(evt => node.stringParameter = evt.newValue);
                                    paramContainer.Add(sField);
                                }
                                else
                                {
                                    node.methodParamType = "None";
                                    paramContainer.Add(new Label($"Unsupported: {p.ParameterType.Name}"));
                                }
                            }
                        };

                        methodDropdown.RegisterValueChangedCallback(evt => {
                            node.methodNameParameter = evt.newValue;
                            updateParams(evt.newValue);
                        });

                        updateParams(initialMethod);
                    };

                    compDropdown.RegisterValueChangedCallback(evt => {
                        node.componentNameParameter = evt.newValue;
                        updateMethods(evt.newValue);
                    });

                    updateMethods(node.componentNameParameter);
                }
            }
            else if (node.nodeType == BtNodeType.Condition)
            {
                var condEnumField = new EnumField("Type", node.conditionType);
                condEnumField.RegisterValueChangedCallback(evt => {
                    node.conditionType = (BuiltInConditionType)evt.newValue;
                    node.nodeName = node.conditionType.ToString();
                    title = $"{node.nodeType}: {node.nodeName}";
                });
                extensionContainer.Add(condEnumField);

                var condLabel = new Label($"Condition: {node.conditionType}") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                extensionContainer.Add(condLabel);

                if (node.conditionType == BuiltInConditionType.CheckDistance)
                {
                    var keyField = CreateBlackboardKeyField("Target Key", node.targetBlackboardKey, val => node.targetBlackboardKey = val, window);
                    extensionContainer.Add(keyField);

                    var distFloat = new FloatField("Dist Limit") { value = node.floatParameter };
                    distFloat.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                    extensionContainer.Add(distFloat);
                }
                else if (node.conditionType == BuiltInConditionType.CheckKey)
                {
                    var keyText = new TextField("Key (e.g. Space)") { value = node.keyParameter };
                    keyText.RegisterValueChangedCallback(evt => node.keyParameter = evt.newValue);
                    extensionContainer.Add(keyText);

                    var checkModeField = new EnumField("Trigger Mode", node.keyCheckMode);
                    checkModeField.RegisterValueChangedCallback(evt => node.keyCheckMode = (KeyCheckMode)evt.newValue);
                    extensionContainer.Add(checkModeField);
                }
                else if (node.conditionType == BuiltInConditionType.CheckAxis)
                {
                    var axisText = new TextField("Axis Name") { value = node.axisParameter };
                    axisText.RegisterValueChangedCallback(evt => node.axisParameter = evt.newValue);
                    extensionContainer.Add(axisText);

                    var axisModeField = new EnumField("Compare Mode", node.axisCheckMode);
                    axisModeField.RegisterValueChangedCallback(evt => node.axisCheckMode = (AxisCheckMode)evt.newValue);
                    extensionContainer.Add(axisModeField);

                    var valFloat = new FloatField("Threshold") { value = node.floatParameter };
                    valFloat.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                    extensionContainer.Add(valFloat);
                }
                else if (node.conditionType == BuiltInConditionType.CompareVariables)
                {
                    var keyField = CreateBlackboardKeyField("Variable Key", node.targetBlackboardKey, val => node.targetBlackboardKey = val, window);
                    extensionContainer.Add(keyField);

                    var compareOpField = new TextField("Compare Op") { value = node.compareOpParameter };
                    compareOpField.RegisterValueChangedCallback(evt => node.compareOpParameter = evt.newValue);
                    extensionContainer.Add(compareOpField);

                    var valFloat = new FloatField("Value") { value = node.floatParameter };
                    valFloat.RegisterValueChangedCallback(evt => node.floatParameter = evt.newValue);
                    extensionContainer.Add(valFloat);
                }
            }

            RefreshExpandedState();
            RefreshPorts();
        }
    }

    public class BlackboardSection : VisualElement
    {
        private BehaviorFlowWindow _window;
        private ScrollView _scrollView;

        public BlackboardSection(BehaviorFlowWindow window)
        {
            _window = window;

            style.flexGrow = 1;
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            style.borderLeftWidth = 1;
            style.paddingTop = 10;
            style.paddingLeft = 8;
            style.paddingRight = 8;

            var titleLabel = new Label("📋  Blackboard Variables")
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12, color = Color.white, marginBottom = 8 }
            };
            Add(titleLabel);

            _scrollView = new ScrollView { style = { flexGrow = 1 } };
            Add(_scrollView);
        }

        public void Refresh(BehaviorFlowGraph graph)
        {
            _scrollView.Clear();
            if (graph == null) return;

            var addButton = new Button(() =>
            {
                graph.blackboard.SetValue("NewVariable", 0f);
                Refresh(graph);
            })
            { text = "+ Add Variable", style = { marginBottom = 10 } };
            _scrollView.Add(addButton);

            foreach (var entry in graph.blackboard.entries)
            {
                var entryContainer = new VisualElement
                {
                    style = {
                        borderBottomColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                        borderBottomWidth = 1,
                        paddingBottom = 8,
                        marginBottom = 8
                    }
                };

                // Header Row
                var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
                var keyText = new TextField { value = entry.key, style = { flexGrow = 1 } };
                keyText.RegisterValueChangedCallback(evt => { entry.key = evt.newValue; });
                headerRow.Add(keyText);

                var deleteButton = new Button(() =>
                {
                    graph.blackboard.entries.Remove(entry);
                    Refresh(graph);
                })
                { text = "✕", style = { width = 20 } };
                headerRow.Add(deleteButton);
                entryContainer.Add(headerRow);

                // Type selector
                var typeEnumField = new EnumField("Type", entry.type);
                typeEnumField.RegisterValueChangedCallback(evt =>
                {
                    entry.type = (BlackboardEntry.ValueType)evt.newValue;
                    Refresh(graph);
                });
                entryContainer.Add(typeEnumField);

                // Default Value Input based on selected type
                if (entry.type == BlackboardEntry.ValueType.Float)
                {
                    var floatField = new FloatField("Val") { value = entry.floatVal };
                    floatField.RegisterValueChangedCallback(evt => entry.floatVal = evt.newValue);
                    entryContainer.Add(floatField);
                }
                else if (entry.type == BlackboardEntry.ValueType.Int)
                {
                    var intField = new IntegerField("Val") { value = entry.intVal };
                    intField.RegisterValueChangedCallback(evt => entry.intVal = evt.newValue);
                    entryContainer.Add(intField);
                }
                else if (entry.type == BlackboardEntry.ValueType.Bool)
                {
                    var toggle = new Toggle("Val") { value = entry.boolVal };
                    toggle.RegisterValueChangedCallback(evt => entry.boolVal = evt.newValue);
                    entryContainer.Add(toggle);
                }
                else if (entry.type == BlackboardEntry.ValueType.String)
                {
                    var strField = new TextField("Val") { value = entry.stringVal };
                    strField.RegisterValueChangedCallback(evt => entry.stringVal = evt.newValue);
                    entryContainer.Add(strField);
                }
                else if (entry.type == BlackboardEntry.ValueType.GameObject)
                {
                    var goField = new IMGUIContainer(() =>
                    {
                        entry.gameObjectVal = (GameObject)EditorGUILayout.ObjectField("Val", entry.gameObjectVal, typeof(GameObject), true);
                    });
                    entryContainer.Add(goField);
                }

                _scrollView.Add(entryContainer);
            }
        }
    }
}
