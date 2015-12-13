// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LobbyInputManager.cs
// Singleton that manages all user input allowed in the Lobby scene.
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
/// Singleton that manages all user input allowed in the Lobby scene..
/// Mouse events originate from the Ngui event system.
/// </summary>
[Obsolete]
public class LobbyInputManager : AInputManager<LobbyInputManager> {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InputMode = GameInputMode.PartialPopup;
    }

    /// <summary>
    /// Called when the GameInputMode changes.
    /// Notes: Changing the eventReceiverMask of the _uiEventDispatcher covers all OnHover, OnClick, OnDrag and OnPress events for UI elements.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    protected override void OnInputModeChanged() {
        switch (InputMode) {
            case GameInputMode.PartialPopup:
                D.Log("{0} is now {1}.", typeof(GameInputMode).Name, InputMode.GetValueName());
                UIEventDispatcher.eventReceiverMask = UIEventDispatcherMask_PopupInputOnly;
                break;
            case GameInputMode.NoInput:
            case GameInputMode.FullPopup:
            case GameInputMode.Normal:
            case GameInputMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(InputMode));
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

