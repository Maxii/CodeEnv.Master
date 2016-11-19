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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Exit button script.
/// </summary>
public class GuiExitButton : AGuiButton {

    protected override string TooltipContent { get { return "Exit the Game."; } }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        _gameMgr.ExitGame();
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

