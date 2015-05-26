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
using System.Diagnostics;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The new game menu launch button. 
/// It also dynamically manages how many AI Players are displayed to the Player
/// based on the UniverseSize currently selected.
/// </summary>
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

    private GameColor _userPlayerColor;
    private GameColor UserPlayerColor {
        get { return _userPlayerColor; }
        set { SetProperty<GameColor>(ref _userPlayerColor, value, "UserPlayerColor", OnUserPlayerColorChanged); }
    }

    private UniverseSizeGuiSelection _universeSizeSelection;

    private Species _userPlayerSpecies;
    private SpeciesGuiSelection _userPlayerSpeciesSelection;

    private GuiElementID[] _aiPlayerSpeciesPopupListIDs;
    private Species[] _aiPlayersSpecies;
    private GameColor[] _aiPlayersColor;

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
        _aiPlayersColor = new GameColor[TempGameValues.MaxAIPlayers];
        PopulateAIPlayerLookup();
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selectionName) {
        base.RecordPopupListState(popupListID, selectionName);
        //D.Log("{0}.RecordPopupListState() called. ID = {1}, Selection = {2}.", GetType().Name, popupListID.GetName(), selectionName);
        switch (popupListID) {
            case GuiElementID.UniverseSizePopupList:
                _universeSizeSelection = Enums<UniverseSizeGuiSelection>.Parse(selectionName);
                UniverseSize = _universeSizeSelection.Convert();
                break;

            case GuiElementID.UserPlayerSpeciesPopupList:
                _userPlayerSpeciesSelection = Enums<SpeciesGuiSelection>.Parse(selectionName);
                _userPlayerSpecies = _userPlayerSpeciesSelection.Convert();
                break;
            case GuiElementID.AIPlayer1SpeciesPopupList:
                _aiPlayersSpecies[0] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiElementID.AIPlayer2SpeciesPopupList:
                _aiPlayersSpecies[1] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiElementID.AIPlayer3SpeciesPopupList:
                _aiPlayersSpecies[2] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiElementID.AIPlayer4SpeciesPopupList:
                _aiPlayersSpecies[3] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiElementID.AIPlayer5SpeciesPopupList:
                _aiPlayersSpecies[4] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiElementID.AIPlayer6SpeciesPopupList:
                _aiPlayersSpecies[5] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiElementID.AIPlayer7SpeciesPopupList:
                _aiPlayersSpecies[6] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;

            case GuiElementID.UserPlayerColorPopupList:
                UserPlayerColor = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer1ColorPopupList:
                _aiPlayersColor[0] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer2ColorPopupList:
                _aiPlayersColor[1] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer3ColorPopupList:
                _aiPlayersColor[2] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer4ColorPopupList:
                _aiPlayersColor[3] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer5ColorPopupList:
                _aiPlayersColor[4] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer6ColorPopupList:
                _aiPlayersColor[5] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiElementID.AIPlayer7ColorPopupList:
                _aiPlayersColor[6] = Enums<GameColor>.Parse(selectionName);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    #region Dynamic AI Player Display System

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

    private void OnUserPlayerColorChanged() {
        var colorToRemove = UserPlayerColor;
        RefreshAIPlayersAvailableColors(colorToRemove);
    }

    private void RefreshAIPlayersAvailableColors(GameColor colorToRemove) {
        _aiPlayerFolderLookup.Values.ForAll(go => {
            go.SetActive(true);  // activate folder so can get at popup lists
            var colorPopupList = go.GetSafeMonoBehaviourInChildren<GuiPlayerColorPopupList>();
            colorPopupList.RemoveColor(colorToRemove);
        });
        if (UniverseSize != default(UniverseSize)) {    // if UniverseSize.None, no worries. RefreshAIPlayerAvailability() will get called when UniverseSize initialized
            RefreshAIPlayerAvailability(UniverseSize.DefaultAIPlayerCount());  // re-establish the available AI players
        }
    }

    private void OnUniverseSizeChanged() {
        int aiPlayerCount = UniverseSize.DefaultAIPlayerCount();
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
        var settings = CreateNewGameSettings();
        RecordPreferences(settings);
        InitiateNewGame(settings);
    }

    private GameSettings CreateNewGameSettings() {
        int aiPlayerCount = UniverseSize.DefaultAIPlayerCount();
        var aiPlayerRaces = new Race[aiPlayerCount];
        for (int i = 0; i < aiPlayerCount; i++) {
            var aiPlayerRace = new Race(_aiPlayersSpecies[i], _aiPlayersColor[i]);
            aiPlayerRaces[i] = aiPlayerRace;
        }

        var userRaceStat = new RaceStat(_userPlayerSpecies, "Maxii", TempGameValues.AnImageFilename, "Maxii description", UserPlayerColor);

        GameSettings settings = new GameSettings() {
            UniverseSize = UniverseSize,
            UserPlayerRace = new Race(userRaceStat),
            AIPlayerRaces = aiPlayerRaces,

            // these are used by PlayerPrefs to record preferences
            UniverseSizeSelection = _universeSizeSelection,
            UserPlayerSpeciesSelection = _userPlayerSpeciesSelection,
            UserPlayerColor = UserPlayerColor
        };
        return settings;
    }

    private void RecordPreferences(GameSettings settings) {
        _playerPrefsMgr.RecordNewGameSettings(settings);
    }

    private void InitiateNewGame(GameSettings settings) {
        _gameMgr.InitiateNewGame(settings);
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(UniverseSize != UniverseSize.None, "UniverseSize has not been set!");
        D.Assert(_userPlayerSpecies != Species.None, "User Player Species has not been set!");
        // TODO
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

