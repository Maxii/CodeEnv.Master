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

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiExitButton : GuiButtonBase {

    protected override void Initialize() {
        base.Initialize();
        tooltip = "Exit the Game.";
    }

    protected override void OnButtonClick(GameObject sender) {
        // UNDONE confirmation popup
        eventMgr.Raise<ExitGameEvent>(new ExitGameEvent());
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

