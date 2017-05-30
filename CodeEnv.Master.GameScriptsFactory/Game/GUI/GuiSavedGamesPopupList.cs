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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// PopupList allowing selection from all games that have been saved.
/// </summary>
public class GuiSavedGamesPopupList : AGuiMenuPopupList<string> {

    private static string[] __dummySavedGameNames = new string[] { "DummySave1", "DummySave2", "DummySave3" };

    public override GuiElementID ElementID { get { return GuiElementID.SavedGamesPopupList; } }

    protected override string[] Choices { get { return __dummySavedGameNames; } }

    //protected override string[] GetNames() {
    //    var savedGames = LevelSerializer.SavedGames[LevelSerializer.PlayerName];
    //    return savedGames.Select(game => game.Caption).ToArray();
    //}

}

