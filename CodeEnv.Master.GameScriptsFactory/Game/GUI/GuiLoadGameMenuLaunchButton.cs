﻿// --------------------------------------------------------------------------------------------------------------------
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

// default namespace

using System.Diagnostics;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Launch button script for the Load[Saved]GameMenu.
/// </summary>
public class GuiLoadGameMenuLaunchButton : AGuiMenuAcceptButtonBase {

    private string selectedGameCaption = string.Empty;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        tooltip = "Load and launch the selected Saved Game.";
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordCheckboxState(string checkboxName, bool checkedState) {
        //UNDONE
    }

    protected override void RecordPopupListState(string selectionName) {
        selectedGameCaption = selectionName;
    }

    protected override void RecordSliderState(float sliderValue) {
        // UNDONE
    }

    protected override void OnCheckboxStateChange(bool state) {
        base.OnCheckboxStateChange(state);
    }

    protected override void OnPopupListSelectionChange(string item) {
        RecordPopupListState(item);
        ValidateState();
    }

    protected override void OnSliderValueChange(float value) {
        base.OnSliderValueChange(value);
    }

    protected override void OnLeftClick() {
        if (LevelSerializer.SavedGames.Count > 0) {
            Arguments.ValidateForContent(selectedGameCaption);
            _eventMgr.Raise<LoadSavedGameEvent>(new LoadSavedGameEvent(this, selectedGameCaption));
        }
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        // selectedGameCaption can be empty if there are no saved games
        if (selectedGameCaption == string.Empty) {
            D.Warn("Selected Game Caption to Load is empty.");
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

