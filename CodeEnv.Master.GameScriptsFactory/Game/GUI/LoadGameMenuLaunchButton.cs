﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: LoadGameMenuLaunchButton.cs
// Launch button for the Load[Saved]GameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Launch button for the Load[Saved]GameMenu.
/// </summary>
public class LoadGameMenuLaunchButton : AGuiMenuAcceptButton {

    private static IEnumerable<KeyCode> _validKeys = new KeyCode[] { KeyCode.Return };

    protected override IEnumerable<KeyCode> ValidKeys { get { return _validKeys; } }

    protected override string TooltipContent { get { return "Launch the selected Saved Game."; } }

    private string _selectedGameCaption = string.Empty;

    protected override void RecordPopupListState(GuiElementID popupListID, string selection, string convertedSelection) {
        base.RecordPopupListState(popupListID, selection, convertedSelection);
        switch (popupListID) {
            case GuiElementID.SavedGamesPopupList:
                _selectedGameCaption = selection;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        base.HandleValidClick();
        ////LoadSavedGame();
        __LoadDummySavedGame();
    }

    #endregion

    private void __LoadDummySavedGame() {
        _gameMgr.LoadSavedGame(_selectedGameCaption);
    }

    ////private void LoadSavedGame() {
    ////    if (LevelSerializer.SavedGames.Count > 0) {
    ////        Arguments.ValidateForContent(_selectedGameCaption);
    ////        _gameMgr.LoadSavedGame(_selectedGameCaption);
    ////    }
    ////}

    protected override void __ValidateCapturedState() {
        base.__ValidateCapturedState();
        // selectedGameCaption can be empty if there are no saved games
        if (_selectedGameCaption == string.Empty) {
            D.Warn("Selected Game Caption to Load is empty.");
        }
    }

    protected override void Cleanup() { }


}

