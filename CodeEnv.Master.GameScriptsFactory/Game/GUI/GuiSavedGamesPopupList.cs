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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// PopupList allowing selection from all games that have been saved.
/// </summary>
public class GuiSavedGamesPopupList : AGuiPopupListBase {

    protected override void InitializeListValues() {
        popupList.items.Clear();
        PopulateList();
    }

    private void PopulateList() {
        var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
        if (savedGames.Count > 0) {
            popupList.gameObject.SetActive(true);
            popupList.items.Clear();
            //popupList.textLabel.text = "Saved Games"; // deprecated in 3.0.6
            popupList.value = "Saved Games";
            foreach (var game in savedGames) {
                popupList.items.Add(game.Caption);
            }
        }
        else {
            //popupList.textLabel.text = "No Saved Games";  // deprecated in 3.0.6
            popupList.value = "No Saved Games";
            popupList.gameObject.SetActive(false);
        }
    }

    protected override void InitializeSelection() {
        popupList.value = "Saved Games";
        //popupList.selection = "Saved Games";
    }

    protected override void OnEnable() {
        base.OnEnable();
        PopulateList();
    }

    protected override void OnPopupListSelectionChange() { }
    //protected override void OnPopupListSelectionChange(string item) { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

