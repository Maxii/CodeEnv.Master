// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedOnLoadPopupList.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiGameSpeedOnLoadPopupList : AGuiEnumPopupListBase<GameClockSpeed> {

    protected override void OnPopupListSelectionChange(string item) { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

