// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSaveMenuAcceptButton.cs
//  Accept button script for the SaveGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Accept button script for the SaveGameMenu.
/// </summary>
public class GuiSaveMenuAcceptButton : AGuiMenuAcceptButtonBase {

    protected override void InitializeTooltip() {
        tooltip = "Click to save to PlayerPrefs.";
    }

    protected override void OnLeftClick() {
        _eventMgr.Raise<SaveGameEvent>(new SaveGameEvent(this, "Game"));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

