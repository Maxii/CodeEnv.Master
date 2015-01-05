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
public class GuiQualitySettingPopupList : AGuiPopupList<string> {

    public override GuiMenuElementID ElementID { get { return GuiMenuElementID.QualitySettingPopupList; } }

    public override bool HasPreference { get { return true; } }

    protected override string[] GetNames() { return QualitySettings.names; }

    // no need for taking an action OnPopupListSelectionChanged as changes aren't recorded 
    // from this popup list until the Menu Accept Button is pushed

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

