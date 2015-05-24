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
public class GuiGameSpeedSlider : AGuiEnumSlider<GameSpeed> {

    protected override string TooltipContent { get { return "Slide to adjust game speed."; } }

    protected override void OnSliderEnumValueChange(GameSpeed value) {
        //D.Log("{0}.OnSliderTValueChange({1}.{2}) called.", GetType().Name, typeof(GameClockSpeed).Name, value.GetName());
        GameTime.Instance.GameSpeed = value;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

