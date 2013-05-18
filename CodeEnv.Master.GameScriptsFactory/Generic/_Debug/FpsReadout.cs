// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FpsReadout.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class FpsReadout : GuiLabelReadoutBase {

    public float secondsBetweenDisplayRefresh = 0.5F;

    private float accumulatedFpsOverInterval = Constants.ZeroF;
    private int framesDrawnInInterval = Constants.Zero;
    private float timeRemainingInInterval;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        timeRemainingInInterval = secondsBetweenDisplayRefresh;
        tooltip = "Current Frames per Second displayed.";
    }

    void Update() {
        // this is a tool, so I'll simply use Unity RealTime_Unity
        float timeSinceLastUpdate = Time.deltaTime;
        timeRemainingInInterval -= timeSinceLastUpdate;
        accumulatedFpsOverInterval += Time.timeScale / timeSinceLastUpdate;
        ++framesDrawnInInterval;

        // Interval ended - update GUI text and start new interval
        if (timeRemainingInInterval <= Constants.ZeroF) {
            // display two fractional digits (f2 formattedFpsValue)
            float fps = accumulatedFpsOverInterval / framesDrawnInInterval;
            RefreshFpsReadout(fps);
            //	DebugConsole.Log(formattedFpsValue,level);
            timeRemainingInInterval = secondsBetweenDisplayRefresh;
            accumulatedFpsOverInterval = Constants.ZeroF;
            framesDrawnInInterval = Constants.Zero;
        }
    }

    private void RefreshFpsReadout(float fps) {
        string formattedFpsValue = string.Format("{0:F1} FPS", fps);

        Color color = Color.green;
        if (fps < 25) {
            color = Color.yellow;
        }
        else if (fps < 10) {
            color = Color.red;
        }

        readoutLabel.text = formattedFpsValue;
        readoutLabel.color = color;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

