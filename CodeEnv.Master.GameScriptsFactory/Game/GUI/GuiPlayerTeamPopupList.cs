// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerTeamPopupList.cs
// Player Team selection popup list in the NewGameMenu.  
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
/// Player Team selection popup list in the NewGameMenu.  
/// </summary>
public class GuiPlayerTeamPopupList : AGuiMenuPopupList<TeamID> {

    [Tooltip("The unique ID of this PlayerTeamPopupList GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    protected override string TooltipContent { get { return "Select the team this player belongs too"; } }

    protected override string[] Choices { get { return Enums<TeamID>.GetNames(excludeDefault: true); } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

