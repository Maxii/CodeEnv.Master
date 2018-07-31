// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePlayOptionMenuAcceptButton.cs
// Accept button for the GamePlayOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Accept button for the GamePlayOptionsMenu.
/// </summary>
public class GamePlayOptionMenuAcceptButton : AGuiMenuAcceptButton {

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Return };

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override string TooltipContent { get { return "Accept Option changes."; } }

    private bool _isCameraRollEnabled;
    private bool _isZoomOutOnCursorEnabled;
    private bool _isResetOnFocusEnabled;
    private bool _isPauseOnLoadEnabled;
    private bool _isAiHandlesUserCmdModuleInitialDesignsEnabled;
    private bool _isAiHandlesUserCmdModuleRefitDesignsEnabled;
    private bool _isAiHandlesUserCentralHubInitialDesignsEnabled;
    private bool _isAiHandlesUserElementRefitDesignsEnabled;
    private GameSpeed _gameSpeedOnLoad;

    protected override void RecordCheckboxState(GuiElementID checkboxID, bool isChecked) {
        base.RecordCheckboxState(checkboxID, isChecked);
        switch (checkboxID) {
            case GuiElementID.PauseOnLoadCheckbox:
                _isPauseOnLoadEnabled = isChecked;
                break;
            case GuiElementID.CameraRollCheckbox:
                _isCameraRollEnabled = isChecked;
                break;
            case GuiElementID.ResetOnFocusCheckbox:
                _isResetOnFocusEnabled = isChecked;
                break;
            case GuiElementID.ZoomOutOnCursorCheckbox:
                _isZoomOutOnCursorEnabled = isChecked;
                break;
            case GuiElementID.AiHandlesUserCmdModuleInitialDesignsCheckbox:
                _isAiHandlesUserCmdModuleInitialDesignsEnabled = isChecked;
                break;
            case GuiElementID.AiHandlesUserCmdModuleRefitDesignsCheckbox:
                _isAiHandlesUserCmdModuleRefitDesignsEnabled = isChecked;
                break;
            case GuiElementID.AiHandlesUserCentralHubInitialDesignsCheckbox:
                _isAiHandlesUserCentralHubInitialDesignsEnabled = isChecked;
                break;
            case GuiElementID.AiHandlesUserElementRefitDesignsCheckbox:
                _isAiHandlesUserElementRefitDesignsEnabled = isChecked;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(checkboxID));
        }
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selection, string convertedSelection) {
        base.RecordPopupListState(popupListID, selection, convertedSelection);
        switch (popupListID) {
            case GuiElementID.GameSpeedOnLoadPopupList:
                _gameSpeedOnLoad = Enums<GameSpeed>.Parse(selection);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    protected override void HandleValidClick() {
        base.HandleValidClick();
        GamePlayOptionSettings settings = new GamePlayOptionSettings() {
            IsCameraRollEnabled = _isCameraRollEnabled,
            IsZoomOutOnCursorEnabled = _isZoomOutOnCursorEnabled,
            IsResetOnFocusEnabled = _isResetOnFocusEnabled,
            IsPauseOnLoadEnabled = _isPauseOnLoadEnabled,
            IsAiHandlesUserCmdModuleInitialDesignsEnabled = _isAiHandlesUserCmdModuleInitialDesignsEnabled,
            IsAiHandlesUserCmdModuleRefitDesignsEnabled = _isAiHandlesUserCmdModuleRefitDesignsEnabled,
            IsAiHandlesUserCentralHubInitialDesignsEnabled = _isAiHandlesUserCentralHubInitialDesignsEnabled,
            IsAiHandlesUserElementRefitDesignsEnabled = _isAiHandlesUserElementRefitDesignsEnabled,
            GameSpeedOnLoad = _gameSpeedOnLoad
        };
        _playerPrefsMgr.RecordGamePlayOptions(settings);
    }

    #region Event and Property Change Handlers

    #endregion

    protected override void __ValidateCapturedState() {
        base.__ValidateCapturedState();
        D.AssertNotDefault((int)_gameSpeedOnLoad);
    }

    protected override void Cleanup() { }


}

