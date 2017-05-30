// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSystemDensityPopupList.cs
// The PopupList that selects the SystemDensity value in the NewGameMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The PopupList that selects the SystemDensity value in the NewGameMenu.
/// Note: The enum used is actually SystemDensityGuiSelection rather than SystemDensity.
/// This allows the use of a Random value.
/// </summary>
public class GuiSystemDensityPopupList : AGuiMenuPopupList<SystemDensityGuiSelection> {

    public override GuiElementID ElementID { get { return GuiElementID.SystemDensityPopupList; } }

    public override string ConvertedSelectedValue {
        get {
            string unconvertedSelectedValue = SelectedValue;
            SystemDensity convertedValue = Enums<SystemDensityGuiSelection>.Parse(unconvertedSelectedValue).Convert();
            return convertedValue.GetValueName();
        }
    }

    protected override bool IncludesRandom { get { return true; } }

    protected override string[] Choices { get { return Enums<SystemDensityGuiSelection>.GetNames(excludeDefault: true); } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this pop up list until the Menu Accept Button is pushed

}

