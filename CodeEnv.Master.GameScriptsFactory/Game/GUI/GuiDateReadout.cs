// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiDateReadout.cs
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
public class GuiDateReadout : GuiLabelReadoutBase {

    protected override void Initialize() {
        base.Initialize();
        RefreshDateReadout();
        tooltip = "The current date in the game.";
    }

    void Update() {
        if (ToUpdate()) {
            RefreshDateReadout();
        }
    }

    private void RefreshDateReadout() {
        readoutLabel.text = GameTime.Date.FormattedDate;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

