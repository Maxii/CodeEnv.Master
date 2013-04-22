// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiNewGameMenuLaunchButton.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


// default namespace

using System.Diagnostics;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiNewGameMenuLaunchButton : GuiMenuAcceptButtonBase {

    private UniverseSize universeSize;
    private Players player;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        tooltip = "Launch New Game with these settings.";
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordCheckboxState(string checkboxName, bool checkedState) {
        // UNDONE
    }

    protected override void RecordPopupListState(string selectionName) {
        UniverseSize _universeSize;
        if (Enums<UniverseSize>.TryParse(selectionName, true, out _universeSize)) {
            //UnityEngine.Debug.Log("UniverseSize recorded as {0}.".Inject(selectionName));
            universeSize = _universeSize;
        }
        Players _player;
        if (Enums<Players>.TryParse(selectionName, true, out _player)) {
            //UnityEngine.Debug.Log("Player recorded as {0}.".Inject(selectionName));
            player = _player;
        }
        // more popupLists here
    }

    protected override void RecordSliderState(float sliderValue) {
        // UNDONE
    }

    protected override void OnCheckboxStateChange(bool state) {
        base.OnCheckboxStateChange(state);
    }

    protected override void OnPopupListSelectionChange(string item) {
        base.OnPopupListSelectionChange(item);
        ValidateState();
    }

    protected override void OnSliderValueChange(float value) {
        base.OnSliderValueChange(value);
    }

    protected override void OnButtonClick(GameObject sender) {
        GameSettings gameSettings = new GameSettings();
        gameSettings.IsNewGame = true;
        gameSettings.SizeOfUniverse = universeSize;
        gameSettings.Player = player;
        eventMgr.Raise<BuildNewGameEvent>(new BuildNewGameEvent(this, gameSettings));
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(universeSize != UniverseSize.None, "UniverseSize!");
        D.Assert(player != Players.None, "Player!");
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

