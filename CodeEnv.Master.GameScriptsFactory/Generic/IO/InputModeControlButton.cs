// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InputModeControlButton.cs
// Changes the InputMode to that selected on Button LeftClick.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Changes the InputMode to that selected on Button LeftClick.
/// </summary>
public class InputModeControlButton : AGuiButton {

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Return };

    [Tooltip("The GameSceneInputMode to use when clicked")]
    [SerializeField]
    private GameSceneInputMode _inputModeOnClick = GameSceneInputMode.None;

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override void HandleValidClick() {
        D.AssertNotEqual(SceneID.LobbyScene, _gameMgr.CurrentSceneID);
        GameInputMode gameInputMode;
        switch (_inputModeOnClick) {
            case GameSceneInputMode.PartialPopup:
                gameInputMode = GameInputMode.PartialPopup;
                break;
            case GameSceneInputMode.FullPopup:
                gameInputMode = GameInputMode.FullPopup;
                break;
            case GameSceneInputMode.Normal:
                gameInputMode = GameInputMode.Normal;
                break;
            case GameSceneInputMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_inputModeOnClick));
        }
        //D.Log("{0} is about to set InputMode to {1}.", GetType().Name, gameInputMode.GetValueName());
        InputManager.Instance.InputMode = gameInputMode;
    }

    #region Event and Property Change Handlers

    #endregion

    protected override void Cleanup() { }

    #region Debug

    protected override void __Validate() {
        base.__Validate();
        if (_inputModeOnClick == default(GameSceneInputMode)) {
            D.WarnContext(this, "{0} has not set {1}.", DebugName, typeof(GameSceneInputMode).Name);
        }
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Input mode changes available while in the GameScene.
    /// </summary>
    public enum GameSceneInputMode {

        /// <summary>
        /// For error detection.
        /// </summary>
        None,

        /// <summary>
        /// InputMode active when a UI window pops up that only fills part of the screen.
        /// UI element interaction is allowed along with screen edge and arrow key panning
        /// of the 3D game world for better viewing. All other 3D world interaction is disabled.
        /// </summary>
        PartialPopup,

        /// <summary>
        /// InputMode active when a window pops up that fills the whole screen.
        /// UI element interaction is allowed but all 3D world interaction is disabled.
        /// </summary>
        FullPopup,

        /// <summary>
        /// Input mode active when no UI pop up windows are present. 
        /// UI element interaction is allowed along with all 3D world interaction.
        /// </summary>
        Normal

    }

    #endregion

}

