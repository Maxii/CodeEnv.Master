// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseOnLoadOption.cs
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
public class GuiPauseOnLoadOption : GuiCheckboxBase {

    protected override void Initialize() {
        base.Initialize();
        checkbox.isChecked = playerPrefsMgr.IsPauseOnLoadPref;
        tooltip = "Check this box if you wish to begin Paused after the game loads.";
    }

    protected override void OnCheckBoxStateChange(bool state) {
        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

        // UNDONE
        playerPrefsMgr.IsPauseOnLoadPref = state;
        Debug.LogWarning("OnPauseAfterReloadOptionChange() is not yet fully implemented.");
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

