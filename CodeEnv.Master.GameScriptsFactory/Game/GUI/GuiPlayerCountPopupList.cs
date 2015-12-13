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

/// <summary>
/// PopupList allowing selection of the number of Players when starting a new game.
/// Also dynamically manages how many AI Players are displayed to the User.
/// Minimum is 2, the human player and 1 AI player.
/// </summary>
public class GuiPlayerCountPopupList : AGuiMenuPopupList<int> {

    public override GuiElementID ElementID { get { return GuiElementID.PlayerCountPopupList; } }

    protected override string[] Choices { get { return _countChoices; } }

    private string[] _countChoices;
    private IDictionary<GuiElementID, UIWidget> _aiPlayerContainerLookup;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        InitializeCountChoices();
        PopulateAIPlayerContainerLookup();
    }

    private void InitializeCountChoices() {
        _countChoices = Enumerable.Range(start: 2, count: TempGameValues.MaxAIPlayers).Select(value => value.ToString()).ToArray();
    }

    #region Event and Property Change Handlers

    protected override void PopupListSelectionChangedEventHandler() {
        base.PopupListSelectionChangedEventHandler();
        int aiPlayerCount = int.Parse(_popupList.value) - Constants.One;
        RefreshAIPlayerAvailability(aiPlayerCount);
    }

    #endregion

    #region Dynamic AI Player Show/Hide System

    private void PopulateAIPlayerContainerLookup() {
        _aiPlayerContainerLookup = new Dictionary<GuiElementID, UIWidget>(TempGameValues.MaxAIPlayers);
        var newGameMenuWindow = gameObject.GetSingleComponentInParents<AGuiWindow>();
        var aiIQPopups = newGameMenuWindow.gameObject.GetSafeComponentsInChildren<GuiPlayerIQPopupList>(includeInactive: true);
        aiIQPopups.ForAll(aiIQPopup => {
            var aiPlayerContainer = aiIQPopup.transform.parent.parent.gameObject.GetSafeComponent<UIWidget>();  // HACK
            _aiPlayerContainerLookup.Add(aiIQPopup.ElementID, aiPlayerContainer);
        });
    }

    /// <summary>
    /// Refreshes the AI Players that are available to choose.
    /// </summary>
    /// <param name="aiPlayerCount">The AIPlayer count.</param>
    /// <exception cref="System.NotImplementedException"></exception>
    private void RefreshAIPlayerAvailability(int aiPlayerCount) {
        D.Log("{0}.RefreshAIPlayerAvailability() called. AIPlayer count = {1}.", ElementID.GetValueName(), aiPlayerCount);
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

