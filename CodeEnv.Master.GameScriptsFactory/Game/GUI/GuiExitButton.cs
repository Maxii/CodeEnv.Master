﻿// --------------------------------------------------------------------------------------------------------------------
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
#define DEBUG_WARN
#define DEBUG_ERROR

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

    protected override void OnLeftClick() {
        // UNDONE confirmation popup
        _eventMgr.Raise<ExitGameEvent>(new ExitGameEvent(this));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

