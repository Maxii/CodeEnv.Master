// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitHudWindow.cs
// Fixed position HudWindow displaying Unit info and controls. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Fixed position HudWindow displaying Unit info and controls.
/// <remarks>7.9.17 Current uses include user-owned selected Cmd Items. </remarks>
/// <remarks>The current version is located on the bottom of the screen and appears when called to Show().</remarks>
/// </summary>
public class UnitHudWindow : AHudWindow<UnitHudWindow> {

    /// <summary>
    /// The local-space corners of this window. Order is bottom-left, top-left, top-right, bottom-right.
    /// </summary>
    public Vector3[] LocalCorners { get { return _backgroundWidget.localCorners; } }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
    }

    public void Show(FormID formID, FleetCmdItem unitItem) {
        var form = PrepareForm(formID);
        (form as UnitHudUserFleetForm).SelectedUnit = unitItem;
        ShowForm(form);
    }

    public void Show(FormID formID, AUnitBaseCmdItem unitItem) {
        var form = PrepareForm(formID);
        (form as AUnitHudUserBaseForm).SelectedUnit = unitItem;
        ShowForm(form);
    }

    protected override void PositionWindow() { }


}

