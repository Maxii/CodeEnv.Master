// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInputMode.cs
// Input modes available in the Game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  Input modes available in the Game.
    /// Modes delineate different combinations of input events supported in the game
    /// covering the 3D world, main camera movement, UI  and PlayerViewMode events.
    /// </summary>
    public enum GameInputMode {

        /// <summary>
        /// For error detection.
        /// </summary>
        None,

        /// <summary>
        /// No input events are supported.
        /// </summary>
        NoInput,

        /// <summary>
        /// Valid only while in the LobbyScene. Only input events on the UI layer are supported as there is no main camera in the Lobby.
        /// </summary>
        Lobby,

        /// <summary>        
        /// Valid only while in the GameScene. 
        /// InputMode active when a UI window pops up that only fills part of the screen.
        /// UI element interaction is allowed along with screen edge and arrow key panning
        /// of the 3D game world for better viewing. All other 3D world interaction is disabled.
        /// </summary>
        PartialPopup,

        /// <summary>        
        /// Valid only while in the GameScene.
        /// InputMode active when a window pops up that fills the whole screen.
        /// UI element interaction is allowed but 3D world interaction via the main camera is disabled.
        /// </summary>
        FullPopup,

        /// <summary>
        /// Valid only while in the GameScene. 
        /// Input mode active when no UI popup windows are present. 
        /// UI element interaction is allowed along with all 3D world interaction.
        /// </summary>
        Normal

    }
}

