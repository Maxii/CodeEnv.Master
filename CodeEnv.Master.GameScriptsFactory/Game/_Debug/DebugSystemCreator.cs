// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSystemCreator.cs
// Creates a editor-configured system at its current location in the scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Creates a editor-configured system at its current location in the scene.
/// <remarks>Naming approach: 
/// <list type="bullet" >
/// <item>
///     <description>System: The name of the system is delivered by this SystemCreator using the name of its gameObject.  </description>
/// </item>
/// <item>
///     <description>Stars, Settlements, Planets and Moons: All names are automatically constructed using the name of the
/// system or planet, and the orbit slot they end up residing within (e.g. Regulus 1a).</description>
/// </item>
///  </remarks>
/// </summary>
[ExecuteInEditMode] // auto detects preset composition
public class DebugSystemCreator : SystemCreator {

    [SerializeField]
    private bool _isCompositionPreset = false;

    [Range(0, MaxPlanetsPerSystem)]
    [SerializeField]
    private int _maxPlanetsInSystem = 3;

    [Range(0, MaxMoonsPerSystem)]
    [SerializeField]
    private int _maxMoonsInSystem = 3;

    [Range(0, 2)]
    [SerializeField]
    private int _countermeasuresPerPlanetoid = 1;

    [Tooltip("Shows a label containing the System name")]
    [SerializeField]
    private bool _enableTrackingLabel = false;


    protected override bool IsCompositionPreset { get { return _isCompositionPreset; } }

    protected override int MaxPlanets { get { return _maxPlanetsInSystem; } }

    protected override int MaxMoons { get { return _maxMoonsInSystem; } }

    protected override int CMsPerPlanetoid { get { return _countermeasuresPerPlanetoid; } }

    protected override bool IsTrackingLabelEnabled { get { return _enableTrackingLabel; } }

    #region ExecuteInEditMode

    protected override void Awake() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.Awake();
    }

    protected override void Update() {
        if (Application.isPlaying) {
            enabled = false;    // Uses ExecuteInEditMode
        }
        base.Update();
        if (transform.childCount > Constants.Zero) {
            _isCompositionPreset = true;
        }
    }

    protected override void OnDestroy() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.OnDestroy();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

