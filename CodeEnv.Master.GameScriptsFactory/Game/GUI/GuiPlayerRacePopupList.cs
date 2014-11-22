﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPlayerRacePopupList.cs
// Player Race selection popup list in the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Player Race selection popup list in the Gui.
/// </summary>
public class GuiPlayerRacePopupList : AGuiEnumPopupListBase<Species> {

    // no need for taking an action OnPopupListSelectionChange as changes aren't recorded 
    // from this popup list until the NewGameLaunchAcceptButton is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

