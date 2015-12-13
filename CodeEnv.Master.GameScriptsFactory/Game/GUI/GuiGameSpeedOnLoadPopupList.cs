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
public class GuiGameSpeedOnLoadPopupList : AGuiMenuPopupList<GameSpeed> {

    public override GuiElementID ElementID { get { return GuiElementID.GameSpeedOnLoadPopupList; } }

    protected override string[] Choices { get { return Enums<GameSpeed>.GetNames(excludeDefault: true); } }

    // no need for taking an action via PopupListSelectionChangedEventHandler as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

