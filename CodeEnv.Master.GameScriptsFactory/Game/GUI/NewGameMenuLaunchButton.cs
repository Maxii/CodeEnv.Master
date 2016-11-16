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

    private EmpireStartLevel _userPlayerStartLevel;
    private EmpireStartLevel[] _aiPlayersStartLevels;
    private EmpireStartLevelGuiSelection _userPlayerStartLevelSelection;
    private EmpireStartLevelGuiSelection[] _aiPlayersStartLevelSelections;

    private SystemDesirability _userPlayerHomeSystemDesirability;
    private SystemDesirability[] _aiPlayersHomeSystemDesirability;
    private SystemDesirabilityGuiSelection _userPlayerHomeSystemDesirabilitySelection;
    private SystemDesirabilityGuiSelection[] _aiPlayersHomeSystemDesirabilitySelections;

    private PlayerSeparation[] _aiPlayersUserSeparations;
    private PlayerSeparationGuiSelection[] _aiPlayersUserSeparationSelections;

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
        int maxAiPlayerQty = TempGameValues.MaxAIPlayers;
        _aiPlayersSpecies = new Species[maxAiPlayerQty];
        _aiPlayersSpeciesSelections = new SpeciesGuiSelection[maxAiPlayerQty];
        _aiPlayersColors = new GameColor[maxAiPlayerQty];
        _aiPlayersIQs = new IQ[maxAiPlayerQty];
        _aiPlayersTeams = new TeamID[maxAiPlayerQty];
        _aiPlayersStartLevels = new EmpireStartLevel[maxAiPlayerQty];
        _aiPlayersStartLevelSelections = new EmpireStartLevelGuiSelection[maxAiPlayerQty];
        _aiPlayersHomeSystemDesirability = new SystemDesirability[maxAiPlayerQty];
        _aiPlayersHomeSystemDesirabilitySelections = new SystemDesirabilityGuiSelection[maxAiPlayerQty];
        _aiPlayersUserSeparations = new PlayerSeparation[maxAiPlayerQty];
        _aiPlayersUserSeparationSelections = new PlayerSeparationGuiSelection[maxAiPlayerQty];
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

            case GuiElementID.UserPlayerStartLevelPopupList:
                _userPlayerStartLevelSelection = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _userPlayerStartLevel = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer1StartLevelPopupList:
                _aiPlayersStartLevelSelections[0] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[0] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer2StartLevelPopupList:
                _aiPlayersStartLevelSelections[1] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[1] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer3StartLevelPopupList:
                _aiPlayersStartLevelSelections[2] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[2] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer4StartLevelPopupList:
                _aiPlayersStartLevelSelections[3] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[3] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer5StartLevelPopupList:
                _aiPlayersStartLevelSelections[4] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[4] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer6StartLevelPopupList:
                _aiPlayersStartLevelSelections[5] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[5] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer7StartLevelPopupList:
                _aiPlayersStartLevelSelections[6] = Enums<EmpireStartLevelGuiSelection>.Parse(selection);
                _aiPlayersStartLevels[6] = Enums<EmpireStartLevel>.Parse(convertedSelection);
                break;

            case GuiElementID.UserPlayerHomeDesirabilityPopupList:
                _userPlayerHomeSystemDesirabilitySelection = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _userPlayerHomeSystemDesirability = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer1HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[0] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[0] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer2HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[1] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[1] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer3HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[2] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[2] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer4HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[3] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[3] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer5HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[4] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[4] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer6HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[5] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[5] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer7HomeDesirabilityPopupList:
                _aiPlayersHomeSystemDesirabilitySelections[6] = Enums<SystemDesirabilityGuiSelection>.Parse(selection);
                _aiPlayersHomeSystemDesirability[6] = Enums<SystemDesirability>.Parse(convertedSelection);
                break;

            case GuiElementID.AIPlayer1UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[0] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[0] = Enums<PlayerSeparation>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer2UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[1] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[1] = Enums<PlayerSeparation>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer3UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[2] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[2] = Enums<PlayerSeparation>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer4UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[3] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[3] = Enums<PlayerSeparation>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer5UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[4] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[4] = Enums<PlayerSeparation>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer6UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[5] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[5] = Enums<PlayerSeparation>.Parse(convertedSelection);
                break;
            case GuiElementID.AIPlayer7UserSeparationPopupList:
                _aiPlayersUserSeparationSelections[6] = Enums<PlayerSeparationGuiSelection>.Parse(selection);
                _aiPlayersUserSeparations[6] = Enums<PlayerSeparation>.Parse(convertedSelection);
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
            UniverseSizeSelection = _universeSizeSelection,

            UniverseSize = _universeSize,   // 10.4.16 needed to know which pref property gets assigned PlayerCount
            PlayerCount = _playerCount,

            SystemDensitySelection = _systemDensitySelection,
            UserPlayerSpeciesSelection = _userPlayerSpeciesSelection,
            AIPlayerSpeciesSelections = _aiPlayersSpeciesSelections,
            UserPlayerColor = _userPlayerColor,
            AIPlayerColors = _aiPlayersColors,
            AIPlayerIQs = _aiPlayersIQs,
            UserPlayerTeam = _userPlayerTeam,
            AIPlayersTeams = _aiPlayersTeams,

            UserPlayerStartLevelSelection = _userPlayerStartLevelSelection,
            AIPlayersStartLevelSelections = _aiPlayersStartLevelSelections,
            UserPlayerHomeDesirabilitySelection = _userPlayerHomeSystemDesirabilitySelection,
            AIPlayersHomeDesirabilitySelections = _aiPlayersHomeSystemDesirabilitySelections,

            AIPlayersUserSeparationSelections = _aiPlayersUserSeparationSelections
        };
        _playerPrefsMgr.RecordNewGameSettings(settings);
    }

    private void InitiateNewGame() {
        int aiPlayerCount = _playerCount - Constants.One;
        var aiPlayers = new Player[aiPlayerCount];

        for (int i = 0; i < aiPlayerCount; i++) {
            Species aiSpecies = _aiPlayersSpecies[i];
            SpeciesStat aiSpeciesStat = _speciesFactory.MakeInstance(aiSpecies);
            LeaderStat aiLeaderStat = _leaderFactory.MakeInstance(aiSpecies);
            aiPlayers[i] = new Player(aiSpeciesStat, aiLeaderStat, _aiPlayersIQs[i], _aiPlayersTeams[i], _aiPlayersColors[i]);
        }

        SpeciesStat userSpeciesStat = _speciesFactory.MakeInstance(_userPlayerSpecies);
        LeaderStat userLeaderStat = _leaderFactory.MakeInstance(_userPlayerSpecies);
        Player userPlayer = new Player(userSpeciesStat, userLeaderStat, IQ.None, _userPlayerTeam, _userPlayerColor, isUser: true);

        GameSettings settings = new GameSettings() {
            __IsStartup = false,
            __UseDebugCreatorsOnly = false,
            __DeployAdditionalAICreators = false,
            __AdditionalFleetCreatorQty = 0,
            __AdditionalStarbaseCreatorQty = 0,
            __AdditionalSettlementCreatorQty = 0,
            __ZoomOnUser = true,
            UniverseSize = _universeSize,
            SystemDensity = _systemDensity,
            PlayerCount = _playerCount,
            UserPlayer = userPlayer,
            AIPlayers = aiPlayers,
            UserStartLevel = _userPlayerStartLevel,
            AIPlayersStartLevels = _aiPlayersStartLevels,
            UserHomeSystemDesirability = _userPlayerHomeSystemDesirability,
            AIPlayersHomeSystemDesirability = _aiPlayersHomeSystemDesirability,
            AIPlayersUserSeparations = _aiPlayersUserSeparations
        };
        _gameMgr.InitiateNewGame(settings);
    }

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        D.AssertNotDefault((int)_universeSize, "UniverseSize has not been set!");
        D.AssertNotDefault((int)_systemDensity, "SystemDensity has not been set!");
        D.Assert(_playerCount > Constants.One, _playerCount.ToString());
        D.AssertNotDefault((int)_userPlayerSpecies, "User Player Species has not been set!");
        //TODO
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

