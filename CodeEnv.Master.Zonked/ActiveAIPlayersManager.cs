// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveAIPlayersManager.cs
// Manages the AIPlayer show/hide system for the NewGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages the AIPlayer show/hide system for the NewGameMenu. 
/// </summary>
[Obsolete]
public class ActiveAIPlayersManager : AMonoBase {

    private IDictionary<GuiElementID, GameObject> _aiPlayerFolderLookup;
    private UIPopupList _universeSizePopupList;

    protected override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        var uSizePopup = gameObject.GetSafeFirstMonoBehaviourInChildren<GuiUniverseSizePopupList>();
        _universeSizePopupList = uSizePopup.gameObject.GetSafeMonoBehaviour<UIPopupList>();

        PopulateAIPlayerFolderLookup();

        EventDelegate.Add(_universeSizePopupList.onChange, OnUniverseSizeSelectionChanged);
    }

    private void PopulateAIPlayerFolderLookup() {
        _aiPlayerFolderLookup = new Dictionary<GuiElementID, GameObject>(TempGameValues.MaxAIPlayers);
        var colorPopups = gameObject.GetSafeMonoBehavioursInChildren<GuiPlayerColorPopupList>(includeInactive: true);
        var aiColorPopups = colorPopups.Where(cPop => cPop.ElementID != GuiElementID.UserPlayerColorPopupList);
        aiColorPopups.ForAll(aiColorPopup => {
            var aiPlayerFolder = aiColorPopup.transform.parent.gameObject;
            _aiPlayerFolderLookup.Add(aiColorPopup.ElementID, aiPlayerFolder);
        });
    }

    private void OnUniverseSizeSelectionChanged() {
        var universeSize = Enums<UniverseSizeGuiSelection>.Parse(_universeSizePopupList.value).Convert();
        int aiPlayerCount = universeSize.DefaultPlayerCount();
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

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        EventDelegate.Remove(_universeSizePopupList.onChange, OnUniverseSizeSelectionChanged);
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

