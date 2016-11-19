// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectedItemHudWindow.cs
//  Fixed position HudWindow displaying a customized HudForm for a Selected Item.
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
/// Fixed position HudWindow displaying a customized HudForm for a Selected Item.
/// The current version is located on the bottom left of the screen and only appears
/// when an ISelectable Item has been selected.
/// </summary>
public class SelectedItemHudWindow : AHudWindow<SelectedItemHudWindow>, ISelectedItemHudWindow {

    /// <summary>
    /// The local-space corners of this window. Order is bottom-left, top-left, top-right, bottom-right.
    /// Used by HoveredItemHudWindow to reposition itself to avoid interfering with this fixed window.
    /// </summary>
    public Vector3[] LocalCorners { get { return _backgroundWidget.localCorners; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        References.SelectedItemHudWindow = Instance;
    }

    public void Show(FormID formID, AItemReport report) {
        var form = PrepareForm(formID);
        (form as AReportForm).Report = report;
        ShowForm(form);
    }

    protected override void PositionWindow() { }

    protected override void Cleanup() {
        base.Cleanup();
        References.SelectedItemHudWindow = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

