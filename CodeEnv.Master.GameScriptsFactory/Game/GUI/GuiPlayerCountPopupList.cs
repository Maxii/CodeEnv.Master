// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerCountPopupList.cs
// PopupList allowing selection of the number of Players when starting a new game.
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
/// PopupList allowing selection of the number of Players when starting a new game.
/// Also dynamically manages how many AI Players are displayed to the User.
/// Minimum is 2, the human player and 1 AI player.
/// </summary>
public class GuiPlayerCountPopupList : AGuiMenuPopupList<int> {

    private const string PrefPropertyNameFormat = "{0}PlayerCount";

    public override GuiElementID ElementID { get { return GuiElementID.PlayerCountPopupList; } }

    protected override bool SelfInitializeSelection { get { return false; } }

    private string[] _countChoices;
    protected override string[] Choices { get { return _countChoices; } }

    private IDictionary<GuiElementID, UIWidget> _aiPlayerContainerLookup;
    GuiUniverseSizePopupList _universeSizePopupList;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _universeSizePopupList = transform.parent.parent.gameObject.GetSingleComponentInChildren<GuiUniverseSizePopupList>(); // HACK
        PopulateAIPlayerContainerLookup();
    }

    protected override void Subscribe() {
        base.Subscribe();
        _universeSizePopupList.universeSizeChanged += UniverseSizeChangedEventHandler;
    }

    protected override void Start() {
        base.Start();
        InitializePlayerCountValues();
    }

    private void InitializePlayerCountValues() {
        RefreshCountChoices();
        AssignSelectionChoices();
        TryMakePreferenceSelection();
    }

    private void RefreshCountChoices() {
        //D.Log("{0}.RefreshCountChoices() called.", Name);
        UniverseSize currentUniverseSizeSelection = Enums<UniverseSize>.Parse(_universeSizePopupList.ConvertedSelectedValue);
        int maxAiPlayers = currentUniverseSizeSelection.MaxPlayerCount() - Constants.One;
        _countChoices = Enumerable.Range(start: 2, count: maxAiPlayers).Select(value => value.ToString()).ToArray();
    }

    #region Event and Property Change Handlers

    private void UniverseSizeChangedEventHandler(object sender, EventArgs e) {
        RefreshCountChoices();
        AssignSelectionChoices();
        TryMakePreferenceSelection();
    }

    protected override void PopupListSelectionChangedEventHandler() {
        base.PopupListSelectionChangedEventHandler();
        int aiPlayerCount = int.Parse(SelectedValue) - Constants.One;
        RefreshAIPlayerAvailability(aiPlayerCount);
    }

    #endregion

    protected override string DeterminePreferencePropertyName() {
        UniverseSize currentUniverseSizeSelection = Enums<UniverseSize>.Parse(_universeSizePopupList.ConvertedSelectedValue);
        return PrefPropertyNameFormat.Inject(currentUniverseSizeSelection.GetValueName());
    }

    #region Dynamic AI Player Show/Hide System

    private void PopulateAIPlayerContainerLookup() {
        _aiPlayerContainerLookup = new Dictionary<GuiElementID, UIWidget>(TempGameValues.MaxAIPlayers);
        var newGameMenuWindow = gameObject.GetSingleComponentInParents<AGuiWindow>();
        var aiIQPopups = newGameMenuWindow.gameObject.GetSafeComponentsInChildren<GuiPlayerIQPopupList>(includeInactive: true);
        aiIQPopups.ForAll(aiIQPopup => {
            var aiPlayerContainer = aiIQPopup.transform.parent.parent.GetSafeComponent<UIWidget>();  // HACK
            _aiPlayerContainerLookup.Add(aiIQPopup.ElementID, aiPlayerContainer);
        });
    }

    /// <summary>
    /// Refreshes the AI Players that are available to choose.
    /// </summary>
    /// <param name="aiPlayerCount">The AIPlayer count.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void RefreshAIPlayerAvailability(int aiPlayerCount) {
        //D.Log("{0}.RefreshAIPlayerAvailability() called. AIPlayer count = {1}.", ElementID.GetValueName(), aiPlayerCount);
        _aiPlayerContainerLookup.Values.ForAll(containerWidget => containerWidget.alpha = Constants.ZeroF);

        // now make the AI player slots that will be in the game visible again
        switch (aiPlayerCount) {
            case 7:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer7IQPopupList].alpha = Constants.OneF;
                goto case 6;
            case 6:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer6IQPopupList].alpha = Constants.OneF;
                goto case 5;
            case 5:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer5IQPopupList].alpha = Constants.OneF;
                goto case 4;
            case 4:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer4IQPopupList].alpha = Constants.OneF;
                goto case 3;
            case 3:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer3IQPopupList].alpha = Constants.OneF;
                goto case 2;
            case 2:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer2IQPopupList].alpha = Constants.OneF;
                goto case 1;
            case 1:
                _aiPlayerContainerLookup[GuiElementID.AIPlayer1IQPopupList].alpha = Constants.OneF;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(aiPlayerCount));
        }
    }

    #endregion

    protected override void Unsubscribe() {
        base.Unsubscribe();
        _universeSizePopupList.universeSizeChanged -= UniverseSizeChangedEventHandler;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

