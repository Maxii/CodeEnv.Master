// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiQualitySettingPopupList.cs
// Manager for the QualitySetting option popupList.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manager for the QualitySetting option popupList.
/// </summary>
public class GuiQualitySettingPopupList : AGuiPopupListBase {

    private string[] _qualityNames;

    protected override void ConfigurePopupList() {
        base.ConfigurePopupList();
        _qualityNames = QualitySettings.names;
    }

    protected override void InitializeListValues() {
        popupList.items.Clear();
        foreach (var name in _qualityNames) {
            //Logger.Log("Adding QualitySetting name {0}.", name);
            popupList.items.Add(name);
        }
    }

    protected override void InitializeSelection() {
        int qualitySettingPreference = PlayerPrefsManager.Instance.QualitySetting;
        popupList.selection = popupList.items[qualitySettingPreference];
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

