// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameMenuLaunchButton.cs
//  Launch button for the NewGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Launch button for the NewGameMenu.
/// </summary>
public class NewGameMenuLaunchButton : AGuiMenuAcceptButton {

    protected override IList<KeyCode> ValidKeys { get { return new List<KeyCode>() { KeyCode.Return }; } }

    protected override string TooltipContent { get { return "Launch a New Game with these settings."; } }

    private UniverseSize _universeSize;
    private UniverseSizeGuiSelection _universeSizeSelection;

    private SystemDensity _systemDensity;
    private SystemDensityGuiSelection _systemDensitySelection;

    private Species _userPlayerSpecies;
    private Species[] _aiPlayersSpecies;
    private SpeciesGuiSelection _userPlayerSpeciesSelection;
    private SpeciesGuiSelection[] _aiPlayersSpeciesSelections;
    private GameColor _userPlayerColor;
    private GameColor[] _aiPlayersColors;
    private IQ[] _aiPlayersIQs;
    private TeamID _userPlayerTeam;
    private TeamID[] _aiPlayersTeams;
    private int _playerCount;

    private SpeciesFactory _speciesFactory;
    private LeaderFactory _leaderFactory;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _speciesFactory = SpeciesFactory.Instance;
        _leaderFactory = LeaderFactory.Instance;
        _aiPlayersSpecies = new Species[TempGameValues.MaxAIPlayers];
        _aiPlayersSpeciesSelections = new SpeciesGuiSelection[TempGameValues.MaxAIPlayers];
        _aiPlayersColors = new GameColor[TempGameValues.MaxAIPlayers];
        _aiPlayersIQs = new IQ[TempGameValues.MaxAIPlayers];
        _aiPlayersTeams = new TeamID[TempGameValues.MaxAIPlayers];
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selection, string convertedSelection) {
        base.RecordPopupListState(popupListID, selection, convertedSelection);
        //D.Log("{0}.RecordPopupListState() called. ID = {1}, Selection = {2}.", GetType().Name, popupListID.GetValueName(), selectionName);
        switch (popupListID) {
            case GuiElementID.UniverseSizePopupList:
                _universeSizeSelection = Enums<UniverseSizeGuiSelection>.Parse(selection);
                _universeSize = Enums<UniverseSize>.Parse(convertedSelection);
                break;
            case GuiElementID.SystemDensityPopupList:
                _systemDensitySelection = Enums<SystemDensityGuiSelection>.Parse(selection);
                _systemDensity = Enums<SystemDensity>.Parse(convertedSelection);
                break;
            case GuiElementID.PlayerCountPopupList:
                _playerCount = int.Parse(selection);
                break;

            case GuiElementID.UserPlayerSpeciesPopupList:
                _userPlayerSpeciesSelection = Enums<SpeciesGuiSelection>.Parse(selection);
                _userPlayerSpecies = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer1SpeciesPopupList:
                _aiPlayersSpeciesSelections[0] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[0] = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer2SpeciesPopupList:
                _aiPlayersSpeciesSelections[1] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[1] = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer3SpeciesPopupList:
                _aiPlayersSpeciesSelections[2] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[2] = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer4SpeciesPopupList:
                _aiPlayersSpeciesSelections[3] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[3] = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer5SpeciesPopupList:
                _aiPlayersSpeciesSelections[4] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[4] = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer6SpeciesPopupList:
                _aiPlayersSpeciesSelections[5] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[5] = Enums<Species>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer7SpeciesPopupList:
                _aiPlayersSpeciesSelections[6] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[6] = Enums<Species>.Parse(convertedSelection);
                break;

            case GuiElementID.UserPlayerColorPopupList:
                _userPlayerColor = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer1ColorPopupList:
                _aiPlayersColors[0] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer2ColorPopupList:
                _aiPlayersColors[1] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer3ColorPopupList:
                _aiPlayersColors[2] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer4ColorPopupList:
                _aiPlayersColors[3] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer5ColorPopupList:
                _aiPlayersColors[4] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer6ColorPopupList:
                _aiPlayersColors[5] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer7ColorPopupList:
                _aiPlayersColors[6] = Enums<GameColor>.Parse(selection);
                break;

            case GuiElementID.AIPlayer1IQPopupList:
                _aiPlayersIQs[0] = Enums<IQ>.Parse(selection);
                break;
            case GuiElementID.AIPlayer2IQPopupList:
                _aiPlayersIQs[1] = Enums<IQ>.Parse(selection);
                break;
            case GuiElementID.AIPlayer3IQPopupList:
                _aiPlayersIQs[2] = Enums<IQ>.Parse(selection);
                break;
            case GuiElementID.AIPlayer4IQPopupList:
                _aiPlayersIQs[3] = Enums<IQ>.Parse(selection);
                break;
            case GuiElementID.AIPlayer5IQPopupList:
                _aiPlayersIQs[4] = Enums<IQ>.Parse(selection);
                break;
            case GuiElementID.AIPlayer6IQPopupList:
                _aiPlayersIQs[5] = Enums<IQ>.Parse(selection);
                break;
            case GuiElementID.AIPlayer7IQPopupList:
                _aiPlayersIQs[6] = Enums<IQ>.Parse(selection);
                break;

            case GuiElementID.UserPlayerTeamPopupList:
                _userPlayerTeam = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer1TeamPopupList:
                _aiPlayersTeams[0] = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer2TeamPopupList:
                _aiPlayersTeams[1] = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer3TeamPopupList:
                _aiPlayersTeams[2] = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer4TeamPopupList:
                _aiPlayersTeams[3] = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer5TeamPopupList:
                _aiPlayersTeams[4] = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer6TeamPopupList:
                _aiPlayersTeams[5] = Enums<TeamID>.Parse(selection);
                break;
            case GuiElementID.AIPlayer7TeamPopupList:
                _aiPlayersTeams[6] = Enums<TeamID>.Parse(selection);
                break;

            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        base.HandleValidClick();
        RecordPreferences();
        InitiateNewGame();
    }

    #endregion

    private void RecordPreferences() {
        var settings = new NewGamePreferenceSettings() {
            UniverseSizeGuiSelection = _universeSizeSelection,

            UniverseSize = _universeSize,   // 10.4.16 needed to know which pref property gets assigned PlayerCount
            PlayerCount = _playerCount,

            SystemDensitySelection = _systemDensitySelection,
            UserPlayerSpeciesSelection = _userPlayerSpeciesSelection,
            AIPlayerSpeciesSelections = _aiPlayersSpeciesSelections,
            UserPlayerColor = _userPlayerColor,
            AIPlayerColors = _aiPlayersColors,
            AIPlayerIQs = _aiPlayersIQs,
            UserPlayerTeam = _userPlayerTeam,
            AIPlayersTeams = _aiPlayersTeams
        };
        _playerPrefsMgr.RecordNewGameSettings(settings);
    }

    private void InitiateNewGame() {
        int aiPlayerCount = _playerCount - Constants.One;
        var aiPlayers = new Player[aiPlayerCount];
        var aiPlayersStartLevel = new EmpireStartLevel[aiPlayerCount];
        var aiPlayersHomeSystemDesirability = new SystemDesirability[aiPlayerCount];
        var aiPlayersSeparationFromUser = new PlayerSeparation[aiPlayerCount];

        for (int i = 0; i < aiPlayerCount; i++) {
            Species aiSpecies = _aiPlayersSpecies[i];
            SpeciesStat aiSpeciesStat = _speciesFactory.MakeInstance(aiSpecies);
            LeaderStat aiLeaderStat = _leaderFactory.MakeInstance(aiSpecies);
            aiPlayers[i] = new Player(aiSpeciesStat, aiLeaderStat, _aiPlayersIQs[i], _aiPlayersTeams[i], _aiPlayersColors[i]);
            aiPlayersStartLevel[i] = EmpireStartLevel.Normal;
            aiPlayersHomeSystemDesirability[i] = SystemDesirability.Normal;
            aiPlayersSeparationFromUser[i] = PlayerSeparation.Normal;
        }

        SpeciesStat userSpeciesStat = _speciesFactory.MakeInstance(_userPlayerSpecies);
        LeaderStat userLeaderStat = _leaderFactory.MakeInstance(_userPlayerSpecies);
        Player userPlayer = new Player(userSpeciesStat, userLeaderStat, IQ.None, _userPlayerTeam, _userPlayerColor, isUser: true);
        EmpireStartLevel userPlayerStartLevel = EmpireStartLevel.Normal;
        SystemDesirability userPlayerHomeSystemDesirability = SystemDesirability.Normal;

        GameSettings settings = new GameSettings() {
            __IsStartup = false,
            __UseDebugCreatorsOnly = false,
            __DeployAdditionalAICreators = false,
            __ZoomOnUser = true,
            UniverseSize = _universeSize,
            SystemDensity = _systemDensity,
            PlayerCount = _playerCount,
            UserPlayer = userPlayer,
            AIPlayers = aiPlayers,
            UserStartLevel = userPlayerStartLevel,
            AIPlayersStartLevel = aiPlayersStartLevel,
            UserHomeSystemDesirability = userPlayerHomeSystemDesirability,
            AIPlayersHomeSystemDesirability = aiPlayersHomeSystemDesirability,
            AIPlayersSeparationFromUser = aiPlayersSeparationFromUser
        };
        _gameMgr.InitiateNewGame(settings);
    }

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        D.Assert(_universeSize != UniverseSize.None, "UniverseSize has not been set!");
        D.Assert(_systemDensity != SystemDensity.None, "SystemDensity has not been set!");
        D.Assert(_playerCount > Constants.One, "Player count {0} is illegal!".Inject(_playerCount));
        D.Assert(_userPlayerSpecies != Species.None, "User Player Species has not been set!");
        //TODO
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

