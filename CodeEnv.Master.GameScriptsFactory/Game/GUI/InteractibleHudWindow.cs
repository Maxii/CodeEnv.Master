// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InteractibleHudWindow.cs
// Fixed position HudWindow displaying interactable info and controls. 
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
/// Fixed position HudWindow displaying interactible info and controls. 
/// <remarks>7.9.17 Current uses include user-owned selected Items. Can also be used to supplement screens.</remarks>
/// <remarks>The current version is located on the bottom left of the screen and appears when called to Show().</remarks>
/// </summary>
public class InteractibleHudWindow : AHudWindow<InteractibleHudWindow>, IInteractibleHudWindow {

    /// <summary>
    /// The local-space corners of this window. Order is bottom-left, top-left, top-right, bottom-right.
    /// <remarks>Used by HoveredHudWindow to reposition itself to avoid interfering with this fixed window.</remarks>
    /// </summary>
    public Vector3[] LocalCorners { get { return _backgroundWidget.localCorners; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.InteractibleHudWindow = Instance;
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            D.Warn("{0}: Can't UIPanel.widgetsAreStatic = true?", DebugName);
        }
    }

    public void Show(FormID formID, AItemData itemData) {
        var form = PrepareForm(formID);
        (form as AItemDataForm).ItemData = itemData;
        ShowForm(form);
    }

    public void Show(FormID formID, AItemReport itemReport) {
        var form = PrepareForm(formID);
        (form as AItemReportForm).Report = itemReport;
        ShowForm(form);
    }

    protected override void PositionWindow() { }

    protected override void Cleanup() {
        base.Cleanup();
        GameReferences.InteractibleHudWindow = null;
    }


}

