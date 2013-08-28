// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FpsReadout.cs
// Frames Per Second readout label for debug support.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Frames Per Second readout label for debug support.
/// </summary>
public class FpsReadout : AGuiLabelReadoutBase {

    public float secondsBetweenDisplayRefresh = 0.5F;

    private float _accumulatedFpsOverInterval = Constants.ZeroF;
    private int _framesDrawnInInterval = Constants.Zero;
    private float _timeRemainingInInterval;

    protected override void Awake() {
        base.Awake();
        _timeRemainingInInterval = secondsBetweenDisplayRefresh;
        tooltip = "Current Frames per Second displayed.";
    }

    void Update() {
        // this is a tool, so I'll simply use Unity RealTime_Unity
        float timeSinceLastUpdate = Time.deltaTime;
        _timeRemainingInInterval -= timeSinceLastUpdate;
        _accumulatedFpsOverInterval += Time.timeScale / timeSinceLastUpdate;
        ++_framesDrawnInInterval;

        // Interval ended - update GUI text and start new interval
        if (_timeRemainingInInterval <= Constants.ZeroF) {
            // display two fractional digits (f2 formattedFpsValue)
            float fps = _accumulatedFpsOverInterval / _framesDrawnInInterval;
            RefreshReadout(fps);
            _timeRemainingInInterval = secondsBetweenDisplayRefresh;
            _accumulatedFpsOverInterval = Constants.ZeroF;
            _framesDrawnInInterval = Constants.Zero;
        }
    }

    private void RefreshReadout(float fps) {
        string formattedFpsValue = string.Format("{0:F1} FPS", fps);

        GameColor color = GameColor.Green;
        if (fps < 25) {
            color = GameColor.Yellow;
        }
        else if (fps < 15) {
            color = GameColor.Red;
        }

        RefreshReadout(formattedFpsValue, color);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

