using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BehaviorFlow.Runtime
{
    public class DemoPlayerAgent : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float jumpForce = 6f;
        
        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            float moveX = 0f;
            float moveZ = 0f;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveZ = 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveZ = -1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX = 1f;
            }
#else
            moveX = Input.GetAxis("Horizontal");
            moveZ = Input.GetAxis("Vertical");
#endif

            Vector3 moveDir = new Vector3(moveX, 0f, moveZ).normalized;
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

            // Space key jump
            bool jumpPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpPressed = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Space))
            {
                jumpPressed = true;
            }
#endif

            if (jumpPressed)
            {
                if (rb != null)
                {
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    Debug.Log("[BehaviorFlow Demo] Player Jumped!");
                }
            }
        }
    }
}
