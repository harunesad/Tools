using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviorFlow.Runtime
{
    [Serializable]
    public class FsmTransition
    {
        public string eventName;
        public string targetStateGuid;
    }

    [Serializable]
    public class FsmState
    {
        [HideInInspector] public string guid = Guid.NewGuid().ToString();
        [HideInInspector] public Vector2 graphPosition;
        public string stateName;

        public List<FsmTransition> transitions = new List<FsmTransition>();

        // Optional embedded behavior tree root
        [SerializeReference] public BtNode behaviorTreeRoot;

        public virtual void OnEnter(GameObject agent, Blackboard blackboard) { }
        public virtual void OnUpdate(GameObject agent, Blackboard blackboard) { }
        public virtual void OnExit(GameObject agent, Blackboard blackboard) { }
    }
}
