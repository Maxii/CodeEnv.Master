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
public class GuiQualitySettingPopupList : AGuiPopupListBase {

    private string[] _qualityNames;

    protected override void ConfigurePopupList() {
        base.ConfigurePopupList();
        _qualityNames = QualitySettings.names;
    }

    protected override void InitializeListValues() {
        _popupList.items.Clear();
        _qualityNames.ForAll(qName => {
            //D.Log("Adding QualitySetting name {0}.", qName);
            _popupList.items.Add(qName);
        });
    }

    protected override void InitializeSelection() {
        int qualitySettingPreference = PlayerPrefsManager.Instance.QualitySetting;
        _popupList.value = _popupList.items[qualitySettingPreference];
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

