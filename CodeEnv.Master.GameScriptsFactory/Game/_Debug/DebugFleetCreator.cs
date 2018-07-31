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
                    _editorSettings = new FleetCreatorEditorSettings(_isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd,
                    _sensorsPerCmd, _activeCMsPerElement, EditorDeployDate, _losWeaponsPerElement, _launchedWeaponsPerElement,
                    _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement, _move, _findFarthest,
                    _attack, _stanceExclusions, presetHullCats);
                }
                else {
                    _editorSettings = new FleetCreatorEditorSettings(_isOwnerUser, _elementQty, _ownerRelationshipWithUser,
                    _countermeasuresPerCmd, _sensorsPerCmd, _activeCMsPerElement, EditorDeployDate, _losWeaponsPerElement,
                    _launchedWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement,
                    _move, _findFarthest, _attack, _stanceExclusions);
                }
            }
            return _editorSettings;
        }
    }

    private FleetCmdItem _command;
    private IList<ShipItem> _elements;

    protected override string InitializeRootUnitName() {
        return !transform.name.IsNullOrEmpty() ? transform.name : "DebugFleet";
    }

    protected override void MakeElements() {
        _elements = new List<ShipItem>();

        if (IsCompositionPreset) {
            IList<ShipDesign> designs = new List<ShipDesign>();
            foreach (var designName in Configuration.ElementDesignNames) {
                ShipDesign design = _ownerDesigns.__GetShipDesign(designName);
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

                string name = _factory.__GetUniqueShipName(design.DesignName);
                _factory.PopulateInstance(Owner, design, name, ref element);

                // Note: Need to tell each element where this creator is located. This assures that whichever element is picked as the HQElement
                // will start with this position. However, the elements here are all placed on top of each other. When the physics engine starts
                // rigidbodies that are not kinematic are imparted with both linear and angular velocity from this intentional collision. 
                // This occurs before the elements are moved away from each other by being formed into a formation. 
                // Accordingly, all element rigidbodies start as kinematic, then I change ships to non-kinematic during CommenceOperations.
                element.transform.position = transform.position;
                _elements.Add(element);
            }
            D.AssertEqual(Constants.Zero, designs.Count);

            // deactivate any preset elements that don't yet have a design available due to lack of a HullStat from research
            var existingElementsWithoutDesigns = existingElements.Except(_elements);
            if (existingElementsWithoutDesigns.Any()) {
                // Occurs when Hulls are not yet researched
                D.Log(ShowDebugLog, "{0} is deactivating {1} preset elements without designs: {2}.", DebugName, existingElementsWithoutDesigns.Count(),
                    existingElementsWithoutDesigns.Select(e => e.DebugName).Concatenate());
                existingElementsWithoutDesigns.ForAll(e => e.gameObject.SetActive(false));
            }
        }
        else {
            foreach (var designName in Configuration.ElementDesignNames) {
                ShipDesign design = _ownerDesigns.__GetShipDesign(designName);
                string name = _factory.__GetUniqueShipName(design.DesignName);
                _elements.Add(_factory.MakeShipInstance(Owner, design, name, gameObject));
            }
        }
    }

    protected override void MakeCommand() {
        if (IsCompositionPreset) {
            _command = gameObject.GetSingleComponentInChildren<FleetCmdItem>();
            _factory.PopulateInstance(Owner, Configuration.CmdModDesignName, ref _command, UnitName, _formation.Convert());
        }
        else {
            _command = _factory.MakeFleetCmdInstance(Owner, Configuration.CmdModDesignName, gameObject, UnitName, _formation.Convert());
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

    protected override void PositionUnit() {
        LogEvent();
        // Fleets don't need to be deployed. They are already on location.
    }

    protected override void CompleteUnitInitialization() {
        LogEvent();
        _elements.ForAll(e => {
            e.FinalInitialize();
            SetFtlDamagedState(e);
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
        foreach (var element in _elements) {
            element.CommenceOperations();
        }
    }

    protected override bool BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
        return true;
    }

    private void SetFtlDamagedState(ShipItem element) {
        if (element.IsFtlCapable) {
            element.Data.__SetFtlEngineToStart(_ftlStartsDamaged);
        }
    }

    protected override void __AdjustElementQtyFieldTo(int qty) {
        _elementQty = qty;
    }

    protected override void ClearElementReferences() {
        _elements.Clear();
    }

    #region Debug

    protected override void __ValidateNotDuplicateDeployment() {
        D.AssertNull(_command);
    }

    #endregion

    #region Archive

    //#region Serialized Editor fields

    //[Range(1, TempGameValues.MaxShipsPerFleet)]
    //[SerializeField]
    //private int _elementQty = 8;

    //[SerializeField]
    //private DebugFleetFormation _formation = DebugFleetFormation.Random;

    ///// <summary>
    ///// Indicates whether this Fleet should move to a destination.
    ///// </summary>
    //[SerializeField]
    //private bool _move = false;

    ///// <summary>
    ///// If fleet is to move to a destination, should it pick the farthest or the closest?
    ///// </summary>
    //[SerializeField]
    //private bool _findFarthest = false;

    ///// <summary>
    ///// The fleet is to move to a destination, should it attack it?
    ///// </summary>
    //[SerializeField]
    //private bool _attack = false;

    ///// <summary>
    ///// Indicates whether the FTL drive of all the ships in the fleet should start damaged, aka not operational.
    ///// They can still repair themselves.
    ///// </summary>
    //[SerializeField]
    //private bool _ftlStartsDamaged = false;

    ///// <summary>
    ///// The exclusions when randomly picking ShipCombatStances.
    ///// </summary>
    //[Tooltip("Controls available Combat Stances that can be assigned. Note: HQ will always be assigned Defensive")]
    //[SerializeField]
    //private DebugShipCombatStanceExclusions _stanceExclusions = default(DebugShipCombatStanceExclusions);

    //#endregion

    //private FleetCreatorEditorSettings _editorSettings;
    //public override AUnitCreatorEditorSettings EditorSettings {
    //    get {
    //        if (_editorSettings == null) {
    //            if (IsCompositionPreset) {
    //                var presetHullCats = gameObject.GetSafeComponentsInChildren<ShipHull>().Select(hull => hull.HullCategory).ToList();
    //                _editorSettings = new FleetCreatorEditorSettings(_isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd,
    //                _sensorsPerCmd, _activeCMsPerElement, DateToDeploy, _losWeaponsPerElement, _launchedWeaponsPerElement,
    //                _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement, _move, _findFarthest,
    //                _attack, _stanceExclusions, presetHullCats);
    //            }
    //            else {
    //                _editorSettings = new FleetCreatorEditorSettings(_isOwnerUser, _elementQty, _ownerRelationshipWithUser,
    //                _countermeasuresPerCmd, _sensorsPerCmd, _activeCMsPerElement, DateToDeploy, _losWeaponsPerElement,
    //                _launchedWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement,
    //                _move, _findFarthest, _attack, _stanceExclusions);
    //            }
    //        }
    //        return _editorSettings;
    //    }
    //}

    //private FleetCmdItem _command;
    //private IList<ShipItem> _elements;

    //protected override void InitializeRootUnitName() {
    //    RootUnitName = !transform.name.IsNullOrEmpty() ? transform.name : "DebugFleet";
    //}

    //protected override void MakeElements() {
    //    _elements = new List<ShipItem>();

    //    if (IsCompositionPreset) {
    //        IList<ShipDesign> designs = new List<ShipDesign>();
    //        foreach (var designName in Configuration.ElementDesignNames) {
    //            ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
    //            designs.Add(design);
    //        }

    //        IList<ShipHullCategory> designHullCategories = designs.Select(d => d.HullCategory).ToList();
    //        IList<ShipItem> existingElements = gameObject.GetSafeComponentsInChildren<ShipItem>();
    //        foreach (var hullCat in designHullCategories) {
    //            var categoryElements = existingElements.Where(e => e.gameObject.GetSingleComponentInChildren<ShipHull>().HullCategory == hullCat);
    //            var categoryElementsStillAvailable = categoryElements.Except(_elements);
    //            ShipItem element = categoryElementsStillAvailable.First();

    //            var design = designs.First(d => d.HullCategory == hullCat);
    //            designs.Remove(design);

    //            string name = _factory.__GetUniqueShipName(design.DesignName);
    //            _factory.PopulateInstance(Owner, design, name, ref element);

    //            // Note: Need to tell each element where this creator is located. This assures that whichever element is picked as the HQElement
    //            // will start with this position. However, the elements here are all placed on top of each other. When the physics engine starts
    //            // rigidbodies that are not kinematic are imparted with both linear and angular velocity from this intentional collision. 
    //            // This occurs before the elements are moved away from each other by being formed into a formation. 
    //            // Accordingly, all element rigidbodies start as kinematic, then I change ships to non-kinematic during CommenceOperations.
    //            element.transform.position = transform.position;
    //            _elements.Add(element);
    //        }
    //        D.AssertEqual(Constants.Zero, designs.Count);
    //    }
    //    else {
    //        foreach (var designName in Configuration.ElementDesignNames) {
    //            ShipDesign design = _gameMgr.PlayersDesigns.GetShipDesign(Owner, designName);
    //            string name = _factory.__GetUniqueShipName(design.DesignName);
    //            _elements.Add(_factory.MakeShipInstance(Owner, design, name, gameObject));
    //        }
    //    }
    //}

    //protected override void MakeCommand(Player owner) {
    //    FleetCmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.MaxShipRadius);
    //    if (IsCompositionPreset) {
    //        _command = gameObject.GetSingleComponentInChildren<FleetCmdItem>();
    //        _factory.PopulateInstance(owner, cameraStat, Configuration.CmdDesignName, ref _command, UnitName, _formation.Convert());
    //    }
    //    else {
    //        _command = _factory.MakeFleetCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject, UnitName, _formation.Convert());
    //    }
    //}

    //protected override void AddElementsToCommand() {
    //    LogEvent();
    //    _elements.ForAll(e => _command.AddElement(e));
    //    // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    //}

    //protected override void AssignHQElement() {
    //    LogEvent();
    //    _command.HQElement = _command.SelectHQElement();
    //}

    //protected override void PositionUnit() {
    //    LogEvent();
    //    // Fleets don't need to be deployed. They are already on location.
    //}

    //protected override void CompleteUnitInitialization() {
    //    LogEvent();
    //    _elements.ForAll(e => {
    //        e.FinalInitialize();
    //        __SetFtlDamagedState(e);
    //    });
    //    _command.FinalInitialize();
    //}

    //protected override void AddUnitToGameKnowledge() {
    //    LogEvent();
    //    //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
    //    _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    //}

    //protected override void BeginElementsOperations() {
    //    LogEvent();
    //    foreach (var element in _elements) {
    //        element.CommenceOperations();
    //    }
    //}

    //protected override bool BeginCommandOperations() {
    //    LogEvent();
    //    _command.CommenceOperations();
    //    return true;
    //}

    //private void __SetFtlDamagedState(ShipItem element) {
    //    D.Assert(element.Data.IsFtlCapable);
    //    element.Data.IsFtlDamaged = _ftlStartsDamaged;
    //}

    //private FleetCmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
    //    float minViewDistance = maxElementRadius + 1F;
    //    float optViewDistanceAdder = 1F;    // the additional distance outside of the UnitRadius of the fleet
    //    // there is no optViewDistance value for a FleetCmd CameraStat
    //    return new FleetCmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    //}

    //protected override void __AdjustElementQtyFieldTo(int qty) {
    //    _elementQty = qty;
    //}

    //protected override void ClearElementReferences() {
    //    _elements.Clear();
    //}

    //#region Debug

    //protected override void __ValidateNotDuplicateDeployment() {
    //    D.AssertNull(_command);
    //}

    //#endregion

    #endregion

}

