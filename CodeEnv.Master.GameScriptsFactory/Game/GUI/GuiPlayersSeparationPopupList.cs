// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayersSeparationPopupList.cs
// Player Separation selection popup list in the NewGameMenu.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Player Separation selection popup list in the NewGameMenu.  
/// </summary>
public class GuiPlayersSeparationPopupList : AGuiMenuPopupList<PlayerSeparationGuiSelection> {

    public override GuiElementID ElementID { get { return GuiElementID.PlayersSeparationPopupList; } }

    public override string ConvertedSelectedValue {
        get {
            string unconvertedSelectedValue = SelectedValue;
            PlayerSeparation convertedValue = Enums<PlayerSeparationGuiSelection>.Parse(unconvertedSelectedValue).Convert();
            return convertedValue.GetValueName();
        }
    }

    protected override string TooltipContent { get { return "Select the starting separation between players"; } }

    protected override string[] Choices { get { return Enums<PlayerSeparationGuiSelection>.GetNames(excludeDefault: true); } }

    protected override bool IncludesRandom { get { return true; } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

}

