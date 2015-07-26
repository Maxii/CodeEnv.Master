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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Frames Per Second readout label for debug support.
/// </summary>
public class FpsReadout : AGuiLabelReadout {

    public static float FramesPerSecond { get { return _lastFpsValue; } }

    private static float _redFramerate = TempGameValues.MinimumFramerate;
    private static float _yellowFramerate = TempGameValues.MinimumFramerate + 5F;
    private static string _formattedFpsText = "{0:F1} FPS";
    private static float _lastFpsValue = 500F;

    public float secondsBetweenDisplayRefresh = 0.5F;

    protected override string TooltipContent { get { return "Current Frames per Second displayed."; } }

    private float _accumulatedFpsOverInterval;
    private int _framesDrawnInInterval;
    private float _timeRemainingInInterval;

    protected override void Awake() {
        base.Awake();
        _timeRemainingInInterval = secondsBetweenDisplayRefresh;
        if (GameManager.Instance.CurrentScene == SceneLevel.GameScene) {
            enabled = false;
            Subscribe();
        }
        // if LobbyScene enabled = true from beginning
    }

    private void Subscribe() {
        GameManager.Instance.onIsRunningOneShot += OnIsRunning;
    }

    protected override void Update() {
        base.Update();
        // this is a tool, so simply use Unity's time
        float timeSinceLastUpdate = Time.deltaTime;
        _timeRemainingInInterval -= timeSinceLastUpdate;
        _accumulatedFpsOverInterval += Time.timeScale / timeSinceLastUpdate;
        ++_framesDrawnInInterval;

        // Interval ended - update GUI text and start new interval
        if (_timeRemainingInInterval <= Constants.ZeroF) {
            // display two fractional digits (f2 formattedFpsValue)
            _lastFpsValue = _accumulatedFpsOverInterval / _framesDrawnInInterval;
            RefreshReadout();
            _timeRemainingInInterval = secondsBetweenDisplayRefresh;
            _accumulatedFpsOverInterval = Constants.ZeroF;
            _framesDrawnInInterval = Constants.Zero;
        }
    }

    private void RefreshReadout() {
        GameColor color = GameColor.Green;
        if (_lastFpsValue < _redFramerate) {
            color = GameColor.Red;
        }
        else if (_lastFpsValue < _yellowFramerate) {
            color = GameColor.Yellow;
        }
        RefreshReadout(_formattedFpsText.Inject(_lastFpsValue), color);
    }

    private void OnIsRunning() {
        enabled = true;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

