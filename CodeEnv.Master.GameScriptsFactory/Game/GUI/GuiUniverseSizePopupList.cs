// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiUniverseSizePopupList.cs
// The PopupList that selects Universe Size.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The PopupList that selects Universe Size.
/// </summary>
public class GuiUniverseSizePopupList : AGuiPopupList<UniverseSizeGuiSelection> {

    public override GuiMenuElementID ElementID { get { return GuiMenuElementID.UniverseSizePopupList; } }

    protected override bool IncludesRandom { get { return true; } }

    public override bool HasPreference { get { return true; } }

    protected override string[] GetNames() { return Enums<UniverseSizeGuiSelection>.GetNames(excludeDefault: true); }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

