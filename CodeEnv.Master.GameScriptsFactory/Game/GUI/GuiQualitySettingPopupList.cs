// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiQualitySettingPopupList.cs
// The QualitySetting option popupList.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The QualitySetting option popupList.
/// </summary>
public class GuiQualitySettingPopupList : AGuiMenuPopupList<string> {

    public override GuiElementID ElementID { get { return GuiElementID.QualitySettingPopupList; } }

    protected override string[] Choices { get { return QualitySettings.names; } }

    // no need for taking an action via PopupListSelectionChangedEventHandler as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

