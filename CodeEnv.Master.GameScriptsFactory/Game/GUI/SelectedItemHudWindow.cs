// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedItemHudWindow.cs
// Fixed position HudWindow displaying a customized Form for something that has been 'selected'.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Fixed position HudWindow displaying a customized Form for something that has been 'selected'.
/// <remarks>The current version is located on the bottom left of the screen and appears when called to Show().</remarks>
/// <remarks>6.21.17 Currently used by Items, UnitDesigns and EquipmentStats.</remarks>
/// </summary>
public class SelectedItemHudWindow : AHudWindow<SelectedItemHudWindow>, ISelectedItemHudWindow {

    /// <summary>
    /// The local-space corners of this window. Order is bottom-left, top-left, top-right, bottom-right.
    /// Used by HoveredItemHudWindow to reposition itself to avoid interfering with this fixed window.
    /// </summary>
    public Vector3[] LocalCorners { get { return _backgroundWidget.localCorners; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.SelectedItemHudWindow = Instance;
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
    }

    public void Show(FormID formID, AItemReport report) {
        var form = PrepareForm(formID);
        (form as AReportForm).Report = report;
        ShowForm(form);
    }

    public void Show(FormID formID, AUnitDesign design) {
        //D.Log("{0}.Show({1}) called.", DebugName, design.DebugName);
        var form = PrepareForm(formID);
        (form as AUnitDesignForm).Design = design;
        ShowForm(form);
    }

    public void Show(FormID formID, AEquipmentStat stat) {
        var form = PrepareForm(formID);
        (form as AEquipmentForm).EquipmentStat = stat;
        ShowForm(form);
    }

    protected override void PositionWindow() { }

    protected override void Cleanup() {
        base.Cleanup();
        GameReferences.SelectedItemHudWindow = null;
    }


}

