// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseResumeOnClick.cs
// Gui class that initiates a PauseGameEvent containing the designated PauseGameCommand
// that can be entered via the Editor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;

/// <summary>
/// Gui class that initiates a GuiPauseEvent containing the designated GuiPauseCommand
/// that can be entered via the Editor.
/// </summary>
public class GuiPauseResumeOnClick : GuiButtonBase {

    /// <summary>
    /// The GuiPauseCommand to be issued via event when the button is clicked.
    /// </summary>
    public GuiPauseCommand pauseCommand;

    protected override void OnButtonClick(GameObject sender) {
        switch (pauseCommand) {
            case GuiPauseCommand.GuiAutoPause:
            case GuiPauseCommand.GuiAutoResume:
            case GuiPauseCommand.UserResume:
            case GuiPauseCommand.UserPause:
                eventMgr.Raise<GuiPauseEvent>(new GuiPauseEvent(pauseCommand));
                break;
            case GuiPauseCommand.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(pauseCommand));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

