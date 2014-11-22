// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInputMode.cs
// Enum delineating different combinations of input events supported in the game
// covering the 3D world, main camera movement, UI  and PlayerViewMode events.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Enum delineating different combinations of input events supported in the game
    /// covering the 3D world, main camera movement, UI  and PlayerViewMode events.
    /// </summary>
    public enum GameInputMode {

        /// <summary>
        /// For error detection.
        /// </summary>
        None,

        /// <summary>
        /// No 3D world, camera movement, UI or PlayerViewMode input events are supported.
        /// </summary>
        NoInput,

        /// <summary>
        /// Only input events aiding an open Popup that doesn't cover the screen are supported. In addition to interaction
        /// with the popup, this support includes key and screen edge main camera movement when in the GameScene so the 
        /// player can move the camera to see world objects that might otherwise be obscured by the popup.
        /// </summary>
        PartialScreenPopup,

        /// <summary>
        /// Only input events allowing interaction with an open Popup that covers the screen are supported. As there
        /// is nothing visible around the popup, key and screen edge main camera movement is not supported.
        /// </summary>
        FullScreenPopup,

        /// <summary>
        /// All 3D world, camera movement, UI and PlayerViewMode input events aiding normal game play are supported.
        /// These include all events except PopupMenu interaction.
        /// </summary>
        Normal

    }
}

