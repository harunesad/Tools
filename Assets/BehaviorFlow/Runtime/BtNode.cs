using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorFlow.Runtime
{
    public enum NodeStatus { Running, Success, Failure }
    public enum BtNodeType { Selector, Sequence, Action, Condition }
    
    public enum BuiltInActionType
    {
        Custom,
        MoveToTarget,
        Wait,
        PlayAnimation,
        PlaySound,
        MoveDirect,
        AddForce,
        SetVelocity,
        RotateToMouse,
        InstantiatePrefab,
        DestroyObject,
        ModifyVariable,
        LogMessage,
        CallMethod
    }

    public enum BuiltInConditionType
    {
        Custom,
        CheckDistance,
        CheckKey,
        CheckAxis,
        CompareVariables
    }

    public enum ModifyMode { Set, Add, Subtract }
    public enum KeyCheckMode { Down, Held, Up }
    public enum AxisCheckMode { GreaterThan, LessThan }

    [Serializable]
    public class BtNode
    {
        [HideInInspector] public string guid = Guid.NewGuid().ToString();
        [HideInInspector] public Vector2 graphPosition;
        public string nodeName;

        public BtNodeType nodeType;
        public BuiltInActionType actionType;
        public BuiltInConditionType conditionType;

        // Core Parameters
        public string targetBlackboardKey;
        public float floatParameter;
        public string stringParameter;

        // Expanded Parameters
        public string keyParameter = "Space"; // KeyCode Name
        public KeyCheckMode keyCheckMode = KeyCheckMode.Down;
        public string axisParameter = "Horizontal";
        public AxisCheckMode axisCheckMode = AxisCheckMode.GreaterThan;
        public Vector3 vectorParameter;
        public GameObject prefabParameter;
        public string spawnPointKey;
        public ModifyMode modifyMode = ModifyMode.Set;
        public string compareOpParameter = "Equal"; // Greater, Less, Equal

        // CallMethod Parameters
        public string componentNameParameter;
        public string methodNameParameter;
        public string methodParamType = "None"; // None, Float, Int, Bool, String

        [SerializeReference]
        public List<BtNode> children = new List<BtNode>();

        // For non-blocking actions like Wait
        [NonSerialized] private float _timer = 0f;
        [NonSerialized] private bool _timerStarted = false;

        public NodeStatus Update(GameObject agent, Blackboard blackboard)
        {
            switch (nodeType)
            {
                case BtNodeType.Selector:
                    foreach (var child in children)
                    {
                        var status = child.Update(agent, blackboard);
                        if (status == NodeStatus.Success || status == NodeStatus.Running)
                            return status;
                    }
                    return NodeStatus.Failure;

                case BtNodeType.Sequence:
                    foreach (var child in children)
                    {
                        var status = child.Update(agent, blackboard);
                        if (status == NodeStatus.Failure || status == NodeStatus.Running)
                            return status;
                    }
                    return NodeStatus.Success;

                case BtNodeType.Action:
                    return ExecuteAction(agent, blackboard);

                case BtNodeType.Condition:
                    return ExecuteCondition(agent, blackboard) ? NodeStatus.Success : NodeStatus.Failure;
            }
            return NodeStatus.Success;
        }

        private NodeStatus ExecuteAction(GameObject agent, Blackboard blackboard)
        {
            switch (actionType)
            {
                case BuiltInActionType.MoveToTarget:
                    {
                        var target = blackboard.GetValue<GameObject>(targetBlackboardKey);
                        if (target == null) return NodeStatus.Failure;

                        var nma = agent.GetComponent<UnityEngine.AI.NavMeshAgent>();
                        if (nma != null)
                        {
                            nma.SetDestination(target.transform.position);
                            if (!nma.pathPending && nma.remainingDistance <= nma.stoppingDistance)
                            {
                                return NodeStatus.Success;
                            }
                            return NodeStatus.Running;
                        }

                        agent.transform.position = Vector3.MoveTowards(agent.transform.position, target.transform.position, Time.deltaTime * (floatParameter > 0 ? floatParameter : 5f));
                        if (Vector3.Distance(agent.transform.position, target.transform.position) < 0.2f)
                        {
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Running;
                    }

                case BuiltInActionType.Wait:
                    {
                        if (!_timerStarted)
                        {
                            _timer = floatParameter;
                            _timerStarted = true;
                        }
                        _timer -= Time.deltaTime;
                        if (_timer <= 0f)
                        {
                            _timerStarted = false;
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Running;
                    }

                case BuiltInActionType.PlayAnimation:
                    {
                        var anim = agent.GetComponent<Animator>();
                        if (anim != null)
                        {
                            anim.SetTrigger(stringParameter);
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.PlaySound:
                    {
                        var audio = agent.GetComponent<AudioSource>();
                        if (audio != null)
                        {
                            // If a specific sound file string matches or default play
                            audio.Play();
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.MoveDirect:
                    {
                        // Moves character direct transform
                        float moveSpeed = floatParameter > 0 ? floatParameter : 5f;
                        Vector3 dir = Vector3.zero;
                        if (blackboard.HasKey(targetBlackboardKey))
                        {
                            dir = blackboard.GetValue<Vector3>(targetBlackboardKey);
                        }
                        else
                        {
                            // Try basic keyboard WASD translation if no specific direction vector is in blackboard
                            dir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
                        }
                        agent.transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
                        return NodeStatus.Success;
                    }

                case BuiltInActionType.AddForce:
                    {
                        var rb = agent.GetComponent<Rigidbody>();
                        var force = vectorParameter;
                        if (rb != null)
                        {
                            rb.AddForce(force, ForceMode.Impulse);
                            return NodeStatus.Success;
                        }
                        var rb2d = agent.GetComponent<Rigidbody2D>();
                        if (rb2d != null)
                        {
                            rb2d.AddForce(new Vector2(force.x, force.y), ForceMode2D.Impulse);
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.SetVelocity:
                    {
                        var rb = agent.GetComponent<Rigidbody>();
                        var vel = vectorParameter;
                        if (rb != null)
                        {
                            rb.linearVelocity = vel;
                            return NodeStatus.Success;
                        }
                        var rb2d = agent.GetComponent<Rigidbody2D>();
                        if (rb2d != null)
                        {
                            rb2d.linearVelocity = new Vector2(vel.x, vel.y);
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.RotateToMouse:
                    {
                        var mainCam = Camera.main;
                        if (mainCam != null)
                        {
                            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                            if (Physics.Raycast(ray, out RaycastHit hit))
                            {
                                Vector3 targetPoint = hit.point;
                                targetPoint.y = agent.transform.position.y;
                                agent.transform.LookAt(targetPoint);
                                return NodeStatus.Success;
                            }
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.InstantiatePrefab:
                    {
                        if (prefabParameter == null) return NodeStatus.Failure;
                        Vector3 spawnPos = agent.transform.position;
                        Quaternion spawnRot = agent.transform.rotation;

                        if (!string.IsNullOrEmpty(spawnPointKey))
                        {
                            var spObj = blackboard.GetValue<GameObject>(spawnPointKey);
                            if (spObj != null)
                            {
                                spawnPos = spObj.transform.position;
                                spawnRot = spObj.transform.rotation;
                            }
                        }

                        UnityEngine.Object.Instantiate(prefabParameter, spawnPos, spawnRot);
                        return NodeStatus.Success;
                    }

                case BuiltInActionType.DestroyObject:
                    {
                        var target = blackboard.GetValue<GameObject>(targetBlackboardKey);
                        if (target != null)
                        {
                            UnityEngine.Object.Destroy(target);
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.ModifyVariable:
                    {
                        if (string.IsNullOrEmpty(targetBlackboardKey)) return NodeStatus.Failure;
                        // Modify float/int/bool variables
                        if (blackboard.HasKey(targetBlackboardKey))
                        {
                            var currentVal = blackboard.GetValue<float>(targetBlackboardKey);
                            if (modifyMode == ModifyMode.Set) currentVal = floatParameter;
                            else if (modifyMode == ModifyMode.Add) currentVal += floatParameter;
                            else if (modifyMode == ModifyMode.Subtract) currentVal -= floatParameter;
                            blackboard.SetValue(targetBlackboardKey, currentVal);
                            return NodeStatus.Success;
                        }
                        return NodeStatus.Failure;
                    }

                case BuiltInActionType.LogMessage:
                    {
                        Debug.Log($"[BehaviorFlow Log]: {stringParameter}");
                        return NodeStatus.Success;
                    }

                case BuiltInActionType.CallMethod:
                    {
                        if (string.IsNullOrEmpty(componentNameParameter) || string.IsNullOrEmpty(methodNameParameter))
                            return NodeStatus.Failure;

                        var comp = agent.GetComponent(componentNameParameter);
                        if (comp == null) return NodeStatus.Failure;

                        var method = comp.GetType().GetMethod(methodNameParameter, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (method == null) return NodeStatus.Failure;

                        try
                        {
                            if (methodParamType == "None")
                            {
                                method.Invoke(comp, null);
                            }
                            else if (methodParamType == "Float")
                            {
                                method.Invoke(comp, new object[] { floatParameter });
                            }
                            else if (methodParamType == "Int")
                            {
                                method.Invoke(comp, new object[] { (int)floatParameter });
                            }
                            else if (methodParamType == "Bool")
                            {
                                method.Invoke(comp, new object[] { stringParameter == "true" || stringParameter == "True" });
                            }
                            else if (methodParamType == "String")
                            {
                                method.Invoke(comp, new object[] { stringParameter });
                            }
                            return NodeStatus.Success;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[BehaviorFlow] Error calling method {methodNameParameter} on {componentNameParameter}: {e.Message}");
                            return NodeStatus.Failure;
                        }
                    }
            }
            return NodeStatus.Success;
        }

        private bool ExecuteCondition(GameObject agent, Blackboard blackboard)
        {
            switch (conditionType)
            {
                case BuiltInConditionType.CheckDistance:
                    {
                        var target = blackboard.GetValue<GameObject>(targetBlackboardKey);
                        if (target == null) return false;

                        float dist = Vector3.Distance(agent.transform.position, target.transform.position);
                        return dist < floatParameter;
                    }

                case BuiltInConditionType.CheckKey:
                    {
                        if (Enum.TryParse<KeyCode>(keyParameter, out KeyCode key))
                        {
                            if (keyCheckMode == KeyCheckMode.Down) return Input.GetKeyDown(key);
                            if (keyCheckMode == KeyCheckMode.Held) return Input.GetKey(key);
                            if (keyCheckMode == KeyCheckMode.Up) return Input.GetKeyUp(key);
                        }
                        return false;
                    }

                case BuiltInConditionType.CheckAxis:
                    {
                        float val = Input.GetAxis(axisParameter);
                        if (axisCheckMode == AxisCheckMode.GreaterThan)
                        {
                            return val > floatParameter;
                        }
                        else
                        {
                            return val < floatParameter;
                        }
                    }

                case BuiltInConditionType.CompareVariables:
                    {
                        if (string.IsNullOrEmpty(targetBlackboardKey)) return false;
                        if (blackboard.HasKey(targetBlackboardKey))
                        {
                            float currentVal = blackboard.GetValue<float>(targetBlackboardKey);
                            if (compareOpParameter == "Equal") return Mathf.Approximately(currentVal, floatParameter);
                            if (compareOpParameter == "Greater") return currentVal > floatParameter;
                            if (compareOpParameter == "Less") return currentVal < floatParameter;
                            if (compareOpParameter == "NotEqual") return !Mathf.Approximately(currentVal, floatParameter);
                        }
                        return false;
                    }
            }
            return true;
        }
    }
}

