// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewGameMenuCancelButton.cs
// Menu Cancel Button that restores the original state of the NewGameMenu to what it was when it was first shown
// by its parent GuiWindow
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Menu Cancel Button that restores the original state of the NewGameMenu to what it was when it was first shown
/// by its parent GuiWindow. This version uses the PlayerColorManager to restore the original state of the PlayerColor
/// PopupLists as the color popupList's choices also need to be restored.
/// </summary>
public class NewGameMenuCancelButton : MenuCancelButton {

    private PlayerColorManager _playerColorMgr;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        var newGameWindow = gameObject.GetSingleComponentInParents<GuiWindow>();
        _playerColorMgr = newGameWindow.gameObject.GetSingleComponentInChildren<PlayerColorManager>();  // moved PlayerColorManager to Players Container    
    }

    protected override void SubscribeToParentWindowShowBeginEvent() {
        // capturing the state of the new game menu is only needed one time since the Launch button destroys this instance
        EventDelegate.Add(_window.onShowBegin, WindowShowBeginEventHandler, oneShot: true); // OPTIMIZE isOneShot of any value?
    }

    protected override void RestorePopupListsState() {
        _playerColorMgr.ResetColorPopupListValues();
        base.RestorePopupListsState();  // restores only the popupLists that need restoring, so will ignore the UIPopupLists handling PlayerColor
    }

}

