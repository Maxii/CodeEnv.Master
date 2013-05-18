// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSavedGamesPopupList.cs
// PopupList allowing selection from all games that have been saved.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// PopupList allowing selection from all games that have been saved.
/// </summary>
public class GuiSavedGamesPopupList : GuiPopupListBase {

    protected override void InitializeListValues() {
        popupList.items.Clear();
        PopulateList();
    }

    private void PopulateList() {
        var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
        if (savedGames.Count > 0) {
            popupList.gameObject.SetActive(true);
            popupList.items.Clear();
            popupList.textLabel.text = "Saved Games";
            foreach (var game in savedGames) {
                popupList.items.Add(game.Caption);
            }
        }
        else {
            popupList.textLabel.text = "No Saved Games";
            popupList.gameObject.SetActive(false);
        }
    }

    protected override void InitializeSelection() {
        popupList.selection = "Saved Games";
    }

    void OnEnable() {
        PopulateList();
    }

    protected override void OnPopupListSelectionChange(string item) { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

