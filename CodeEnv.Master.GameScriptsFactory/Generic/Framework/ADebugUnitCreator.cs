// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADebugUnitCreator.cs
// Abstract base class for Unit Creators whose configuration is determined in the editor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Unit Creators whose configuration is determined in the editor.
/// </summary>
[ExecuteInEditMode] // auto detects preset composition
public abstract class ADebugUnitCreator : AUnitCreator {

    #region Serialized Editor fields

    [SerializeField]
    protected bool _isOwnerUser = false;

    [SerializeField]
    protected DebugDiploUserRelations _ownerRelationshipWithUser = DebugDiploUserRelations.Neutral;

    [SerializeField]
    private bool _isCompositionPreset;  // setting auto detected by ExecuteInEditMode

    [SerializeField]
    private bool _toDelayOperations = false;

    [Range(0, 19)]
    [SerializeField]
    private int _hourDelay = 0;

    [Range(0, 99)]
    [SerializeField]
    private int _dayDelay = 0;

    [Range(0, 10)]
    [SerializeField]
    private int _yearDelay = 0;

    [SerializeField]
    protected DebugLosWeaponLoadout _losWeaponsPerElement = DebugLosWeaponLoadout.Random;

    [SerializeField]
    protected DebugLaunchedWeaponLoadout _launchedWeaponsPerElement = DebugLaunchedWeaponLoadout.Random;

    [SerializeField]
    protected DebugActiveCMLoadout _activeCMsPerElement = DebugActiveCMLoadout.One;

    [SerializeField]
    protected DebugShieldGenLoadout _shieldGeneratorsPerElement = DebugShieldGenLoadout.One;

    [SerializeField]
    protected DebugPassiveCMLoadout _passiveCMsPerElement = DebugPassiveCMLoadout.One;

    [SerializeField]
    protected DebugSensorLoadout _srSensorsPerElement = DebugSensorLoadout.One;

    [SerializeField]
    protected DebugPassiveCMLoadout _countermeasuresPerCmd = DebugPassiveCMLoadout.One;

    [SerializeField]
    protected DebugSensorLoadout _sensorsPerCmd = DebugSensorLoadout.Random;

    #endregion

    private UnitCreatorConfiguration _configuration;
    public UnitCreatorConfiguration Configuration {
        get { return _configuration; }
        set {
            D.AssertNull(_configuration);   // currently one time only
            SetProperty<UnitCreatorConfiguration>(ref _configuration, value, "Configuration", ConfigurationPropSetHandler);
        }
    }

    public sealed override GameDate DeployDate { get { return Configuration.DeployDate; } }

    protected sealed override Player Owner { get { return Configuration.Owner; } }

    public abstract AUnitCreatorEditorSettings EditorSettings { get; }

    /// <summary>
    /// The date to deploy the unit from this DebugCreator.
    /// <remarks>This value is used to construct an EditorSetting for this DebugCreator. That EditorSetting
    /// is used by the UniverseCreator and UnitConfigurator to create a Configuration for this DebugCreator.
    /// The Configuration may or may not use this DateToDeploy value. It will not use it if this 
    /// creator is used as part of the initial creators reqd on the GameStart date. It will always be used
    /// if DebugControls.UseDebugCreatorsOnly is true.</remarks>
    /// </summary>
    protected GameDate EditorDeployDate {
        get {
            if (_toDelayOperations) {
                return new GameDate(GameTime.GameStartDate, new GameTimeDuration(_hourDelay, _dayDelay, _yearDelay));
            }
            return GameTime.GameStartDate;
        }
    }

    protected bool IsCompositionPreset { get { return _isCompositionPreset; } }

    protected PlayerDesigns _ownerDesigns;

    // 10.12.16 Eliminated overridden InitiateDeployment() which checked ValidateConfiguration() as unconfigured DebugUnitCreators
    // are destroyed by UniverseCreator. It makes no sense for UniverseCreator to call InitiateDeployment on a Creator that
    // hasn't been used and configured.

    #region ExecuteInEditMode

    protected sealed override void Awake() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.Awake();
    }

    protected sealed override void Start() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.Start();
    }

    void Update() {
        if (Application.isPlaying) {
            enabled = false;    // Uses ExecuteInEditMode
        }

        int activeElementCount = GetComponentsInChildren<AUnitElementItem>().Count();
        bool hasActiveElements = activeElementCount > Constants.Zero;
        if (hasActiveElements != (transform.childCount > Constants.Zero)) {
            D.Error("{0} elements not properly configured.", DebugName);
        }
        if (hasActiveElements) {
            _isCompositionPreset = true;
            __AdjustElementQtyFieldTo(activeElementCount);
        }
    }

    protected sealed override void OnDestroy() {
        if (!Application.isPlaying) {
            return; // Uses ExecuteInEditMode
        }
        base.OnDestroy();
    }

    #endregion

    protected sealed override void PrepareUnitForDeployment_Internal() {
        D.AssertNotNull(Configuration);    // would only be called with a Configuration
        D.Log(ShowDebugLog, "{0} is building and positioning {1}. Targeted DeployDate = {2}.", DebugName, UnitName, DeployDate);
        MakeUnit();
    }

    private void MakeUnit() {
        LogEvent();
        MakeElements();
        MakeCommand();
        AddElementsToCommand();
        AssignHQElement();
        PositionUnit();
        HandleUnitPositioned();
    }

    protected abstract void MakeElements();

    protected abstract void MakeCommand();

    protected abstract void AddElementsToCommand();

    protected abstract void AssignHQElement();

    protected abstract void PositionUnit();

    /// <summary>
    /// Hook for derived classes once the Unit is made and positioned but not yet operational.
    /// </summary>
    protected virtual void HandleUnitPositioned() {
        LogEvent();
    }

    /// <summary>
    /// Adjusts the serialized element qty field in the editor to the provided value.
    /// <remarks>Makes the number of elements in a Preset Composition Unit visible in the editor.</remarks>
    /// </summary>
    /// <param name="qty">The qty.</param>
    protected abstract void __AdjustElementQtyFieldTo(int qty);

    #region Event and Property Change Handlers

    private void ConfigurationPropSetHandler() {
        HandleConfigurationPropSet();
    }

    #endregion

    private void HandleConfigurationPropSet() {
        _ownerDesigns = _gameMgr.GetAIManagerFor(Owner).Designs;
    }

    #region Archive

    //#region Serialized Editor fields

    //[SerializeField]
    //protected bool _isOwnerUser;

    //[SerializeField]
    //protected DebugDiploUserRelations _ownerRelationshipWithUser;

    //[SerializeField]
    //private bool _isCompositionPreset;

    //[SerializeField]
    //private bool _toDelayOperations;

    //[Range(0, 19)]
    //[SerializeField]
    //private int _hourDelay = 0;

    //[Range(0, 99)]
    //[SerializeField]
    //private int _dayDelay = 0;

    //[Range(0, 10)]
    //[SerializeField]
    //private int _yearDelay = 0;

    //[SerializeField]
    //protected DebugLosWeaponLoadout _losWeaponsPerElement = DebugLosWeaponLoadout.Random;

    //[SerializeField]
    //protected DebugLaunchedWeaponLoadout _launchedWeaponsPerElement = DebugLaunchedWeaponLoadout.Random;

    //[SerializeField]
    //protected DebugActiveCMLoadout _activeCMsPerElement = DebugActiveCMLoadout.One;

    //[SerializeField]
    //protected DebugShieldGenLoadout _shieldGeneratorsPerElement = DebugShieldGenLoadout.One;

    //[SerializeField]
    //protected DebugPassiveCMLoadout _passiveCMsPerElement = DebugPassiveCMLoadout.One;

    //[SerializeField]
    //protected DebugSensorLoadout _srSensorsPerElement = DebugSensorLoadout.One;

    //[SerializeField]
    //protected DebugPassiveCMLoadout _countermeasuresPerCmd = DebugPassiveCMLoadout.One;

    //[SerializeField]
    //protected DebugSensorLoadout _sensorsPerCmd = DebugSensorLoadout.Random;

    //#endregion

    //public abstract AUnitCreatorEditorSettings EditorSettings { get; }

    ///// <summary>
    ///// The date to deploy the unit from this DebugCreator.
    ///// <remarks>This value is used to construct an EditorSetting for this DebugCreator. That EditorSetting
    ///// is used by the UniverseCreator and UnitConfigurator to create a Configuration for this DebugCreator.
    ///// The Configuration may or may not use this DateToDeploy value. It will not use it if this 
    ///// creator is used as part of the initial creators reqd on the GameStart date. It will always be used
    ///// if DebugControls.UseDebugCreatorsOnly is true.</remarks>
    ///// </summary>
    //protected GameDate DateToDeploy {
    //    get {
    //        if (_toDelayOperations) {
    //            return new GameDate(GameTime.GameStartDate, new GameTimeDuration(_hourDelay, _dayDelay, _yearDelay));
    //        }
    //        return GameTime.GameStartDate;
    //    }
    //}

    //protected bool IsCompositionPreset { get { return _isCompositionPreset; } }

    //#region ExecuteInEditMode

    //protected sealed override void Awake() {
    //    if (!Application.isPlaying) {
    //        return; // Uses ExecuteInEditMode
    //    }
    //    base.Awake();
    //}

    //protected sealed override void Start() {
    //    if (!Application.isPlaying) {
    //        return; // Uses ExecuteInEditMode
    //    }
    //    base.Start();
    //}

    //void Update() {
    //    if (Application.isPlaying) {
    //        enabled = false;    // Uses ExecuteInEditMode
    //    }

    //    int activeElementCount = GetComponentsInChildren<AUnitElementItem>().Count();
    //    bool hasActiveElements = activeElementCount > Constants.Zero;
    //    if (hasActiveElements != (transform.childCount > Constants.Zero)) {
    //        D.Error("{0} elements not properly configured.", DebugName);
    //    }
    //    if (hasActiveElements) {
    //        _isCompositionPreset = true;
    //        __AdjustElementQtyFieldTo(activeElementCount);
    //    }
    //}

    //protected sealed override void OnDestroy() {
    //    if (!Application.isPlaying) {
    //        return; // Uses ExecuteInEditMode
    //    }
    //    base.OnDestroy();
    //}

    //#endregion

    ///// <summary>
    ///// Adjusts the serialized element qty field in the editor to the provided value.
    ///// <remarks>Makes the number of elements in a Preset Composition Unit visible in the editor.</remarks>
    ///// </summary>
    ///// <param name="qty">The qty.</param>
    //protected abstract void __AdjustElementQtyFieldTo(int qty);

    #endregion

}

