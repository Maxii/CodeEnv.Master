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
    protected bool _isOwnerUser;

    [SerializeField]
    protected DebugDiploUserRelations _ownerRelationshipWithUser;

    [SerializeField]
    private bool _isCompositionPreset;

    [SerializeField]
    private bool _toDelayOperations;

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
    protected DebugLosWeaponLoadout _losWeaponsPerElement;

    [SerializeField]
    protected DebugWeaponLoadout _missileWeaponsPerElement;

    [Range(0, 5)]
    [SerializeField]
    protected int _activeCMsPerElement = 2;

    [Range(0, 5)]
    [SerializeField]
    protected int _shieldGeneratorsPerElement = 2;

    [Range(0, 5)]
    [SerializeField]
    protected int _passiveCMsPerElement = 2;

    [Range(1, 3)]
    [SerializeField]
    protected int _srSensorsPerElement = 1;

    [Range(0, 3)]
    [SerializeField]
    protected int _countermeasuresPerCmd = 2;

    [Range(1, 6)]
    [SerializeField]
    protected int _sensorsPerCmd = 3;

    #endregion

    public abstract AUnitCreatorEditorSettings EditorSettings { get; }

    /// <summary>
    /// The date to deploy the unit from this DebugCreator.
    /// <remarks>This value is used to construct an EditorSetting for this DebugCreator. That EditorSetting
    /// is used by the UniverseCreator and UnitConfigurator to create a Configuration for this DebugCreator.
    /// The Configuration may or may not use this DateToDeploy value. It will not use it if this 
    /// creator is used as part of the initial creators reqd on the GameStart date. It will always be used
    /// if DebugControls.UseDebugCreatorsOnly is true.</remarks>
    /// </summary>
    protected GameDate DateToDeploy {
        get {
            if (_toDelayOperations) {
                return new GameDate(GameTime.GameStartDate, new GameTimeDuration(_hourDelay, _dayDelay, _yearDelay));
            }
            return GameTime.GameStartDate;
        }
    }

    protected bool IsCompositionPreset { get { return _isCompositionPreset; } }

    // 10.12.16 Eliminated overridden InitiateDeployment() which checked ValidateConfiguration() as un-configured DebugUnitCreators
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


    /// <summary>
    /// Adjusts the serialized element qty field in the editor to the provided value.
    /// <remarks>Makes the number of elements in a Preset Composition Unit visible in the editor.</remarks>
    /// </summary>
    /// <param name="qty">The qty.</param>
    protected abstract void __AdjustElementQtyFieldTo(int qty);

    #region Owner selection Archive

    //private Player ValidateAndInitializeOwner() {
    //    Player userPlayer = _gameMgr.UserPlayer;
    //    if (IsOwnedByUser) {
    //        return userPlayer;
    //    }
    //    DiplomaticRelationship desiredUserRelations = DesiredRelationsWithUser;

    //    Player aiOwner = null;
    //    IEnumerable<Player> aiOwnerCandidates;
    //    if (_gameMgr.__TryGetAIPlayersWithUserRelationshipOf(desiredUserRelations, out aiOwnerCandidates)) {
    //        // aiOwner has already been met and has the desired user relationship, 
    //        // or is unmet but has already been assigned the desired relationship for when they meet
    //        aiOwner = aiOwnerCandidates.Shuffle().First();
    //    }
    //    else if (_gameMgr.__TryGetAIPlayersWithNoCurrentOrAssignedUserRelationship(out aiOwnerCandidates)) {
    //        // aiOwner has neither been met or assigned a user relationship for when they meet
    //        aiOwner = aiOwnerCandidates.Shuffle().First();
    //        _gameMgr.GetAIManagerFor(aiOwner).__AssignUserRelations(userPlayer, desiredUserRelations);
    //        _gameMgr.UserAIManager.__AssignUserRelations(aiOwner, desiredUserRelations);
    //    }

    //    if (aiOwner != null) {
    //        D.Log(ShowCmdHQDebugLog, "{0} picked AI Owner {1}. User relationship upon detection will be {2}.", DebugName, aiOwner.LeaderName, desiredUserRelations.GetValueName());
    //    }
    //    return aiOwner;
    //}
    //private Player ValidateAndInitializeOwner() {
    //    Player userPlayer = _gameMgr.UserPlayer;
    //    if (_isOwnerUser) {
    //        return userPlayer;
    //    }
    //    DiplomaticRelationship desiredUserRelations;
    //    switch (_ownerRelationshipWithUser) {
    //        case __DiploStateWithUser.Alliance:
    //            desiredUserRelations = DiplomaticRelationship.Alliance;
    //            break;
    //        case __DiploStateWithUser.Friendly:
    //            desiredUserRelations = DiplomaticRelationship.Friendly;
    //            break;
    //        case __DiploStateWithUser.Neutral:
    //            desiredUserRelations = DiplomaticRelationship.Neutral;
    //            break;
    //        case __DiploStateWithUser.ColdWar:
    //            desiredUserRelations = DiplomaticRelationship.ColdWar;
    //            break;
    //        case __DiploStateWithUser.War:
    //            desiredUserRelations = DiplomaticRelationship.War;
    //            break;
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_ownerRelationshipWithUser));
    //    }

    //    Player aiOwner = null;
    //    IEnumerable<Player> aiOwnerCandidates;
    //    if (_gameMgr.__TryGetAIPlayersWithUserRelationshipOf(desiredUserRelations, out aiOwnerCandidates)) {
    //        // aiOwner has already been met and has the desired user relationship, 
    //        // or is unmet but has already been assigned the desired relationship for when they meet
    //        aiOwner = aiOwnerCandidates.Shuffle().First();
    //    }
    //    else if (_gameMgr.__TryGetAIPlayersWithNoCurrentOrAssignedUserRelationship(out aiOwnerCandidates)) {
    //        // aiOwner has neither been met or assigned a user relationship for when they meet
    //        aiOwner = aiOwnerCandidates.Shuffle().First();
    //        _gameMgr.GetAIManagerFor(aiOwner).__AssignUserRelations(userPlayer, desiredUserRelations);
    //        _gameMgr.UserAIManager.__AssignUserRelations(aiOwner, desiredUserRelations);
    //    }

    //    if (aiOwner != null) {
    //        D.Log(ShowCmdHQDebugLog, "{0} picked AI Owner {1}. User relationship upon detection will be {2}.", DebugName, aiOwner.LeaderName, desiredUserRelations.GetValueName());
    //    }
    //    return aiOwner;
    //}

    #endregion

}

