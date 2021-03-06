﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedOnLoadOption.cs
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
[Obsolete]
public class GuiGameSpeedOnLoadOption : AGuiPopupList {

    protected override void Initialize() {
        base.Initialize();
        _popupList.selection = playerPrefsMgr.GameSpeedOnLoad.GetName();
        tooltip = "Set the game speed you wish to begin with after the game loads.";
    }

    protected override void OnPopupListSelectionChange(string item) {
        GameSpeed speed;
        if (!Enums<GameSpeed>.TryParse(item, true, out speed)) {
            WarnOnIncorrectName(item);
            return;
        }

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

        // UNDONE
        //playerPrefsMgr.GameSpeedOnLoad = speed;
        //Debug.LogWarning("GameSpeedOnLoadOption is not yet fully implemented.");
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

