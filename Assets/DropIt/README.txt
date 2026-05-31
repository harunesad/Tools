========================================================================
             DropIt! - Physics Scatter & Level Design Tool
========================================================================
Thank you for using DropIt! Below is your comprehensive guide to painting,
scattering, snapping, and simulating physics directly in the Unity Editor
without entering Play Mode.

------------------------------------------------------------------------
1. Opening the DropIt Dashboard
------------------------------------------------------------------------
Access the main control center:
Menu: Tools -> DropIt -> Scatter Dashboard

We recommend docking this window next to your Hierarchy or Inspector 
for a seamless level design workflow.

------------------------------------------------------------------------
2. Building Your Prefab Palette
------------------------------------------------------------------------
1. Open the DropIt Dashboard.
2. Select one or more 3D prefabs (rocks, foliage, crates, debris) from your Project view.
3. Drag and drop them directly into the "Active Brush Palette" drop box.
4. If multiple prefabs are active, DropIt! automatically rotates/selects 
   them randomly or cycles through them for organic natural scattering.

------------------------------------------------------------------------
3. Real-Time Ghost Visualizer & Controls
------------------------------------------------------------------------
When the paint brush is active, a high-fidelity transparent preview 
(Ghost Preview) shows you exactly where and how your object will spawn before 
you click:

- **Dynamic Overlay:** A 3D floating text overlay displays near your cursor,
  indicating:
  * Active Rotation Axis (X, Y, or Z)
  * Active Pivot Alignment Preset
  * Current Manual Rotation Angles (X, Y, Z)
  
- **Dynamic Rotation Axis Cycling:**
  * **Right-Click** anywhere in the Scene View to cycle the active rotation axis:
    Y (Green) -> X (Red) -> Z (Blue) -> Y (Green).
  * **Mouse Scroll Wheel** to interactively spin the ghost preview around the 
    active rotation axis. The overlay updates in real-time.
    
- **Grid Snapping:** Lock placements to custom coordinate grid sizes for 
  modular structures, walls, or grids.

------------------------------------------------------------------------
4. Premium Pivot Alignment Presets
------------------------------------------------------------------------
Prevent objects from getting buried inside the terrain or floating above ground:
1. Locate the **Pivot Alignment** settings in the Dashboard.
2. Choose a preset:
   - **Bottom:** Perfectly aligns the bottom-most boundary of the mesh to the 
     surface. (Best for trees, rocks, buildings, and ground-scatter objects).
   - **Center:** Aligns the center of the object bounds with the placement normal.
   - **Top:** Aligns the topmost bounds with the surface. (Best for hanging ropes, 
     cables, ceilings).
3. **Manual Offset (XYZ):** Input micro-adjustments to fine-tune placement heights.

------------------------------------------------------------------------
5. Randomization Engine
------------------------------------------------------------------------
Under the Paint Brush tab, toggle automatic scatter randomization:
- **Random Rotation:** Define Min/Max Euler ranges per axis (e.g., Y: 0 to 360).
- **Random Scale:** Uniform (preserves aspect ratio) or Non-Uniform sizing limits
  (e.g., scale bounds between 0.8x and 1.3x).

------------------------------------------------------------------------
6. Editor Physics Simulation (Drop, Stack & Roll)
------------------------------------------------------------------------
DropIt! features a premium physics baking engine using modern Unity 6 
`Physics.Simulate()` stepping. This allows you to simulate natural gravity 
directly inside Editor mode without entering Play Mode:

1. Drag-and-drop or paint objects roughly in the air above their destination.
2. Select the objects you want to drop.
3. Open the **Physics Drop** tab in the DropIt Dashboard.
4. Adjust parameters like:
   - **Gravity Multiplier**
   - **Bounciness**
   - **Maximum Steps / Iterations**
5. Click **Simulate Physics Drop on Selection**.
6. The selection will fall, bounce, roll, and settle realistically on 
   colliders and on top of each other.
7. Once they settle, their final positions are baked, and temporary physics 
   components (Rigidbodies/Colliders) are cleanly removed automatically.

------------------------------------------------------------------------
7. Perfect Snap to Ground
------------------------------------------------------------------------
Select floating objects and click **Snap Selected Directly to Ground** 
under the Physics tab. This instantly projects the objects down onto the nearest
collider surface below using your active Pivot Alignment rule.

========================================================================
For updates, detailed tutorials, and level-design templates, visit our 
Asset Store publisher page!
========================================================================
