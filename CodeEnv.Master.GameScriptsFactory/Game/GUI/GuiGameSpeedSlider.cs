// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedSlider.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiGameSpeedSlider : AGuiEnumSliderBase<GameClockSpeed> {

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        tooltip = "Controls how fast time in the Game progresses.";
    }

    protected override void OnSliderValueChange(GameClockSpeed value) {
        GameTime.Instance.GameSpeed = value;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

