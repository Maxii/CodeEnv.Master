// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedOnLoadPopupList.cs
// The GameSpeedOnLoad option popupList.
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
/// The GameSpeedOnLoad option popupList.
/// </summary>
public class GuiGameSpeedOnLoadPopupList : AGuiPopupList<GameClockSpeed> {

    public override GuiMenuElementID ElementID { get { return GuiMenuElementID.GameSpeedOnLoadPopupList; } }

    public override bool HasPreference { get { return true; } }

    protected override string[] GetNames() { return Enums<GameClockSpeed>.GetNames(excludeDefault: true); }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

