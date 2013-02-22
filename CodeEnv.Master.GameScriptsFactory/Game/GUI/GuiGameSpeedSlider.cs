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
public class GuiGameSpeedSlider : GuiSliderBase, IDisposable {

    private GameClockSpeed[] speedsOrderedByRisingValue;
    private float[] orderedSliderStepValues;

    protected override void Initialize() {
        base.Initialize();
        InitializeSlider();
        eventMgr.AddListener<GameLoadedEvent>(OnGameLoaded);
        tooltip = "Controls how fast time in the Game progresses.";
    }

    private void OnGameLoaded(GameLoadedEvent e) {
        GameClockSpeed speed = playerPrefsMgr.GameSpeedOnLoad;
        SetSliderValue(speed);
        slider.ForceUpdate();   // forces an event so GameTime knows
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
        GameClockSpeed initialSpeed = playerPrefsMgr.GameSpeedOnLoad;
        SetSliderValue(initialSpeed);
    }

    private void SetSliderValue(GameClockSpeed speed) {
        int indexOfSpeed = speedsOrderedByRisingValue.FindIndex<GameClockSpeed>(s => (s == speed));
        float sliderValueOfSpeed = orderedSliderStepValues[indexOfSpeed];
        slider.sliderValue = sliderValueOfSpeed;
    }

    protected override void OnSliderValueChange(float value) {
        float tolerance = 0.05F;
        int index = orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(value, v, tolerance));
        Arguments.ValidateNotNegative(index);
        GameClockSpeed clockSpeed = speedsOrderedByRisingValue[index];

        // dispatch event to GameTime and Game Speed Readout telling of change
        eventMgr.Raise<GameSpeedChangeEvent>(new GameSpeedChangeEvent(clockSpeed));
    }

    #region IDisposable
    [NonSerialized]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            eventMgr.RemoveListener<GameLoadedEvent>(OnGameLoaded);
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

