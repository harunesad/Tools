========================================================================
                     SmartSave - Complete Save System
========================================================================
Thank you for using SmartSave! Below is a quick integration guide to 
get your game's save system running with zero coding in less than 5 minutes.

------------------------------------------------------------------------
1. Opening the Live Debugger Window
------------------------------------------------------------------------
Access the main visual inspector at any time:
Menu: Tools -> SmartSave Debugger

During Play Mode, this window lets you inspect, edit, and manage all 
saveable values and profiles in real-time.

------------------------------------------------------------------------
2. Drop-In Auto-Trackers (Zero Coding)
------------------------------------------------------------------------
Simply drag and drop these ready-to-use components onto any GameObject:

- AutoSaveTransform: Automatically tracks and saves Position, Rotation, 
  and Scale (compatible with Rigidbody physics).
- AutoSaveActiveState: Remembers if a GameObject was active or disabled. 
  Perfect for collectables, chests, or persistent enemy states.
- AutoSaveScene: Put this on a manager object to automatically remember 
  and reload the player's last active scene.

------------------------------------------------------------------------
3. RPG & Option Templates (Ready to Use)
------------------------------------------------------------------------
We provide pre-built, highly optimized templates to manage common systems:

- PlayerProgression: Attach to your Player. Saves PlayerName, Level, XP, 
  Gold, Health, and Mana.
- GameSettings: Saves Master Volume, SFX, Music, Graphic Quality index, 
  Fullscreen mode, and Active Language.

------------------------------------------------------------------------
4. Custom Script Saving [Saveable] Attribute
------------------------------------------------------------------------
To save custom variables in your own scripts:
1. Inherit your class from `SaveableBehaviour` instead of `MonoBehaviour`.
2. Place the `[Saveable]` attribute above any public/private field:

   using SmartSave;
   public class MyGameScript : SaveableBehaviour 
   {
       [Saveable] private int playerScore = 0;
       [Saveable] public string currentWeaponID = "Sword";
   }

The system will automatically find, encrypt, and serialize these variables.

------------------------------------------------------------------------
5. Manual Script Saving via API
------------------------------------------------------------------------
To trigger save/load at custom times (e.g. at checkpoints):
- To Save:    SmartSave.SaveManager.Save();
- To Load:    SmartSave.SaveManager.Load();
- Change Slot: SmartSave.SaveManager.SetActiveSlot(2);

------------------------------------------------------------------------
6. Core Cryptography & Security
------------------------------------------------------------------------
SmartSave comes with built-in AES (Advanced Encryption Standard) 
cryptography. 
You can toggle encryption and change the password key at any time in the 
"Settings" tab of the SmartSave Debugger window.

========================================================================
For support or advanced guides, please visit our Asset Store page!
