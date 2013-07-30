// --------------------------------------------------------------------------------------------------------------------
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

/// <summary>
/// Player Color selection popup list in the Gui.
/// </summary>
public class GuiPlayerColorPopupList : AGuiEnumPopupListBase<GameColor> {

    // no need for taking an action OnPopupListSelectionChange as changes aren't recorded 
    // from this popup list until the NewGameLaunchAcceptButton is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

