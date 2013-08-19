// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseResumeOnClick.cs
// Gui class that initiates a GamePauseStateChangedEvent containing the designated GamePauseState
// that can be entered via the Editor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Gui class that initiates a GuiPauseRequestEvent containing the designated PauseRequest
/// that can be entered via the Editor.
/// </summary>
public class GuiPauseResumeOnClick : AGuiButtonBase {

    /// <summary>
    /// The PauseRequest to be issued via event when the button is clicked.
    /// </summary>
    public PauseRequest pauseCommand;

    protected override void OnLeftClick() {
        switch (pauseCommand) {
            case PauseRequest.GuiAutoPause:
            case PauseRequest.GuiAutoResume:
            case PauseRequest.PriorityResume:
            case PauseRequest.PriorityPause:
                _eventMgr.Raise<GuiPauseRequestEvent>(new GuiPauseRequestEvent(this, pauseCommand));
                break;
            case PauseRequest.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pauseCommand));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

