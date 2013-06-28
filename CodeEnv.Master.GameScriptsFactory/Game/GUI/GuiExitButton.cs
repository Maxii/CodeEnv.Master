// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiExitButton.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiExitButton : AGuiButtonBase {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        tooltip = "Exit the Game.";
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
    }

    protected override void OnButtonClick(GameObject sender) {
        // UNDONE confirmation popup
        eventMgr.Raise<ExitGameEvent>(new ExitGameEvent(this));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

