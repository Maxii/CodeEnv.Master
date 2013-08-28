// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiNewGameMenuLaunchButton.cs
// Script for the new game menu launch button.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Diagnostics;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Script for the new game menu launch button.
/// </summary>
public class GuiNewGameMenuLaunchButton : AGuiMenuAcceptButtonBase {

    private UniverseSize _universeSize;
    private Races _playerRace;
    private GameColor _playerColor;

    protected override void Awake() {
        base.Awake();
        tooltip = "Launch New Game with these settings.";
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        base.RecordPopupListState(popupListName, selectionName);
        UniverseSize universeSize;
        if (Enums<UniverseSize>.TryParse(selectionName, true, out universeSize)) {
            //Logger.Log("UniverseSize recorded as {0}.".Inject(selectionName));
            _universeSize = universeSize;
        }
        Races playerRace;
        if (Enums<Races>.TryParse(selectionName, true, out playerRace)) {
            //Logger.Log("Player recorded as {0}.".Inject(selectionName));
            _playerRace = playerRace;
        }
        GameColor playerColor;
        if (Enums<GameColor>.TryParse(selectionName, true, out playerColor)) {
            _playerColor = playerColor;
        }
    }

    protected override void OnLeftClick() {
        GameSettings gameSettings = new GameSettings();
        gameSettings.IsNewGame = true;
        gameSettings.UniverseSize = _universeSize;
        gameSettings.PlayerRace = new Race(new RaceStat(_playerRace, "Maxii", new StringBuilder("Maxii description"), _playerColor));
        _eventMgr.Raise<BuildNewGameEvent>(new BuildNewGameEvent(this, gameSettings));
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(_universeSize != UniverseSize.None, "UniverseSize!");
        D.Assert(_playerRace != Races.None, "PlayerRace!");
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

