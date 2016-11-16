// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerStartLevelPopupList.cs
// Player EmpireStartLevel selection popup list in the NewGameMenu. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Player EmpireStartLevel selection popup list in the NewGameMenu. 
/// </summary>
public class GuiPlayerStartLevelPopupList : AGuiMenuPopupList<EmpireStartLevelGuiSelection> {

    [Tooltip("The unique ID of this PlayerStartLevelPopupList GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    public override string ConvertedSelectedValue {
        get {
            string unconvertedSelectedValue = SelectedValue;
            EmpireStartLevel convertedValue = Enums<EmpireStartLevelGuiSelection>.Parse(unconvertedSelectedValue).Convert();
            return convertedValue.GetValueName();
        }
    }

    protected override string TooltipContent { get { return "Select how advanced this player's empire starts"; } }

    protected override string[] Choices { get { return Enums<EmpireStartLevelGuiSelection>.GetNames(excludeDefault: true); } }

    protected override bool IncludesRandom { get { return true; } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

