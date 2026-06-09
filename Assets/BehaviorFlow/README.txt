================================================================================
                      BEHAVIORFLOW 2.0 - HYBRID FSM & BT SYSTEM
================================================================================

BehaviorFlow is a high-performance visual scripting system for Unity that blends
Macro Finite State Machines (FSM) with micro Behavior Trees (BT) decision structures.
It compiles visual graphs directly into raw, allocation-free, pure C# MonoBehaviour scripts.

--------------------------------------------------------------------------------
💎 KEY FEATURES & CAPABILITIES
--------------------------------------------------------------------------------

1. Hybrid Dual-Layer Architecture:
   - FSM (Macro): Map out main states (e.g. Locomotion, Chase, Attack).
   - Behavior Tree (Micro): Double-click any state to program detailed sub-actions
     using Sequences, Selectors, Actions, and Conditions.

2. Safety-First Preset Dropdowns:
   - FSM state names use presets ("Idle", "Patrol", "Chase", "Move", etc.)
     to ensure C# identifier safety, with auto-sanitizing custom fallback names.

3. Blackboard Variable & Key Binding Dropdowns:
   - Add variables to your Blackboard (Float, Int, Bool, String, Vector3, GameObject)
     and bind them inside nodes using auto-populated dropdowns. No typing required!

4. Advanced Built-In Actions & Conditions:
   - Inputs: CheckKey (Down/Held/Up), CheckAxis (e.g., Horizontal, Vertical).
   - Physics: AddForce, SetVelocity (supports 3D Rigidbody and 2D Rigidbody2D).
   - Spawning: InstantiatePrefab (spawns projectiles or items), DestroyObject.
   - Modifiers: ModifyVariable (mathematical operators on blackboard values), Wait,
     PlayAnimation, PlaySound, LogMessage.
   - Sensors: CheckDistance, CompareVariables.

--------------------------------------------------------------------------------
🧪 STEP-BY-STEP TESTING & VERIFICATION GUIDE
--------------------------------------------------------------------------------

### Scenario A: Setup a Physics-based Player Controller

1. Prepare Unity Scene:
   - Create a Ground Plane.
   - Create a Sphere named "Player" and add a Rigidbody component.

2. Create & Open Graph:
   - Right-click in Project view: Create > BehaviorFlow > Behavior Graph.
   - Name it "PlayerGraph".
   - Open menu: Tools > BehaviorFlow > Graph Editor.
   - Drag "PlayerGraph" into the slot at the top-left of the editor window.

3. Edit State & BT:
   - Right-click the grid and select "Add FSM State" (defaults to "Idle" preset).
   - Double-click the "Idle" state node to enter the Behavior Tree editor.
   - Right-click to add:
     - Composite: Sequence
     - Condition: CheckKey (Set Key to "Space", Mode to "Down").
     - Action: AddForce (Set Force Vector to "0, 5, 0" in properties panel).
   - Connect: Sequence Out -> CheckKey In, and CheckKey Out -> AddForce In.
   - Click "< Back to FSM" at the breadcrumbs bar.

4. Compile & Attach:
   - Click "Save Graph" then "Compile to C#".
   - Save the file as "PlayerController.cs".
   - Attach the compiled "PlayerController" script to your Player Sphere in the scene.
   - Press PLAY in Unity and press SPACEBAR to verify the physics jump!

### Scenario B: Setup an AI Chase Agent

1. Prepare Unity Scene:
   - Bake NavMesh navigation on your Ground Plane.
   - Create a Capsule named "Enemy" and add a NavMeshAgent component.

2. Create & Open Graph:
   - Create a new Behavior Graph named "EnemyGraph" and assign it to the editor.
   - In the "Blackboard Variables" panel (right sidebar), click "+ Add Variable":
     - Change key name to "TargetPlayer".
     - Set type to "GameObject".

3. Edit State & BT:
   - Right-click the grid and select "Add FSM State" and select "Chase" preset.
   - Double-click the "Chase" state node.
   - Right-click to add:
     - Composite: Sequence
     - Action: MoveToTarget.
   - Select the "MoveToTarget" node. In the properties panel, click the "Target Key"
     dropdown and select "TargetPlayer".
   - Connect the Sequence Out to the MoveToTarget In.
   - Return to FSM view and click Save.

4. Compile & Attach:
   - Click "Compile to C#" and save the file as "EnemyAI.cs".
   - Attach "EnemyAI" script to your Enemy Capsule.
   - Drag the Player Sphere into the "Target Player" slot on the EnemyAI script in inspector.
   - Press PLAY in Unity and watch the Enemy chase the Player!
