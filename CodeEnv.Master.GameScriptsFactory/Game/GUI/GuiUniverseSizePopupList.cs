// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiUniverseSizePopupList.cs
//  The PopupList that selects the UniverseSize value in the NewGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The PopupList that selects the UniverseSize value in the NewGameMenu.
/// Note: The enum used is actually UniverseSizeGuiSelection rather than UniverseSize.
/// This allows the use of a Random value.
/// </summary>
public class GuiUniverseSizePopupList : AGuiMenuPopupList<UniverseSizeGuiSelection> {

    public override GuiElementID ElementID { get { return GuiElementID.UniverseSizePopupList; } }

    protected override bool IncludesRandom { get { return true; } }

    protected override string[] Choices { get { return Enums<UniverseSizeGuiSelection>.GetNames(excludeDefault: true); } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

