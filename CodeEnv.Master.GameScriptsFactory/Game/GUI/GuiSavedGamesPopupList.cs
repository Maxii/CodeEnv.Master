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

    private static string[] __dummySavedGameNames = new string[] { "DummySave1", "DummySave2", "DummySave3" };

    protected override void InitializeListValues() {
        _popupList.items.Clear();
        //PopulateList();
        __PopulateDummyList();
    }

    private void __PopulateDummyList() {
        __dummySavedGameNames.ForAll(dsg => _popupList.items.Add(dsg));
    }

    private void PopulateList() {
        var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
        if (savedGames.Count > 0) {
            _popupList.items.Clear();
            savedGames.ForAll(sg => _popupList.items.Add(sg.Caption));
        }
        else {
            _popupList.value = "No Saved Games to Load";
        }
    }

    protected override void InitializeSelection() {
        _popupList.value = _popupList.items[0];
    }

    protected override void OnPopupListSelectionChange() { }

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

