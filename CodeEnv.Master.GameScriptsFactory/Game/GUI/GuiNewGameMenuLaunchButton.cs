// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiNewGameMenuLaunchButton.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiNewGameMenuLaunchButton : GuiMenuAcceptButtonBase {

    private UniverseSize newGameSize;

    protected override void Initialize() {
        base.Initialize();
        tooltip = "Launch New Game with these settings.";
    }

    protected override void OnCheckboxStateChange(bool state) { }

    protected override void OnPopupListSelectionChange(string item) {
        // UIPopupList.current gives the reference to the sender, but there is nothing except names to distinguish them
        if (Enums<UniverseSize>.TryParse(item, true, out newGameSize)) {
            Debug.Log("UniverseSize {0} PopupList change event received by LaunchGameButton.".Inject(item));
            return;
        }
        // more popupLists here
    }

    protected override void OnSliderValueChange(float value) { }

    protected override void OnButtonClick(GameObject sender) {
        NewGameSettings newGameSettings = new NewGameSettings();
        newGameSettings.SizeOfUniverse = newGameSize;
        eventMgr.Raise<LaunchNewGameEvent>(new LaunchNewGameEvent(newGameSettings));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

