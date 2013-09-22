// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGraphicsOptionMenuAcceptButton.cs
// Accept button script for the GraphicsOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Accept button script for the GraphicsOptionsMenu.
/// </summary>
public class GuiGraphicsOptionMenuAcceptButton : AGuiMenuAcceptButtonBase {

    private int _qualitySetting;

    protected override void InitializeTooltip() {
        tooltip = "Click to implement Option changes.";
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        base.RecordPopupListState(popupListName, selectionName);
        //D.Log("SelectionName = {0}.", selectionName);
        _qualitySetting = QualitySettings.names.IndexOf<string>(selectionName);
        // more popupLists here
    }

    protected override void OnPopupListSelectionChange(string selectionName) {
        base.OnPopupListSelectionChange(selectionName);
        ValidateState();
    }

    protected override void OnLeftClick() {
        ValidateState();
        GraphicsOptionSettings settings = new GraphicsOptionSettings();
        settings.QualitySetting = _qualitySetting;
        _eventMgr.Raise<GraphicsOptionsAcceptedEvent>(new GraphicsOptionsAcceptedEvent(this, settings));
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(Utility.IsInRange(_qualitySetting, Constants.Zero, QualitySettings.names.Length - 1), "QualitySetting = {0}.".Inject(_qualitySetting));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

