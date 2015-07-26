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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Changes the InputMode to that selected on Button LeftClick.
/// </summary>
public class InputModeControlButton : AGuiButton {

    public GameSceneInputMode inputModeOnClick;

    protected override void Awake() {
        base.Awake();
        if (inputModeOnClick == default(GameSceneInputMode)) {
            D.WarnContext("{0} has not set {1}.".Inject(GetType().Name, typeof(GameSceneInputMode).Name), gameObject);
        }
    }

    protected override void OnLeftClick() {
        D.Assert(_gameMgr.CurrentScene != SceneLevel.LobbyScene);
        GameInputMode gameInputMode;
        switch (inputModeOnClick) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(inputModeOnClick));
        }
        //D.Log("{0} is about to set InputMode to {1}.", GetType().Name, gameInputMode.GetValueName());
        InputManager.Instance.InputMode = gameInputMode;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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
        /// Input mode active when no UI popup windows are present. 
        /// UI element interaction is allowed along with all 3D world interaction.
        /// </summary>
        Normal

    }

    #endregion
}

