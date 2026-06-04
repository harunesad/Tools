========================================================================
            MotionFlow - Universal Visual Animator & Spline Suite
========================================================================
Thank you for using MotionFlow! MotionFlow is a premium, high-performance
visual sequence animator and spline path editor. It allows you to animate
3D GameObjects, UI Toolkit, uGUI, Materials, Lights, and Audio with
real-time SceneView previews and an interactive 3D Bezier path editor.

------------------------------------------------------------------------
1. Comparison with DOTween (What's the difference?)
------------------------------------------------------------------------
Can we do everything DOTween does?
- DOTween is a mature, code-first API engine supporting complex code-based 
  sequences, text shakings (punch/shake), and dynamic logic nesting.
- MotionFlow is a Visual-First Animator & Spline Suite.
- What MotionFlow does that DOTween DOES NOT:
  * Sleek Unity Editor Window to build, adjust, and preview tracks visually.
  * Direct Bezier Spline Path Editor handles in SceneView.
  * Visual Triggers (Trigger components with OnStart, OnCollision, OnClick).
  * "Compile to C#" which converts visual animations into raw, optimized,
    allocation-free coroutines for maximum runtime performance.

------------------------------------------------------------------------
2. Step-by-Step Testing Guide (How to Test)
------------------------------------------------------------------------
Follow these simple steps in a clean Unity scene to test MotionFlow:

--- [TEST SCENARIO 1: Simple Object Animation] ---
1. Create a 3D Cube in your scene (`GameObject > 3D Object > Cube`).
2. Open the Sequencer Window: `Tools > MotionFlow > Animation Sequencer`.
3. Click "Create New Asset" and save it as `TestMotionAsset.asset`.
4. Click "+ Add Sequence" and name it: `CubeMove`.
5. Click "+ Add Animation Track" and configure:
   - Target Type: `Transform`
   - Resolver: `ByName`
   - Target Match Query: `Cube` (matches your cube name)
6. Click "+ Add Step" on that track:
   - Property: `Position`
   - Start Value: `(0, 0, 0)`
   - End Value: `(0, 3, 0)`
   - Easing: `EaseInOutBounce` or `EaseOutElastic`
   - Duration: `2` (seconds)
7. Click the green "▶ Test Play In Editor" button at the top to watch 
   the cube animate in real-time in the SceneView!
8. To run it at runtime, select the Cube, add a `MotionFlow Trigger` 
   component, set Condition to `OnStart`, and drag your Asset/Sequence.

--- [TEST SCENARIO 2: Spline Path Movement] ---
1. Create a new empty GameObject and name it `CameraPath`.
2. Add the `MotionFlow Path` component to `CameraPath`.
3. Click "Add Waypoint Node" multiple times in the Inspector.
4. Go to SceneView, drag the yellow spheres and cyan/red handles to form 
   a curved path.
5. In your `CubeMove` sequence, add a new track or update the existing one:
   - Check the "Path Spline" checkbox.
   - Set "Path Spline Name" to `CameraPath`.
   - Set Duration to `3`.
6. Click "▶ Test Play In Editor" to watch the cube glide along the spline!

--- [TEST SCENARIO 3: C# Compilation] ---
1. Build a visual sequence like in Scenario 1.
2. Click the "⚙ Compile to C#" button at the top of the workspace.
3. Save it to your Assets folder.
4. Attach the generated script to your Cube, check "Play On Start", 
   and hit Play in Unity to see it run at high performance with 0 allocation!

------------------------------------------------------------------------
3. Interactive Triggers (MotionFlow Trigger)
------------------------------------------------------------------------
Attach the **MotionFlow Trigger** component to any GameObject:
- Select your target animation sequence.
- Choose a condition: `OnStart`, `OnEnable`, `OnClick3D` (requires collider), 
  `OnTriggerEnter3D` (requires collider tag filtering), or `OnCustomCall`.

To trigger sequences programmatically from C# scripts:
```csharp
using MotionFlow.Runtime;
// ...
var trigger = GetComponent<MotionFlowTrigger>();
trigger.PlaySequence();
```

========================================================================
For updates, detailed tutorials, and template scenes, visit our Asset 
Store publisher page!
========================================================================
