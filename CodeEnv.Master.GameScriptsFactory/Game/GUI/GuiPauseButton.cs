// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiPauseButton.cs
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
public class GuiPauseButton : GuiButtonBase {

    protected override void Initialize() {
        base.Initialize();
        bool initialPauseState = playerPrefsMgr.IsPauseOnLoadPref;
        GameManager.IsGamePaused = initialPauseState;
        ChangeButtonLabel(initialPauseState);
        tooltip = "Pause or resume the game.";
    }

    protected override void OnButtonClick(GameObject sender) {
        // Toggle pause state
        bool toPause = !GameManager.IsGamePaused;
        GameManager.IsGamePaused = toPause;
        ChangeButtonLabel(toPause);
    }

    private void ChangeButtonLabel(bool toPause) {
        UILabel pauseButtonLabel = button.GetComponentInChildren<UILabel>();
        pauseButtonLabel.text = (toPause) ? UIMessages.ResumeButtonLabel : UIMessages.PauseButtonLabel;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

