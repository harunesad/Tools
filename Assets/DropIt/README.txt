========================================================================
             DropIt! - Physics Scatter & Level Design Tool
========================================================================
Thank you for using DropIt! Below is a quick-start guide to scattering, 
snapping, and physically simulating object placements directly in the 
Unity Editor Viewport.

------------------------------------------------------------------------
1. Opening the DropIt Dashboard
------------------------------------------------------------------------
Access the main control center:
Menu: Tools -> DropIt -> Scatter Dashboard

We recommend docking this window next to your Inspector for a smooth 
level design workflow.

------------------------------------------------------------------------
2. Building Your Prefab Palette
------------------------------------------------------------------------
1. Open the DropIt Dashboard.
2. Select 3D prefabs (rocks, trees, crates, debris) from your Project view.
3. Drag and drop them directly into the "Active Brush Palette" drop box.
4. You will see preview thumbnails and a simple button to clear them.

------------------------------------------------------------------------
3. Using the Paint Brush
------------------------------------------------------------------------
1. Go to the "Paint Brush" tab.
2. Check "Activate Paint Brush".
3. Hold Left Mouse Button (LMB) and drag over any collider surface 
   (Terrain, ProBuilder mesh, or static objects) in the Scene View to paint.
4. Customize parameters:
   - Brush Size (Radius): Controls the coverage area.
   - Density: Number of prefabs spawned per mouse stroke.
   - Align to Slope: Automatically rotates objects to match surface normals.
   - Prevent Overlap: Ensures objects do not spawn inside each other.

------------------------------------------------------------------------
4. Randomization Settings
------------------------------------------------------------------------
Under the Paint Brush tab, you can enable automatic randomization:
- Random Rotation: Set Min/Max Euler angles (e.g., Y: 0 to 360).
- Random Scale: Choose Uniform (proportional) or Non-Uniform limits 
  (e.g., scale between 0.8x and 1.3x).

------------------------------------------------------------------------
5. Editor Physics Simulation (Drop & Stack)
------------------------------------------------------------------------
You can simulate realistic gravity falling directly inside Editor mode 
without entering Play mode:
1. Place several objects roughly in the air above where you want them.
2. Select all of them in the scene hierarchy.
3. Go to the "Physics Drop" tab in the DropIt Dashboard.
4. Customize Gravity Multiplier and Bounciness.
5. Click "Simulate Physics Drop on Selection".
6. The objects will fall, collide with terrain and each other naturally. 
7. Once they settle, their final positions are automatically baked, and 
   temporary physics components are removed cleanly.

------------------------------------------------------------------------
6. Snap to Ground
------------------------------------------------------------------------
Select any object floating in the air and click "Snap Selected Directly to 
Ground" under the Physics tab to instantly snap its pivot point to the 
nearest collider surface below.

========================================================================
For support or advanced level design templates, visit our Asset Store page!
