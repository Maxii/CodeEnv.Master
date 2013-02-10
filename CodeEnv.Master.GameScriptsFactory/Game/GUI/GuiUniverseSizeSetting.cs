// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiUniverseSizeSetting.cs
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
public class GuiUniverseSizeSetting : GuiPopupListBase {

    protected override void Initialize() {
        base.Initialize();
        popupList.selection = playerPrefsMgr.UniverseSizePref.GetName();
        tooltip = "Choose the size of the Universe for your game.";
    }

    protected override void OnPopupListSelectionChange(string item) {
        UniverseSize size;
        if (!Enums<UniverseSize>.TryParse(item, true, out size)) {
            WarnOnIncorrectName(item);
            return;
        }

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));
        if (size != UniverseSize.Normal) {
            Debug.LogError("Universe Size Change is only allowed during New Game Setup.");
            return;
        }
        GameManager.UniverseSize = size;
        playerPrefsMgr.UniverseSizePref = size;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

