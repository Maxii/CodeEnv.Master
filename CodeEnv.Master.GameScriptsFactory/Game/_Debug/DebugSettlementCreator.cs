// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugSettlementCreator.cs
// Unit Creator that builds and deploys an editor-configured Settlement in a randomly selected system in the scene.
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
/// Unit Creator that builds and deploys an editor-configured Settlement in a randomly selected system in the scene.
/// </summary>
public class DebugSettlementCreator : ADebugUnitCreator {

    #region Serialized Editor fields

    [Range(1, TempGameValues.MaxFacilitiesPerBase)]
    [SerializeField]
    private int _elementQty = 8;

    [SerializeField]
    private DebugBaseFormation _formation = DebugBaseFormation.Random;

    #endregion

    private BaseCreatorEditorSettings _editorSettings;
    public override AUnitCreatorEditorSettings EditorSettings {
        get {
            if (_editorSettings == null) {
                if (IsCompositionPreset) {
                    var presetHullCats = gameObject.GetSafeComponentsInChildren<FacilityHull>().Select(hull => hull.HullCategory).ToList();
                    _editorSettings = new BaseCreatorEditorSettings(UnitName, _isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd, _activeCMsPerElement, DateToDeploy,
                        _losWeaponsPerElement, _missileWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _sensorsPerElement,
                        _formation, presetHullCats);
                }
                else {
                    _editorSettings = new BaseCreatorEditorSettings(UnitName, _isOwnerUser, _elementQty, _ownerRelationshipWithUser, _countermeasuresPerCmd, _activeCMsPerElement,
                        DateToDeploy, _losWeaponsPerElement, _missileWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _sensorsPerElement,
                        _formation);
                }
            }
            return _editorSettings;
        }
    }

    private SettlementCmdItem _command;
    private IList<FacilityItem> _elements;

    protected override void MakeElements() {
        _elements = new List<FacilityItem>();

        if (IsCompositionPreset) {
            IList<FacilityDesign> designs = new List<FacilityDesign>();
            foreach (var designName in Configuration.ElementDesignNames) {
                FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
                designs.Add(design);
            }

            IList<FacilityHullCategory> designHullCategories = designs.Select(d => d.HullCategory).ToList();
            IList<FacilityItem> existingElements = gameObject.GetSafeComponentsInChildren<FacilityItem>();
            foreach (var hullCat in designHullCategories) {
                var categoryElements = existingElements.Where(e => e.gameObject.GetSingleComponentInChildren<FacilityHull>().HullCategory == hullCat);
                var categoryElementsStillAvailable = categoryElements.Except(_elements);
                FacilityItem element = categoryElementsStillAvailable.First();

                var design = designs.First(d => d.HullCategory == hullCat);
                designs.Remove(design);

                var cameraStat = MakeElementCameraStat(design.HullStat);
                _factory.PopulateInstance(Owner, Topography.System, cameraStat, design, ref element);

                // Note: Need to tell each element where this creator is located. This assures that whichever element is picked as the HQElement
                // will start with this position. However, the elements here are all placed on top of each other. When the physics engine starts
                // rigidbodies that are not kinematic are imparted with both linear and angular velocity from this intentional collision. 
                // This occurs before the elements are moved away from each other by being formed into a formation. 
                // Accordingly, all element rigidbodies start as kinematic, then I change ships to non-kinematic during CommenceOperations.
                element.transform.position = transform.position;
                _elements.Add(element);
            }
            D.AssertEqual(Constants.Zero, designs.Count);
        }
        else {
            foreach (var designName in Configuration.ElementDesignNames) {
                FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
                FollowableItemCameraStat cameraStat = MakeElementCameraStat(design.HullStat);
                _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.System, cameraStat, design, gameObject));
            }
        }
    }

    protected override void MakeCommand(Player owner) {
        CmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.FacilityMaxRadius);
        if (IsCompositionPreset) {
            _command = gameObject.GetSingleComponentInChildren<SettlementCmdItem>();
            _factory.PopulateInstance(owner, cameraStat, Configuration.CmdDesignName, ref _command);
        }
        else {
            _command = _factory.MakeSettlementCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
        }
    }

    protected override void AddElementsToCommand() {
        LogEvent();
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    protected override void AssignHQElement() {
        LogEvent();
        _command.HQElement = _command.SelectHQElement();
    }

    protected override bool DeployUnit() {
        LogEvent(); // 10.6.16 Selection of system to deploy to moved to UniverseCreator
        var system = gameObject.GetSingleComponentInParents<SystemItem>();
        system.Settlement = _command;
        return true;
    }

    protected override void CompleteUnitInitialization() {
        LogEvent();
        _elements.ForAll(e => e.FinalInitialize());
        _command.FinalInitialize();
    }

    protected override void AddUnitToGameKnowledge() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
        _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    }

    protected override void AddUnitToOwnerAndAllysKnowledge() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is adding Unit {1} to {2}'s Knowledge.", DebugName, UnitName, Owner);
        var ownerAIMgr = _gameMgr.GetAIManagerFor(Owner);
        _elements.ForAll(e => ownerAIMgr.HandleGainedItemOwnership(e));
        ownerAIMgr.HandleGainedItemOwnership(_command);    // OPTIMIZE not really needed as this happens automatically when elements handled

        var alliedPlayers = Owner.GetOtherPlayersWithRelationship(DiplomaticRelationship.Alliance);
        if (alliedPlayers.Any()) {
            alliedPlayers.ForAll(ally => {
                //D.Log("{0} is adding Unit {1} to {2}'s Knowledge as Ally.", DebugName, UnitName, ally);
                var allyAIMgr = _gameMgr.GetAIManagerFor(ally);
                _elements.ForAll(e => allyAIMgr.HandleChgdItemOwnerIsAlly(e));
                allyAIMgr.HandleChgdItemOwnerIsAlly(_command);  // OPTIMIZE not really needed as this happens automatically when elements handled
            });
        }
    }

    protected override void RegisterCommandForOrders() {
        var ownerAIMgr = _gameMgr.GetAIManagerFor(Owner);
        ownerAIMgr.RegisterForOrders(_command);
    }

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations());
    }

    protected override void BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
    }

    [Obsolete]
    protected override void __IssueFirstUnitOrder(Action onCompleted) {
        LogEvent();
        onCompleted();
    }

    private CmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
        float optViewDistanceAdder = Constants.ZeroF;
        return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    private FollowableItemCameraStat MakeElementCameraStat(FacilityHullStat hullStat) {
        FacilityHullCategory hullCat = hullStat.HullCategory;
        float fov;
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Defense:
                fov = 70F;
                break;
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Laboratory:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Barracks:
                fov = 60F;
                break;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = hullStat.HullDimensions.magnitude / 2F;
        //D.Log(ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov);
    }

    protected override void __AdjustElementQtyFieldTo(int qty) {
        _elementQty = qty;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

