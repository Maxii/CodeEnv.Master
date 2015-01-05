﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerColorPopupList.cs
// Player Color selection popup list in the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Player Color selection popup list in the Gui.
/// </summary>
public class GuiPlayerColorPopupList : AGuiPopupList<GameColor> {

    public GuiMenuElementID elementID;

    public bool hasPreference;

    public override GuiMenuElementID ElementID { get { return elementID; } }

    public override bool HasPreference { get { return hasPreference; } }

    protected override string[] GetNames() { return Enums<GameColor>.GetNames(excludeDefault: true); }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

