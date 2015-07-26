// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiNewGameMenuLaunchButton.cs
// The new game menu launch button. 
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
/// The new game menu launch button. 
/// It also dynamically manages how many AI Players are displayed to the Player
/// based on the UniverseSize currently selected.
/// </summary>
[Obsolete]
public class GuiNewGameMenuLaunchButton : AGuiMenuAcceptButton {

    protected override string TooltipContent { get { return "Launch a New Game with these settings."; } }

    private UniverseSize _universeSize;
    /// <summary>
    /// The size of the Universe currently selected in the new game menu.
    /// Notes: This value can change multiple times while the menu is up before the Menu
    /// Accept button is clicked. It is used to enable dynamic adjustment of the 
    /// number of AI Players being displayed in the menu. 
    /// It is constructed as a private property to make use of OnUniverseSizeChanged.
    /// The actual UniverseSize for the new game started by clicking the Accept button is held in GameSettings.
    /// WARNING: Cannot use UIPopupList.onChange as it provides UniverseSizeGuiSelection which contains
    /// Random. Calling Random.Convert to get an actual UniverseSize generates a new random UniverseSize value
    /// everytime it is called.
    /// </summary>
    private UniverseSize UniverseSize {
        get { return _universeSize; }
        set { SetProperty<UniverseSize>(ref _universeSize, value, "UniverseSize", OnUniverseSizeChanged); }
    }

    private UniverseSizeGuiSelection _universeSizeSelection;

    private GameColor _userPlayerColor;
    private Species _userPlayerSpecies;
    private SpeciesGuiSelection _userPlayerSpeciesSelection;

    private GuiElementID[] _aiPlayerSpeciesPopupListIDs;
    private Species[] _aiPlayersSpecies;
    private SpeciesGuiSelection[] _aiPlayerSpeciesSelections;
    private GameColor[] _aiPlayerColors;

    private IDictionary<GuiElementID, GameObject> _aiPlayerFolderLookup = new Dictionary<GuiElementID, GameObject>(7);

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _aiPlayerSpeciesPopupListIDs = new GuiElementID[TempGameValues.MaxAIPlayers] { 
                                                                            GuiElementID.AIPlayer1SpeciesPopupList, 
                                                                            GuiElementID.AIPlayer2SpeciesPopupList, 
                                                                            GuiElementID.AIPlayer3SpeciesPopupList, 
                                                                            GuiElementID.AIPlayer4SpeciesPopupList, 
                                                                            GuiElementID.AIPlayer5SpeciesPopupList, 
                                                                            GuiElementID.AIPlayer6SpeciesPopupList, 
                                                                            GuiElementID.AIPlayer7SpeciesPopupList
        };
        _aiPlayersSpecies = new Species[TempGameValues.MaxAIPlayers];
        _aiPlayerSpeciesSelections = new SpeciesGuiSelection[TempGameValues.MaxAIPlayers];
        _aiPlayerColors = new GameColor[TempGameValues.MaxAIPlayers];
        PopulateAIPlayerLookup();
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selection) {
        base.RecordPopupListState(popupListID, selection);
        //D.Log("{0}.RecordPopupListState() called. ID = {1}, Selection = {2}.", GetType().Name, popupListID.GetName(), selectionName);
        switch (popupListID) {
            case GuiElementID.UniverseSizePopupList:
                _universeSizeSelection = Enums<UniverseSizeGuiSelection>.Parse(selection);
                UniverseSize = _universeSizeSelection.Convert();
                break;

            case GuiElementID.UserPlayerSpeciesPopupList:
                _userPlayerSpeciesSelection = Enums<SpeciesGuiSelection>.Parse(selection);
                _userPlayerSpecies = _userPlayerSpeciesSelection.Convert();
                break;
            case GuiElementID.AIPlayer1SpeciesPopupList:
                _aiPlayerSpeciesSelections[0] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[0] = _aiPlayerSpeciesSelections[0].Convert();
                break;
            case GuiElementID.AIPlayer2SpeciesPopupList:
                _aiPlayerSpeciesSelections[1] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[1] = _aiPlayerSpeciesSelections[1].Convert();
                break;
            case GuiElementID.AIPlayer3SpeciesPopupList:
                _aiPlayerSpeciesSelections[2] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[2] = _aiPlayerSpeciesSelections[2].Convert();
                break;
            case GuiElementID.AIPlayer4SpeciesPopupList:
                _aiPlayerSpeciesSelections[3] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[3] = _aiPlayerSpeciesSelections[3].Convert();
                break;
            case GuiElementID.AIPlayer5SpeciesPopupList:
                _aiPlayerSpeciesSelections[4] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[4] = _aiPlayerSpeciesSelections[4].Convert();
                break;
            case GuiElementID.AIPlayer6SpeciesPopupList:
                _aiPlayerSpeciesSelections[5] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[5] = _aiPlayerSpeciesSelections[5].Convert();
                break;
            case GuiElementID.AIPlayer7SpeciesPopupList:
                _aiPlayerSpeciesSelections[6] = Enums<SpeciesGuiSelection>.Parse(selection);
                _aiPlayersSpecies[6] = _aiPlayerSpeciesSelections[6].Convert();
                break;

            case GuiElementID.UserPlayerColorPopupList:
                _userPlayerColor = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer1ColorPopupList:
                _aiPlayerColors[0] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer2ColorPopupList:
                _aiPlayerColors[1] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer3ColorPopupList:
                _aiPlayerColors[2] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer4ColorPopupList:
                _aiPlayerColors[3] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer5ColorPopupList:
                _aiPlayerColors[4] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer6ColorPopupList:
                _aiPlayerColors[5] = Enums<GameColor>.Parse(selection);
                break;
            case GuiElementID.AIPlayer7ColorPopupList:
                _aiPlayerColors[6] = Enums<GameColor>.Parse(selection);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    #region Dynamic AI Player Show/Hide System

    private void PopulateAIPlayerLookup() {
        // populate the AI Player folder lookup table using the Species Key
        _popupLists.ForAll(popup => {
            var popupMenuElement = popup.gameObject.GetSafeMonoBehaviour<AGuiMenuElement>();
            if (popupMenuElement.ElementID.EqualsAnyOf(_aiPlayerSpeciesPopupListIDs)) {
                var aiPlayerSpeciesElement = popupMenuElement;
                GameObject aiPlayerFolder = aiPlayerSpeciesElement.transform.parent.gameObject;
                _aiPlayerFolderLookup.Add(aiPlayerSpeciesElement.ElementID, aiPlayerFolder);
            }
        });
    }

    private void OnUniverseSizeChanged() {
        int aiPlayerCount = UniverseSize.DefaultPlayerCount();
        RefreshAIPlayerAvailability(aiPlayerCount);
    }

    /// <summary>
    /// Refreshes the AI Players that are available to choose.
    /// </summary>
    /// <param name="aiPlayerCount">The AIPlayer count.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void RefreshAIPlayerAvailability(int aiPlayerCount) {
        _aiPlayerFolderLookup.Values.ForAll(go => go.SetActive(false));

        // now reactivate the AI player slots that will be in the game
        switch (aiPlayerCount) {
            case 7:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer7SpeciesPopupList].SetActive(true);
                goto case 6;
            case 6:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer6SpeciesPopupList].SetActive(true);
                goto case 5;
            case 5:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer5SpeciesPopupList].SetActive(true);
                goto case 4;
            case 4:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer4SpeciesPopupList].SetActive(true);
                goto case 3;
            case 3:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer3SpeciesPopupList].SetActive(true);
                goto case 2;
            case 2:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer2SpeciesPopupList].SetActive(true);
                goto case 1;
            case 1:
                _aiPlayerFolderLookup[GuiElementID.AIPlayer1SpeciesPopupList].SetActive(true);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(aiPlayerCount));
        }
    }

    #endregion

    protected override void OnLeftClick() {
        RecordPreferences();
        InitiateNewGame();
    }

    private void InitiateNewGame() {
        int aiPlayerCount = UniverseSize.DefaultPlayerCount();
        var aiPlayerRaces = new Race[aiPlayerCount];
        for (int i = 0; i < aiPlayerCount; i++) {
            aiPlayerRaces[i] = new Race(_aiPlayersSpecies[i], _aiPlayerColors[i]);
        }
        var userRaceStat = new RaceStat(_userPlayerSpecies, "Maxii", TempGameValues.AnImageFilename, "Maxii description", _userPlayerColor);

        GameSettings settings = new GameSettings() {
            UniverseSize = UniverseSize,
            UserPlayerRace = new Race(userRaceStat),
            AIPlayerRaces = aiPlayerRaces,
        };
        _gameMgr.InitiateNewGame(settings);
    }

    private void RecordPreferences() {
        var settings = new NewGamePreferenceSettings() {
            UniverseSizeSelection = _universeSizeSelection,
            UserPlayerSpeciesSelection = _userPlayerSpeciesSelection,
            UserPlayerColor = _userPlayerColor,
            AIPlayerSpeciesSelections = _aiPlayerSpeciesSelections,
            AIPlayerColors = _aiPlayerColors

        };
        _playerPrefsMgr.RecordNewGameSettings(settings);
    }

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        D.Assert(UniverseSize != UniverseSize.None, "UniverseSize has not been set!");
        D.Assert(_userPlayerSpecies != Species.None, "User Player Species has not been set!");
        // TODO
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

