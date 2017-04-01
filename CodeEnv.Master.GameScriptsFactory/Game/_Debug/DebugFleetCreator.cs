// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugFleetCreator.cs
// Unit Creator that builds and deploys an editor-configured fleet at its current location in the scene.
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
using MoreLinq;

/// <summary>
/// Unit Creator that builds and deploys an editor-configured fleet at its current location in the scene.
/// </summary>
public class DebugFleetCreator : ADebugUnitCreator, IDebugFleetCreator {

    #region Serialized Editor fields

    [Range(1, TempGameValues.MaxShipsPerFleet)]
    [SerializeField]
    private int _elementQty = 8;

    [SerializeField]
    private DebugFleetFormation _formation = DebugFleetFormation.Random;

    /// <summary>
    /// Indicates whether this Fleet should move to a destination.
    /// </summary>
    [SerializeField]
    private bool _move = false;

    /// <summary>
    /// If fleet is to move to a destination, should it pick the farthest or the closest?
    /// </summary>
    [SerializeField]
    private bool _findFarthest = false;

    /// <summary>
    /// The fleet is to move to a destination, should it attack it?
    /// </summary>
    [SerializeField]
    private bool _attack = false;

    /// <summary>
    /// Indicates whether the FTL drive of all the ships in the fleet should start damaged, aka not operational.
    /// They can still repair themselves.
    /// </summary>
    [SerializeField]
    private bool _ftlStartsDamaged = false;

    /// <summary>
    /// The exclusions when randomly picking ShipCombatStances.
    /// </summary>
    [Tooltip("Controls available Combat Stances that can be assigned. Note: HQ will always be assigned Defensive")]
    [SerializeField]
    private DebugShipCombatStanceExclusions _stanceExclusions = default(DebugShipCombatStanceExclusions);

    #endregion

    private FleetCreatorEditorSettings _editorSettings;
    public override AUnitCreatorEditorSettings EditorSettings {
        get {
            if (_editorSettings == null) {
                if (IsCompositionPreset) {
                    var presetHullCats = gameObject.GetSafeComponentsInChildren<ShipHull>().Select(hull => hull.HullCategory).ToList();
                    _editorSettings = new FleetCreatorEditorSettings(UnitName, _isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd,
                        _sensorsPerCmd, _activeCMsPerElement, DateToDeploy, _losWeaponsPerElement, _missileWeaponsPerElement,
                        _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement, _formation, _move, _findFarthest,
                        _attack, _stanceExclusions, presetHullCats);
                }
                else {
                    _editorSettings = new FleetCreatorEditorSettings(UnitName, _isOwnerUser, _elementQty, _ownerRelationshipWithUser,
                        _countermeasuresPerCmd, _sensorsPerCmd, _activeCMsPerElement, DateToDeploy, _losWeaponsPerElement,
                        _missileWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement,
                        _formation, _move, _findFarthest, _attack, _stanceExclusions);
                }
            }
            return _editorSettings;
        }
    }

    private FleetCmdItem _command;
    private IList<ShipItem> _elements;

    protected override void MakeElements() {
        _elements = new List<ShipItem>();

        if (IsCompositionPreset) {
            IList<ShipDesign> designs = new List<ShipDesign>();
            foreach (var designName in Configuration.ElementDesignNames) {
                ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
                designs.Add(design);
            }

            IList<ShipHullCategory> designHullCategories = designs.Select(d => d.HullCategory).ToList();
            IList<ShipItem> existingElements = gameObject.GetSafeComponentsInChildren<ShipItem>();
            foreach (var hullCat in designHullCategories) {
                var categoryElements = existingElements.Where(e => e.gameObject.GetSingleComponentInChildren<ShipHull>().HullCategory == hullCat);
                var categoryElementsStillAvailable = categoryElements.Except(_elements);
                ShipItem element = categoryElementsStillAvailable.First();

                var design = designs.First(d => d.HullCategory == hullCat);
                designs.Remove(design);

                var cameraStat = MakeElementCameraStat(design.HullStat);
                _factory.PopulateInstance(Owner, cameraStat, design, ref element);

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
                ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
                FollowableItemCameraStat cameraStat = MakeElementCameraStat(design.HullStat);
                _elements.Add(_factory.MakeShipInstance(Owner, cameraStat, design, gameObject));
            }
        }
    }

    protected override void MakeCommand(Player owner) {
        FleetCmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.ShipMaxRadius);
        if (IsCompositionPreset) {
            _command = gameObject.GetSingleComponentInChildren<FleetCmdItem>();
            _factory.PopulateInstance(owner, cameraStat, Configuration.CmdDesignName, ref _command);
        }
        else {
            _command = _factory.MakeFleetCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject);
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

    protected override bool PositionUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
        return true;
    }

    protected override void CompleteUnitInitialization() {
        LogEvent();
        _elements.ForAll(e => {
            e.FinalInitialize();
            __SetFtlDamagedState(e);
        });
        _command.FinalInitialize();
    }

    protected override void AddUnitToGameKnowledge() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
        _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    }

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations());
    }

    protected override void BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
    }

    private void __SetFtlDamagedState(ShipItem element) {
        D.Assert(element.Data.IsFtlCapable);
        element.Data.IsFtlDamaged = _ftlStartsDamaged;
    }

    private FleetCmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F;
        float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
        // there is no optViewDistance value for a FleetCmd CameraStat
        return new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    private FollowableItemCameraStat MakeElementCameraStat(ShipHullStat hullStat) {
        ShipHullCategory hullCat = hullStat.HullCategory;
        float fov;
        switch (hullCat) {
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Troop:
                fov = 70F;
                break;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Investigator:
                fov = 65F;
                break;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                fov = 60F;
                break;
            case ShipHullCategory.Frigate:
                fov = 55F;
                break;
            case ShipHullCategory.Fighter:
            case ShipHullCategory.Scout:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = hullStat.HullDimensions.magnitude / 2F;
        //D.Log(ShowDebugLog, ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        float distanceDampener = 3F;    // default
        float rotationDampener = 10F;   // ships can change direction pretty fast
        return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov, distanceDampener, rotationDampener);
    }

    protected override void __AdjustElementQtyFieldTo(int qty) {
        _elementQty = qty;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Archive

    #endregion


}

