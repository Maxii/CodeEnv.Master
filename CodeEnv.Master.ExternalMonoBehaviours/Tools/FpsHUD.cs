// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FpsHUD.cs
// FPS Counter as a HeadsUpDisplay. 
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
using CodeEnv.Master.Common.Resources;
using CodeEnv.Master.Common.Unity;


/// <summary>
/// FPS Counter as a HeadsUpDisplay. Attach it to a GUIText to make a framesDrawnInInterval/second indicator.
///
/// It calculates framesDrawnInInterval/second over each secondsBetweenDisplayRefresh, so the displayed value is more stable.
/// It is also fairly accurate at very low FPS counts (<10).
/// </summary>
[RequireComponent(typeof(GUIText))]
public class FpsHUD : MonoBehaviour {

    public float secondsBetweenDisplayRefresh = 0.5F;

    private float accumulatedFpsOverInterval = Constants.ZeroF;
    private int framesDrawnInInterval = Constants.Zero;
    private float timeRemainingInInterval;

    void Start() {
        if (!guiText) {
            Debug.LogError("FpsHUD needs a GUIText component!");
            enabled = false;
            return;
        }
        timeRemainingInInterval = secondsBetweenDisplayRefresh;
    }

    void Update() {
        float timeSinceLastUpdate = Time.deltaTime;
        timeRemainingInInterval -= timeSinceLastUpdate;
        accumulatedFpsOverInterval += Time.timeScale / timeSinceLastUpdate;
        ++framesDrawnInInterval;

        // Interval ended - update GUI text and start new interval
        if (timeRemainingInInterval <= Constants.ZeroF) {
            // display two fractional digits (f2 formattedFpsValue)
            float fps = accumulatedFpsOverInterval / framesDrawnInInterval;
            string formattedFpsValue = string.Format("{0:F2} FPS", fps);
            guiText.text = formattedFpsValue;

            Color color = Color.green;
            if (fps < 25) {
                color = Color.yellow;
            }
            else if (fps < 10) {
                color = Color.red;
            }
            guiText.material.color = color;
            //	DebugConsole.Log(formattedFpsValue,level);
            timeRemainingInInterval = secondsBetweenDisplayRefresh;
            accumulatedFpsOverInterval = Constants.ZeroF;
            framesDrawnInInterval = Constants.Zero;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

