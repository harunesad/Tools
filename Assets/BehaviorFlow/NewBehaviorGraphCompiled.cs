using UnityEngine;
using UnityEngine.AI;
using System;

namespace BehaviorFlow.Generated
{
    public class NewBehaviorGraphCompiled : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Animator animator;
        private Rigidbody rb;
        private Rigidbody2D rb2d;

        public enum StateType
        {
            Interact,
        }

        [Header("Active Status")]
        public StateType currentState;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            rb2d = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            TransitionToState(StateType.Interact);
        }

        private void Update()
        {
            switch (currentState)
            {
                case StateType.Interact:
                    Update_Interact();
                    break;
            }
        }

        private void Update_Interact()
        {
            // Compiled Behavior Tree Logic
            var comp_393c059555984ecb80e96c0ec8de72a0 = GetComponent<ScoreSystem>();
            if (comp_393c059555984ecb80e96c0ec8de72a0 != null) comp_393c059555984ecb80e96c0ec8de72a0.AddScore(100f);
        }

        public void TransitionToState(StateType newState)
        {
            currentState = newState;
        }
    }
}
