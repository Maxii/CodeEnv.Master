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

    private static GuiMenuElementID[] _aiPlayerSpeciesPopupListIDs = new GuiMenuElementID[7] { 
                                                                                                GuiMenuElementID.AIPlayer1SpeciesPopupList, 
                                                                                                GuiMenuElementID.AIPlayer2SpeciesPopupList, 
                                                                                                GuiMenuElementID.AIPlayer3SpeciesPopupList, 
                                                                                                GuiMenuElementID.AIPlayer4SpeciesPopupList, 
                                                                                                GuiMenuElementID.AIPlayer5SpeciesPopupList, 
                                                                                                GuiMenuElementID.AIPlayer6SpeciesPopupList, 
                                                                                                GuiMenuElementID.AIPlayer7SpeciesPopupList
    };

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

    private Species _humanPlayerSpecies;
    private SpeciesGuiSelection _humanPlayerSpeciesSelection;

    private Species[] _aiPlayersSpecies = new Species[7];

    private GameColor _humanPlayerColor;
    private GameColor[] _aiPlayersColor = new GameColor[7];

    private IDictionary<GuiMenuElementID, GameObject> _aiPlayerFolderLookup = new Dictionary<GuiMenuElementID, GameObject>(7);

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        PopulateAIPlayerLookup();
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordPopupListState(GuiMenuElementID popupListID, string selectionName) {
        base.RecordPopupListState(popupListID, selectionName);
        D.Log("{0}.RecordPopupListState() called. ID = {1}, Selection = {2}.", GetType().Name, popupListID.GetName(), selectionName);
        switch (popupListID) {
            case GuiMenuElementID.UniverseSizePopupList:
                _universeSizeSelection = Enums<UniverseSizeGuiSelection>.Parse(selectionName);
                UniverseSize = _universeSizeSelection.Convert();
                break;

            case GuiMenuElementID.HumanPlayerSpeciesPopupList:
                _humanPlayerSpeciesSelection = Enums<SpeciesGuiSelection>.Parse(selectionName);
                _humanPlayerSpecies = _humanPlayerSpeciesSelection.Convert();
                break;
            case GuiMenuElementID.AIPlayer1SpeciesPopupList:
                _aiPlayersSpecies[0] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiMenuElementID.AIPlayer2SpeciesPopupList:
                _aiPlayersSpecies[1] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiMenuElementID.AIPlayer3SpeciesPopupList:
                _aiPlayersSpecies[2] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiMenuElementID.AIPlayer4SpeciesPopupList:
                _aiPlayersSpecies[3] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiMenuElementID.AIPlayer5SpeciesPopupList:
                _aiPlayersSpecies[4] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiMenuElementID.AIPlayer6SpeciesPopupList:
                _aiPlayersSpecies[5] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;
            case GuiMenuElementID.AIPlayer7SpeciesPopupList:
                _aiPlayersSpecies[6] = Enums<SpeciesGuiSelection>.Parse(selectionName).Convert();
                break;

            case GuiMenuElementID.HumanPlayerColorPopupList:
                _humanPlayerColor = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer1ColorPopupList:
                _aiPlayersColor[0] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer2ColorPopupList:
                _aiPlayersColor[1] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer3ColorPopupList:
                _aiPlayersColor[2] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer4ColorPopupList:
                _aiPlayersColor[3] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer5ColorPopupList:
                _aiPlayersColor[4] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer6ColorPopupList:
                _aiPlayersColor[5] = Enums<GameColor>.Parse(selectionName);
                break;
            case GuiMenuElementID.AIPlayer7ColorPopupList:
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
            var popupMenuElement = popup.gameObject.GetSafeMonoBehaviourComponent<AGuiMenuElement>();
            if (popupMenuElement.ElementID.EqualsAnyOf(_aiPlayerSpeciesPopupListIDs)) {
                var aiPlayerSpeciesElement = popupMenuElement;
                GameObject aiPlayerFolder = aiPlayerSpeciesElement.transform.parent.gameObject;
                _aiPlayerFolderLookup.Add(aiPlayerSpeciesElement.ElementID, aiPlayerFolder);
            }
        });
    }

    private void OnUniverseSizeChanged() {
        int aiPlayerCount = UniverseSize.DefaultAIPlayerCount();
        RefreshAvailableAIPlayerElements(aiPlayerCount);
    }

    private void RefreshAvailableAIPlayerElements(int aiPlayerCount) {
        _aiPlayerFolderLookup.Values.ForAll(go => go.SetActive(false));

        // now reactivate the AI player slots that will be in the game
        switch (aiPlayerCount) {
            case 7:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer7SpeciesPopupList].SetActive(true);
                goto case 6;
            case 6:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer6SpeciesPopupList].SetActive(true);
                goto case 5;
            case 5:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer5SpeciesPopupList].SetActive(true);
                goto case 4;
            case 4:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer4SpeciesPopupList].SetActive(true);
                goto case 3;
            case 3:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer3SpeciesPopupList].SetActive(true);
                goto case 2;
            case 2:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer2SpeciesPopupList].SetActive(true);
                goto case 1;
            case 1:
                _aiPlayerFolderLookup[GuiMenuElementID.AIPlayer1SpeciesPopupList].SetActive(true);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(aiPlayerCount));
        }
    }

    #endregion

    protected override void OnLeftClick() {
        RecordPreferences();

        int aiPlayerCount = UniverseSize.DefaultAIPlayerCount();
        var aiPlayerRaces = new Race[aiPlayerCount];
        for (int i = 0; i < aiPlayerCount; i++) {
            var aiPlayerRace = new Race(_aiPlayersSpecies[i], _aiPlayersColor[i]);
            aiPlayerRaces[i] = aiPlayerRace;
        }

        GameSettings settings = new GameSettings() {
            IsNewGame = true,
            UniverseSize = UniverseSize,
            HumanPlayerRace = new Race(new RaceStat(_humanPlayerSpecies, "Maxii", "Maxii description", _humanPlayerColor)),
            AIPlayerRaces = aiPlayerRaces
        };
        _gameMgr.InitiateNewGame(settings);
    }

    private void RecordPreferences() {
        _playerPrefsMgr.UniverseSizeSelection = _universeSizeSelection;
        _playerPrefsMgr.PlayerSpeciesSelection = _humanPlayerSpeciesSelection;
        _playerPrefsMgr.PlayerColor = _humanPlayerColor;
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(UniverseSize != UniverseSize.None, "UniverseSize has not been set!");
        D.Assert(_humanPlayerSpecies != Species.None, "HumanPlayer Species has not been set!");
        // TODO
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

