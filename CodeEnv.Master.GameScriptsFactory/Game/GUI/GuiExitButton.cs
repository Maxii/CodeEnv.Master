// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiExitButton.cs
// Exit button script.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Exit button script.
/// </summary>
public class GuiExitButton : AGuiButtonBase {

    protected override void Awake() {
        base.Awake();
        tooltip = "Exit the Game.";
    }

    protected override void Start() {
        base.Start();
    }

    protected override void OnLeftClick() {
        // UNDONE confirmation popup
        _eventMgr.Raise<ExitGameEvent>(new ExitGameEvent(this));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

