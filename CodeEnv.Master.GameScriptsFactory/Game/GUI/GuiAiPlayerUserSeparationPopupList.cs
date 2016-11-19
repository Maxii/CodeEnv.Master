// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiAiPlayerUserSeparationPopupList.cs
// AIPlayer UserSeparation selection popup list in the NewGameMenu.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AIPlayer UserSeparation selection popup list in the NewGameMenu.  
/// </summary>
public class GuiAiPlayerUserSeparationPopupList : AGuiMenuPopupList<PlayerSeparationGuiSelection> {

    [Tooltip("The unique ID of this AIPlayerUserSeparation GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    public override string ConvertedSelectedValue {
        get {
            string unconvertedSelectedValue = SelectedValue;
            PlayerSeparation convertedValue = Enums<PlayerSeparationGuiSelection>.Parse(unconvertedSelectedValue).Convert();
            return convertedValue.GetValueName();
        }
    }

    protected override string TooltipContent { get { return "Select the starting separation between this player and the user"; } }

    protected override string[] Choices { get { return Enums<PlayerSeparationGuiSelection>.GetNames(excludeDefault: true); } }

    protected override bool IncludesRandom { get { return true; } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

