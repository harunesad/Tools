using System.Collections.Generic;
using UnityEngine;

namespace BehaviorFlow.Runtime
{
    [CreateAssetMenu(fileName = "NewBehaviorGraph", menuName = "BehaviorFlow/Behavior Graph")]
    public class BehaviorFlowGraph : ScriptableObject
    {
        [SerializeReference]
        public List<FsmState> states = new List<FsmState>();

        public string startStateGuid;
        public Blackboard blackboard = new Blackboard();
    }
}
