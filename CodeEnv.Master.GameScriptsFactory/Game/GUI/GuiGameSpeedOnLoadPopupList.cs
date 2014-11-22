﻿// --------------------------------------------------------------------------------------------------------------------
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

using CodeEnv.Master.Common;

/// <summary>
/// The GameSpeedOnLoad option popupList.
/// </summary>
public class GuiGameSpeedOnLoadPopupList : AGuiEnumPopupListBase<GameClockSpeed> {

    protected override void OnPopupListSelectionChange() { }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

