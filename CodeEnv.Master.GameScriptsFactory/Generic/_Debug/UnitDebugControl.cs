// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitDebugControls.cs
// Debug controls for a Unit. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Debug controls for a Unit. Currently handles changing the owner
/// to one with a chosen User relationship.
/// </summary>
[Obsolete]
public class UnitDebugControl : AMonoBase {

    private const string DebugNameFormat = "{0}.{1}";

    /// <summary>
    /// Returns <c>true</c> if the UnitDebugControl script should be enabled in the editor,
    /// <c>false</c> otherwise.
    /// </summary>
    public bool EnableDebugCntlInEditor { get; private set; }

    private string DebugName { get { return _unitCmd != null ? DebugNameFormat.Inject(_unitCmd.DebugName, GetType().Name) : GetType().Name; } }

    [Tooltip("This Unit's current relationship with the User")]
    [SerializeField]
    private DiplomaticRelationship _currentOwnerUserRelations;

    [Tooltip("Select desired relationship of new Unit owner with User")]
    [SerializeField]
    private NewOwnerUserRelationshipChoices _newOwnerUserRelationsChoice;

#pragma warning disable 0414
    [Tooltip("The Owner's name. For display only.")]
    [SerializeField]
    private string _ownerName = "No owner";
#pragma warning restore

    private bool _allowNewOwnerUserRelationsCheck = false;
    private AUnitCmdItem _unitCmd;
    private GameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = GameManager.Instance;
        EnableDebugCntlInEditor = false;
    }

    public void Initialize() {
        _unitCmd = gameObject.GetSingleComponentInChildren<AUnitCmdItem>();
        SyncUserRelationsFieldsTo(_unitCmd.Owner.UserRelations);
        AssignOwnerName();
        Subscribe();
        EnableDebugCntlInEditor = true;
    }

    private void Subscribe() {
        _unitCmd.ownerChanging += UnitOwnerChangingEventHandler;
        _unitCmd.ownerChanged += UnitOwnerChangedEventHandler;
        _unitCmd.Owner.relationsChanged += UnitOwnerRelationsChangedEventHandler;
    }

    #region Event and Property Change Handlers

    private void UnitOwnerChangingEventHandler(object sender, OwnerChangingEventArgs e) {
        _unitCmd.Owner.relationsChanged -= UnitOwnerRelationsChangedEventHandler;
    }

    private void UnitOwnerChangedEventHandler(object sender, EventArgs e) {
        SyncUserRelationsFieldsTo(_unitCmd.Owner.UserRelations);
        _unitCmd.Owner.relationsChanged += UnitOwnerRelationsChangedEventHandler;
        AssignOwnerName();
    }

    private void UnitOwnerRelationsChangedEventHandler(object sender, RelationsChangedEventArgs e) {
        D.AssertEqual(_unitCmd.Owner, sender as Player);
        Player playerWhoseRelationsWithUnitCmdOwnerChgd = e.ChgdRelationsPlayer;
        if (playerWhoseRelationsWithUnitCmdOwnerChgd.IsUser) {
            SyncUserRelationsFieldsTo(_unitCmd.Owner.UserRelations);
        }
    }

    #endregion

    private void SyncUserRelationsFieldsTo(DiplomaticRelationship ownerUserRelationship) {
        _allowNewOwnerUserRelationsCheck = false;
        _currentOwnerUserRelations = ownerUserRelationship;
        _newOwnerUserRelationsChoice = Convert(ownerUserRelationship);
        _allowNewOwnerUserRelationsCheck = true;
    }

    private void AssignOwnerName() {
        _ownerName = _unitCmd.Owner.DebugName;
    }

    #region Value Change Checking

    void OnValidate() {
        //D.Log("{0}.OnValidate() called.", DebugName);
        CheckValuesForChange();
    }

    private void CheckValuesForChange() {
        if (_allowNewOwnerUserRelationsCheck) {
            CheckNewOwnerUserRelations();   // test above only allows this check when the user changes a relationship during runtime
        }
    }

    private NewOwnerUserRelationshipChoices? _priorNewOwnerUserRelationsChoice = null;  // can't use None without showing it as a choice

    private void CheckNewOwnerUserRelations() {
        if (_newOwnerUserRelationsChoice != _priorNewOwnerUserRelationsChoice) {
            // a new desired owner relationship has been selected

            Player newOwner;
            if (_newOwnerUserRelationsChoice == NewOwnerUserRelationshipChoices.BecomeUser) {
                newOwner = _gameMgr.UserPlayer;
            }
            else {
                DiplomaticRelationship newOwnerUserRelationshipChoice = Convert(_newOwnerUserRelationsChoice);

                IEnumerable<Player> newOwnerCandidates = _gameMgr.UserPlayer.OtherKnownPlayers.Where(aiPlayer => aiPlayer.IsRelationshipWith(_gameMgr.UserPlayer, newOwnerUserRelationshipChoice));
                if (newOwnerCandidates.Any()) {
                    newOwner = newOwnerCandidates.Shuffle().First();
                    // newOwner has already been met and has the desired relationship
                }
                else {
                    newOwnerCandidates = _gameMgr.UniverseCreator.__GetUnmetAiPlayersWithInitialUserRelationsOf(newOwnerUserRelationshipChoice);
                    if (newOwnerCandidates.Any()) {
                        // newOwner is an unmet, unassigned player
                        newOwner = newOwnerCandidates.Shuffle().First();
                    }
                    else {
                        D.Warn("{0} has found no players who have or will have the desired user relationship {1}.", DebugName, newOwnerUserRelationshipChoice.GetValueName());
                        SyncUserRelationsFieldsTo(_currentOwnerUserRelations);
                        return;
                    }
                }
            }

            var tempNewOwnerUserRelationsChoice = _newOwnerUserRelationsChoice; // choice can be changed by owner change event
            D.LogBold("{0} has selected {1} as its new owner.", DebugName, newOwner);
            _unitCmd.Data.Owner = newOwner;  // generates an ownerChange event which will sync to current user relationship
            ////_unitCmd.ChangeOwner(newOwner);

            if (_currentOwnerUserRelations == DiplomaticRelationship.None) {
                // correct _newOwnerUserRelationsChoice to what was chosen
                _allowNewOwnerUserRelationsCheck = false;
                _newOwnerUserRelationsChoice = tempNewOwnerUserRelationsChoice;
                _allowNewOwnerUserRelationsCheck = true;
            }
            _priorNewOwnerUserRelationsChoice = _newOwnerUserRelationsChoice;
        }
    }

    #endregion

    private static DiplomaticRelationship Convert(NewOwnerUserRelationshipChoices newOwnerUserRelationship) {
        switch (newOwnerUserRelationship) {
            case NewOwnerUserRelationshipChoices.BecomeUser:
                return DiplomaticRelationship.Self;
            case NewOwnerUserRelationshipChoices.Alliance:
                return DiplomaticRelationship.Alliance;
            case NewOwnerUserRelationshipChoices.Friendly:
                return DiplomaticRelationship.Friendly;
            case NewOwnerUserRelationshipChoices.Neutral:
                return DiplomaticRelationship.Neutral;
            case NewOwnerUserRelationshipChoices.ColdWar:
                return DiplomaticRelationship.ColdWar;
            case NewOwnerUserRelationshipChoices.War:
                return DiplomaticRelationship.War;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(newOwnerUserRelationship));
        }
    }

    private static NewOwnerUserRelationshipChoices Convert(DiplomaticRelationship ownerUserRelationship) {
        switch (ownerUserRelationship) {
            case DiplomaticRelationship.Self:
                return NewOwnerUserRelationshipChoices.BecomeUser;
            case DiplomaticRelationship.Alliance:
                return NewOwnerUserRelationshipChoices.Alliance;
            case DiplomaticRelationship.Friendly:
                return NewOwnerUserRelationshipChoices.Friendly;
            case DiplomaticRelationship.Neutral:
                return NewOwnerUserRelationshipChoices.Neutral;
            case DiplomaticRelationship.ColdWar:
                return NewOwnerUserRelationshipChoices.ColdWar;
            case DiplomaticRelationship.War:
                return NewOwnerUserRelationshipChoices.War;
            case DiplomaticRelationship.None:
                return default(NewOwnerUserRelationshipChoices);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ownerUserRelationship));
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        if (_unitCmd != null) {
            // will be null if not yet built or destroyed
            _unitCmd.ownerChanging -= UnitOwnerChangingEventHandler;
            _unitCmd.ownerChanged -= UnitOwnerChangedEventHandler;
            _unitCmd.Owner.relationsChanged -= UnitOwnerRelationsChangedEventHandler;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Nested Classes

    [Obsolete]
    public enum NewOwnerUserRelationshipChoices {
        BecomeUser,
        Alliance,
        Friendly,
        Neutral,
        ColdWar,
        War
    }

    #endregion

}

