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
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Launch button for the NewGameMenu.
/// </summary>
public class NewGameMenuLaunchButton : AGuiMenuAcceptButton {

    protected override string TooltipContent { get { return "Launch a New Game with these settings."; } }

    private UniverseSize _universeSize;
    private UniverseSizeGuiSelection _universeSizeSelection;
    private Species _userPlayerSpecies;
    private Species[] _aiPlayersSpecies;
    private SpeciesGuiSelection _userPlayerSpeciesSelection;
    private SpeciesGuiSelection[] _aiPlayersSpeciesSelections;
    private GameColor _userPlayerColor;
    private GameColor[] _aiPlayersColors;
    private IQ[] _aiPlayersIQs;
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
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selection) {
        base.RecordPopupListState(popupListID, selection);
        //D.Log("{0}.RecordPopupListState() called. ID = {1}, Selection = {2}.", GetType().Name, popupListID.GetName(), selectionName);
        switch (popupListID) {
            case GuiElementID.UniverseSizePopupList:
                _universeSizeSelection = Enums<UniverseSizeGuiSelection>.Parse(selection);
                _universeSize = _universeSizeSelection.Convert();
                break;
            case GuiElementID.PlayerCountPopupList:
                _playerCount = int.Parse(selection);
                break;

            case GuiElementID.UserPlayerSpeciesPopupList:
                _userPlayerSpeciesSelection = Enums<SpeciesGuiSelection>.Parse(selection);
                _userPlayerSpecies = _userPlayerSpeciesSelection.Convert();
                break;
            case GuiElementID.AIPlayer1SpeciesPopupList:
                _aiPlayersSpeciesSelections[0] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[0] = _aiPlayersSpeciesSelections[0].Convert();
                break;
            case GuiElementID.AIPlayer2SpeciesPopupList:
                _aiPlayersSpeciesSelections[1] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[1] = _aiPlayersSpeciesSelections[1].Convert();
                break;
            case GuiElementID.AIPlayer3SpeciesPopupList:
                _aiPlayersSpeciesSelections[2] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[2] = _aiPlayersSpeciesSelections[2].Convert();
                break;
            case GuiElementID.AIPlayer4SpeciesPopupList:
                _aiPlayersSpeciesSelections[3] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[3] = _aiPlayersSpeciesSelections[3].Convert();
                break;
            case GuiElementID.AIPlayer5SpeciesPopupList:
                _aiPlayersSpeciesSelections[4] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[4] = _aiPlayersSpeciesSelections[4].Convert();
                break;
            case GuiElementID.AIPlayer6SpeciesPopupList:
                _aiPlayersSpeciesSelections[5] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[5] = _aiPlayersSpeciesSelections[5].Convert();
                break;
            case GuiElementID.AIPlayer7SpeciesPopupList:
                _aiPlayersSpeciesSelections[6] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[6] = _aiPlayersSpeciesSelections[6].Convert();
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

            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    #region Event and Property Change Handlers

    protected override void HandleLeftClick() {
        base.HandleLeftClick();
        RecordPreferences();
        InitiateNewGame();
    }

    #endregion

    private void RecordPreferences() {
        var settings = new NewGamePreferenceSettings() {
            UniverseSizeSelection = _universeSizeSelection,
            PlayerCount = _playerCount,
            UserPlayerSpeciesSelection = _userPlayerSpeciesSelection,
            AIPlayerSpeciesSelections = _aiPlayersSpeciesSelections,
            UserPlayerColor = _userPlayerColor,
            AIPlayerColors = _aiPlayersColors,
            AIPlayerIQs = _aiPlayersIQs
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
            aiPlayers[i] = new Player(aiSpeciesStat, aiLeaderStat, _aiPlayersIQs[i], _aiPlayersColors[i]);
        }

        SpeciesStat userSpeciesStat = _speciesFactory.MakeInstance(_userPlayerSpecies);
        LeaderStat userLeaderStat = _leaderFactory.MakeInstance(_userPlayerSpecies);
        Player userPlayer = new Player(userSpeciesStat, userLeaderStat, IQ.None, _userPlayerColor, isUser: true);

        GameSettings settings = new GameSettings() {
            UniverseSize = _universeSize,
            PlayerCount = _playerCount,
            UserPlayer = userPlayer,
            AIPlayers = aiPlayers
        };
        _gameMgr.InitiateNewGame(settings);
    }

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        D.Assert(_universeSize != UniverseSize.None, "UniverseSize has not been set!");
        D.Assert(_playerCount > Constants.One, "Player count {0} is illegal!".Inject(_playerCount));
        D.Assert(_userPlayerSpecies != Species.None, "User Player Species has not been set!");
        //TODO
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

