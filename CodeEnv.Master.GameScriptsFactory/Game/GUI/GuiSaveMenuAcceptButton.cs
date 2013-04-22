// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSaveMenuAcceptButton.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR
#define UNITY_EDITOR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiSaveMenuAcceptButton : GuiMenuAcceptButtonBase {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        tooltip = "Click to save to PlayerPrefs.";
    }

    protected override void OnButtonClick(GameObject sender) {
        eventMgr.Raise<SaveGameEvent>(new SaveGameEvent(this, "Game"));
    }

    protected override void RecordCheckboxState(string checkboxName, bool checkedState) { }

    protected override void RecordPopupListState(string selectionName) { }

    protected override void RecordSliderState(float sliderValue) { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

