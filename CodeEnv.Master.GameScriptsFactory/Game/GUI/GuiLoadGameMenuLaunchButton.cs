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

using System.Diagnostics;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Launch button script for the Load[Saved]GameMenu.
/// </summary>
public class GuiLoadGameMenuLaunchButton : AGuiMenuAcceptButtonBase {

    protected override string TooltipContent {
        get { return "Launch the selected Saved Game."; }
    }

    private string _selectedGameCaption = string.Empty;

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        base.RecordPopupListState(popupListName, selectionName);
        _selectedGameCaption = selectionName;
        //D.Log("{0} has recorded PopupList [{1}]'s current selection: {2}.", GetType().Name, popupListName, selectionName);
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

