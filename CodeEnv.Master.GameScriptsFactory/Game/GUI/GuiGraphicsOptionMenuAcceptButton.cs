// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGraphicsOptionMenuAcceptButton.cs
// Accept button for the GraphicsOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Accept button for the GraphicsOptionsMenu.
/// </summary>
public class GuiGraphicsOptionMenuAcceptButton : AGuiMenuAcceptButtonBase {

    protected override string TooltipContent {
        get { return "Click to implement Option changes."; }
    }

    private int _qualitySetting;
    private bool _isElementIconsEnabled;

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        base.RecordPopupListState(popupListName, selectionName);
        //D.Log("SelectionName = {0}.", selectionName);
        _qualitySetting = QualitySettings.names.IndexOf<string>(selectionName);
        // TODO more popupLists here
    }

    protected override void RecordCheckboxState(string checkboxName, bool checkedState) {
        if (checkboxName.Contains("element")) {
            _isElementIconsEnabled = checkedState;
        }
        else {
            D.Error("Name of Checkbox {0} not found.", checkboxName);
        }
    }

    protected override void OnPopupListSelectionChange() {
        base.OnPopupListSelectionChange();
        ValidateState();
    }

    protected override void OnLeftClick() {
        ValidateState();
        GraphicsOptionSettings settings = new GraphicsOptionSettings() {
            QualitySetting = _qualitySetting,
            IsElementIconsEnabled = _isElementIconsEnabled
        };
        _playerPrefsMgr.RecordGraphicsOptions(settings);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(Utility.IsInRange(_qualitySetting, Constants.Zero, QualitySettings.names.Length - 1), "QualitySetting = {0}.".Inject(_qualitySetting));
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

