// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GraphicsOptionMenuAcceptButton.cs
// Accept button for the GraphicsOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Accept button for the GraphicsOptionsMenu.
/// </summary>
public class GraphicsOptionMenuAcceptButton : AGuiMenuAcceptButton {

    protected override IList<KeyCode> ValidKeys { get { return new List<KeyCode>() { KeyCode.Return }; } }

    protected override string TooltipContent { get { return "Click to implement Option changes."; } }

    private string _qualitySetting;
    private bool _isElementIconsEnabled;

    protected override void RecordPopupListState(GuiElementID popupListID, string selection, string convertedSelection) {
        base.RecordPopupListState(popupListID, selection, convertedSelection);
        switch (popupListID) {
            case GuiElementID.QualitySettingPopupList:
                _qualitySetting = selection;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    protected override void RecordCheckboxState(GuiElementID checkboxID, bool isChecked) {
        base.RecordCheckboxState(checkboxID, isChecked);
        switch (checkboxID) {
            case GuiElementID.ElementIconsCheckbox:
                _isElementIconsEnabled = isChecked;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(checkboxID));
        }
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        base.HandleValidClick();
        GraphicsOptionSettings settings = new GraphicsOptionSettings() {
            QualitySetting = _qualitySetting,
            IsElementIconsEnabled = _isElementIconsEnabled
        };
        _playerPrefsMgr.RecordGraphicsOptions(settings);
    }

    #endregion

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        if (!QualitySettings.names.ToList().Contains(_qualitySetting)) {
            D.Error("QualitySetting {0} not present among {1}.", _qualitySetting, QualitySettings.names.Concatenate());
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

