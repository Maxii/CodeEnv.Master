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

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Accept button for the GraphicsOptionsMenu.
/// </summary>
public class GuiGraphicsOptionMenuAcceptButton : AGuiMenuAcceptButton {

    protected override string TooltipContent {
        get { return "Click to implement Option changes."; }
    }

    private string _qualitySetting;
    private bool _isElementIconsEnabled;

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(GuiMenuElementID popupListID, string selectionName) {
        base.RecordPopupListState(popupListID, selectionName);
        switch (popupListID) {
            case GuiMenuElementID.QualitySettingPopupList:
                _qualitySetting = selectionName;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    protected override void RecordCheckboxState(GuiMenuElementID checkboxID, bool checkedState) {
        base.RecordCheckboxState(checkboxID, checkedState);
        switch (checkboxID) {
            case GuiMenuElementID.ElementIconsCheckbox:
                _isElementIconsEnabled = checkedState;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(checkboxID));
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
        D.Assert(QualitySettings.names.ToList().Contains(_qualitySetting), "QualitySetting {0} not present among {1}.".Inject(_qualitySetting, QualitySettings.names.Concatenate()));
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

