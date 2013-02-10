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
public class GuiGameSpeedSlider : GuiSliderBase {

    private GameEventManager eventMgr;

    private GameClockSpeed[] speedsOrderedByRisingValue;
    private float[] orderedSliderStepValues;

    protected override void Initialize() {
        base.Initialize();
        eventMgr = GameEventManager.Instance;

        InitializeSlider();
        tooltip = "Controls how fast time in the Game progresses.";
    }

    private void InitializeSlider() {
        GameClockSpeed[] speeds = Enum.GetValues(typeof(GameClockSpeed)) as GameClockSpeed[];
        speeds = speeds.Except<GameClockSpeed>(GameClockSpeed.None).ToArray<GameClockSpeed>();

        int numberOfSliderSteps = speeds.Length;
        slider.numberOfSteps = numberOfSliderSteps;

        //var sortedSpeeds = from s in speeds orderby s.GetSpeedMultiplier() select s;   // using Linq Query syntax
        var sortedSpeeds = speeds.OrderBy(s => s.GetSpeedMultiplier());   // using IEnumerable extension methods and lamba
        speedsOrderedByRisingValue = sortedSpeeds.ToArray<GameClockSpeed>();
        orderedSliderStepValues = MyNguiUtilities.GenerateOrderedSliderStepValues(numberOfSliderSteps);

        // Set Sliders initial value to that associated with GameClockSpeed.Normal
        int indexOfNormalSpeed = speedsOrderedByRisingValue.FindIndex<GameClockSpeed>(s => (s == GameClockSpeed.Normal));
        float sliderValueAtNormalSpeed = orderedSliderStepValues[indexOfNormalSpeed];
        slider.sliderValue = sliderValueAtNormalSpeed;
    }

    protected override void OnSliderValueChange(float value) {
        float tolerance = 0.05F;
        int index = orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(value, v, tolerance));
        Arguments.ValidateNotNegative(index);
        GameClockSpeed clockSpeed = speedsOrderedByRisingValue[index];

        // dispatch event to GameClock and Game Speed Readout telling of change
        eventMgr.Raise<GameSpeedChangeEvent>(new GameSpeedChangeEvent(clockSpeed));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

