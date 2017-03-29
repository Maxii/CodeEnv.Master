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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Frames Per Second readout label for debug support.
/// </summary>
public class FpsReadout : AGuiLabelReadout, IFpsReadout {

    private const string FormattedFpsText = "{0:F1} FPS";
    private const float _yellowFramerate = TempGameValues.MinimumFramerate;
    private static float _redFramerate = TempGameValues.MinimumFramerate - 5F;

    [Tooltip("Number of seconds between display refreshes")]
    [Range(0.1F, 1F)]
    [SerializeField]
    private float _displayRefreshPeriod = 0.4F;

    public string DebugName { get { return GetType().Name; } }

    public float FramesPerSecond { get { return _lastFpsValue; } }

    private bool _isReadoutToShow;
    public bool IsReadoutToShow {
        get { return _isReadoutToShow; }
        set {
#if DEBUG
            _isReadoutToShow = value;
#else
            if(value) {
                D.Warn("{0} instructed to show when not in Debug mode.", DebugName);
            }
            _isReadoutToShow = false;
#endif
        }
    }

    protected override string TooltipContent { get { return "Current Frames per Second."; } }

    private float _timeRemainingInDisplayRefreshInterval;
    private float _calcRefreshPeriod = 0.1F;
    private float _lastFpsValue;
    private float _accumulatedFpsOverCalcInterval;
    private int _framesDuringCalcInterval;
    private float _timeRemainingInCalcInterval;

    #region MonoBehaviour Singleton Pattern

    protected static FpsReadout _instance;
    public static FpsReadout Instance {
        get {
            if (_instance == null) {
                if (IsApplicationQuiting) {
                    //D.Warn("Application is quiting while trying to access {0}.Instance.".Inject(typeof(FpsReadout).Name));
                    return null;
                }
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(FpsReadout);
                _instance = GameObject.FindObjectOfType(thisType) as FpsReadout;
                // value is required for the first time, so look for it                        
                if (_instance == null) {
                    var stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetMethod().DeclaringType, stackFrame.GetMethod().Name);
                    D.Error("No instance of {0} found. Is it destroyed/deactivated? Called by {1}.".Inject(thisType.Name, callerIdMessage));
                }
                _instance.InitializeOnInstance();
            }
            return _instance;
        }
    }

    protected sealed override void Awake() {
        base.Awake();
        // If no other MonoBehaviour has requested Instance in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object
        if (_instance == null) {
            _instance = this as FpsReadout;
            InitializeOnInstance();
        }
        InitializeOnAwake();
    }

    #endregion

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// Note: This method is not called by instance copies, only by the original instance. If not persistent across scenes,
    /// then this method will be called each time the new instance in a scene is instantiated.
    /// </summary>
    private void InitializeOnInstance() {
        GameReferences.FpsReadout = _instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    private void InitializeOnAwake() {
        InitializeValuesAndReferences();
        AssessShowWidgets();
    }

    private void InitializeValuesAndReferences() {
        _timeRemainingInCalcInterval = _calcRefreshPeriod;
        _timeRemainingInDisplayRefreshInterval = _displayRefreshPeriod;
    }

    private void AssessShowWidgets() {
        UISprite textFieldWidget = GetComponent<UISprite>();
        Collider collider = GetComponent<Collider>();
#if DEBUG
        textFieldWidget.enabled = true;
        _readoutLabel.enabled = true;
        collider.enabled = true;
#else
        textFieldWidget.enabled = false;
        _readoutLabel.enabled = false;
        collider.enabled = false;
#endif
    }

    private void RefreshReadout() {
        D.Assert(IsReadoutToShow);

        GameColor color = GameColor.Green;
        if (_lastFpsValue < _redFramerate) {
            color = GameColor.Red;
        }
        else if (_lastFpsValue < _yellowFramerate) {
            color = GameColor.Yellow;
        }
        RefreshReadout(FormattedFpsText.Inject(_lastFpsValue), color);
    }

    #region Event and Property Change Handlers

    void Update() {
        // this is a tool, so simply use Unity's time
        float timeSinceLastUpdate = Time.deltaTime;
        _timeRemainingInCalcInterval -= timeSinceLastUpdate;
        _timeRemainingInDisplayRefreshInterval -= timeSinceLastUpdate;
        _accumulatedFpsOverCalcInterval += Time.timeScale / timeSinceLastUpdate;
        ++_framesDuringCalcInterval;

        if (_timeRemainingInCalcInterval <= Constants.ZeroF) {
            // Calc interval ended - update FPS value and start new calc interval
            _lastFpsValue = _accumulatedFpsOverCalcInterval / _framesDuringCalcInterval;
            _timeRemainingInCalcInterval = _calcRefreshPeriod;
            _accumulatedFpsOverCalcInterval = Constants.ZeroF;
            _framesDuringCalcInterval = Constants.Zero;
        }

        if (_timeRemainingInDisplayRefreshInterval <= Constants.ZeroF) {
            // Display refresh interval ended - refresh the readout and start new refresh interval
            _timeRemainingInDisplayRefreshInterval = _displayRefreshPeriod;
            if (IsReadoutToShow) {
                RefreshReadout();
            }
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        _instance = null;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

