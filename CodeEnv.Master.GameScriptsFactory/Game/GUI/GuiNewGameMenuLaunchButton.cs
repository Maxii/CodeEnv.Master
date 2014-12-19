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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Diagnostics;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Script for the new game menu launch button.
/// </summary>
public class GuiNewGameMenuLaunchButton : AGuiMenuAcceptButtonBase {

    protected override string TooltipContent {
        get { return "Launch New Game with these settings."; }
    }

    private UniverseSize _universeSize;
    private Species _playerRace;
    //private GameColor _playerColor;

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        base.RecordPopupListState(popupListName, selectionName);
        UniverseSize universeSize;
        if (Enums<UniverseSize>.TryParse(selectionName, true, out universeSize)) {
            //D.Log("UniverseSize recorded as {0}.".Inject(selectionName));
            _universeSize = universeSize;
        }
        Species playerRace;
        if (Enums<Species>.TryParse(selectionName, true, out playerRace)) {
            //D.Log("Player recorded as {0}.".Inject(selectionName));
            _playerRace = playerRace;
        }
        GameColor playerColor;
        if (Enums<GameColor>.TryParse(selectionName, true, out playerColor)) {
            //_playerColor = playerColor;
        }
    }

    protected override void OnLeftClick() {
        GameSettings settings = new GameSettings() {
            IsNewGame = true,
            UniverseSize = _universeSize,
            PlayerRace = TempGameValues.HumanPlayersRace
        };
        _gameMgr.InitiateNewGame(settings);
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(_universeSize != UniverseSize.None, "UniverseSize!");
        D.Assert(_playerRace != Species.None, "PlayerRace!");
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

