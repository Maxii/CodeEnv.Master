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

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The PopupList that selects the UniverseSize value in the NewGameMenu.
/// Note: The enum used is actually UniverseSizeGuiSelection rather than UniverseSize.
/// This allows the use of a Random value.
/// </summary>
public class GuiUniverseSizePopupList : AGuiMenuPopupList<UniverseSizeGuiSelection> {

    public event EventHandler universeSizeChanged;

    public override GuiElementID ElementID { get { return GuiElementID.UniverseSizePopupList; } }

    public override string ConvertedSelectedValue {
        get {
            string unconvertedSelectedValue = SelectedValue;
            UniverseSize convertedValue = Enums<UniverseSizeGuiSelection>.Parse(unconvertedSelectedValue).Convert();
            return convertedValue.GetValueName();
        }
    }

    protected override bool IncludesRandom { get { return true; } }

    protected override string[] Choices { get { return Enums<UniverseSizeGuiSelection>.GetNames(excludeDefault: true); } }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this pop up list until the Menu Accept Button is pushed

    #region Event and Property Change Handlers

    protected override void PopupListSelectionChangedEventHandler() {
        base.PopupListSelectionChangedEventHandler();
        OnUniverseSizeChanged();
    }

    private void OnUniverseSizeChanged() {
        if (universeSizeChanged != null) {
            universeSizeChanged(this, EventArgs.Empty);
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

