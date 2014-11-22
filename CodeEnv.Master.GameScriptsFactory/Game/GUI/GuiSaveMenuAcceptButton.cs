﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSaveMenuAcceptButton.cs
//  Accept button for the SaveGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Accept button for the SaveGameMenu.
/// </summary>
public class GuiSaveMenuAcceptButton : AGuiMenuAcceptButtonBase {

    protected override string TooltipContent {
        get { return "Click to save game."; }
    }

    protected override void OnLeftClick() {
        _gameMgr.SaveGame("Game");
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

