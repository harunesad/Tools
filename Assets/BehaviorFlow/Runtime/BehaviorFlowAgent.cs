using UnityEngine;

namespace BehaviorFlow.Runtime
{
    public class BehaviorFlowAgent : MonoBehaviour
    {
        public BehaviorFlowGraph behaviorGraph;
        
        [Header("Runtime Info")]
        [SerializeField] private string activeStateName;
        
        private FsmState currentState;
        private Blackboard runtimeBlackboard;

        private void Start()
        {
            if (behaviorGraph == null) return;

            // Clone blackboard so instances don't share data values
            runtimeBlackboard = new Blackboard();
            foreach (var entry in behaviorGraph.blackboard.entries)
            {
                runtimeBlackboard.entries.Add(new BlackboardEntry
                {
                    key = entry.key,
                    type = entry.type,
                    floatVal = entry.floatVal,
                    intVal = entry.intVal,
                    boolVal = entry.boolVal,
                    stringVal = entry.stringVal,
                    vectorVal = entry.vectorVal,
                    gameObjectVal = entry.gameObjectVal
                });
            }

            // Find starting state
            currentState = behaviorGraph.states.Find(s => s.guid == behaviorGraph.startStateGuid);
            if (currentState != null)
            {
                activeStateName = currentState.stateName;
                currentState.OnEnter(gameObject, runtimeBlackboard);
            }
        }

        private void Update()
        {
            if (currentState == null) return;

            // Update behavior tree nested in state if present
            if (currentState.behaviorTreeRoot != null)
            {
                currentState.behaviorTreeRoot.Update(gameObject, runtimeBlackboard);
            }

            currentState.OnUpdate(gameObject, runtimeBlackboard);
        }

        public void SendFsmEvent(string eventName)
        {
            if (currentState == null) return;

            var transition = currentState.transitions.Find(t => t.eventName == eventName);
            if (transition != null)
            {
                var nextState = behaviorGraph.states.Find(s => s.guid == transition.targetStateGuid);
                if (nextState != null)
                {
                    currentState.OnExit(gameObject, runtimeBlackboard);
                    currentState = nextState;
                    activeStateName = currentState.stateName;
                    currentState.OnEnter(gameObject, runtimeBlackboard);
                }
            }
        }
    }
}
