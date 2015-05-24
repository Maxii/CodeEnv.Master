// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiLoadGameMenuLaunchButton.cs
// Launch button script for the Load[Saved]GameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Diagnostics;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Launch button script for the Load[Saved]GameMenu.
/// </summary>
public class GuiLoadGameMenuLaunchButton : AGuiMenuAcceptButton {

    protected override string TooltipContent {
        get { return "Launch the selected Saved Game."; }
    }

    private string _selectedGameCaption = string.Empty;

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selectionName) {
        base.RecordPopupListState(popupListID, selectionName);
        switch (popupListID) {
            case GuiElementID.SavedGamesPopupList:
                _selectedGameCaption = selectionName;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    protected override void OnPopupListSelectionChange() {
        base.OnPopupListSelectionChange();
        ValidateState();
    }

    protected override void OnLeftClick() {
        //LoadSavedGame();
        __LoadDummySavedGame();
    }

    private void __LoadDummySavedGame() {
        _gameMgr.LoadSavedGame(_selectedGameCaption);
    }

    private void LoadSavedGame() {
        if (LevelSerializer.SavedGames.Count > 0) {
            Arguments.ValidateForContent(_selectedGameCaption);
            _gameMgr.LoadSavedGame(_selectedGameCaption);
        }
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        // selectedGameCaption can be empty if there are no saved games
        if (_selectedGameCaption == string.Empty) {
            D.Warn("Selected Game Caption to Load is empty.");
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

