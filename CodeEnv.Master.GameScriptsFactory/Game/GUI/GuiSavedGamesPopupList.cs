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

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// PopupList allowing selection from all games that have been saved.
/// </summary>
public class GuiSavedGamesPopupList : AGuiPopupList<string> {

    private static string[] __dummySavedGameNames = new string[] { "DummySave1", "DummySave2", "DummySave3" };

    public override GuiElementID ElementID { get { return GuiElementID.SavedGamesPopupList; } }

    protected override string[] GetNames() { return __dummySavedGameNames; }

    //protected override string[] GetNames() {
    //    var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
    //    return savedGames.Select(sg => sg.Caption).ToArray();
    //}

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

