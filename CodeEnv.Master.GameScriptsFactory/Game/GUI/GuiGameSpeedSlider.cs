// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGameSpeedSlider.cs
// Gui Slider that changes the speed of the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Gui Slider that changes the speed of the game.
/// </summary>
public class GuiGameSpeedSlider : AGuiEnumSliderBase<GameClockSpeed> {

    protected override void InitializeTooltip() {
        tooltip = "Controls how fast time in the Game progresses.";
    }

    protected override void OnSliderTValueChange(GameClockSpeed value) {
        //D.Log("{0}.OnSliderTValueChange({1}.{2}) called.", GetType().Name, typeof(GameClockSpeed).Name, value.GetName());
        GameTime.Instance.GameSpeed = value;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

