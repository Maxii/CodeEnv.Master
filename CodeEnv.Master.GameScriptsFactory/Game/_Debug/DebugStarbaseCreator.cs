// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugStarbaseCreator.cs
// Unit Creator that builds and deploys an editor-configured Starbase at its current location in the scene.
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
/// Unit Creator that builds and deploys an editor-configured Starbase at its current location in the scene.
/// </summary>
public class DebugStarbaseCreator : ADebugUnitCreator {

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
                    _editorSettings = new BaseCreatorEditorSettings(_isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd,
                    _sensorsPerCmd, _activeCMsPerElement, EditorDeployDate, _losWeaponsPerElement, _launchedWeaponsPerElement,
                    _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement, presetHullCats);
                }
                else {
                    _editorSettings = new BaseCreatorEditorSettings(_isOwnerUser, _elementQty, _ownerRelationshipWithUser,
                    _countermeasuresPerCmd, _sensorsPerCmd, _activeCMsPerElement, EditorDeployDate, _losWeaponsPerElement,
                    _launchedWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement);
                }
            }
            return _editorSettings;
        }
    }

    private StarbaseCmdItem _command;
    private IList<FacilityItem> _elements;

    protected override string InitializeRootUnitName() {
        return !transform.name.IsNullOrEmpty() ? transform.name : "DebugStarbase";
    }

    protected override void MakeElements() {
        _elements = new List<FacilityItem>();

        if (IsCompositionPreset) {
            IList<FacilityDesign> designs = new List<FacilityDesign>();
            foreach (var designName in Configuration.ElementDesignNames) {
                FacilityDesign design = _ownerDesigns.__GetFacilityDesign(designName);
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

                string name = _factory.__GetUniqueFacilityName(design.DesignName);
                _factory.PopulateInstance(Owner, Topography.OpenSpace, design, name, ref element);

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
                FacilityDesign design = _ownerDesigns.__GetFacilityDesign(designName);
                string name = _factory.__GetUniqueFacilityName(design.DesignName);
                _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.OpenSpace, design, name, gameObject));
            }
        }
    }

    protected override void MakeCommand() {
        if (IsCompositionPreset) {
            _command = gameObject.GetSingleComponentInChildren<StarbaseCmdItem>();
            _factory.PopulateInstance(Owner, Configuration.CmdModDesignName, ref _command, UnitName, _formation.Convert());
        }
        else {
            _command = _factory.MakeStarbaseCmdInstance(Owner, Configuration.CmdModDesignName, gameObject, UnitName, _formation.Convert());
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
        // Starbases don't need to be deployed. They are already on location
    }

    protected override void HandleUnitPositioned() {
        base.HandleUnitPositioned();
        // 7.5.18 MyNGPathfindingGraph addition now handled by MyNGPathfindingGraph
        SectorGrid.Instance.GetSector(SectorID).Add(_command);
    }

    protected override void CompleteUnitInitialization() {
        LogEvent();
        PopulateCmdWithColonists();
        _elements.ForAll(e => e.FinalInitialize());
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

    protected override bool BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
        return true;
    }

    protected override void __AdjustElementQtyFieldTo(int qty) {
        _elementQty = qty;
    }

    protected override void ClearElementReferences() {
        _elements.Clear();
    }

    private void PopulateCmdWithColonists() {   // TODO Starbases should probably be built and populated with some other ship
        Level currentColonyShipLevel = _ownerDesigns.GetCurrentShipTemplateDesign(ShipHullCategory.Colonizer).HullStat.Level;
        _command.Data.Population = currentColonyShipLevel.GetInitialColonistPopulation();
    }

    #region Debug

    protected override void __ValidateNotDuplicateDeployment() {
        D.AssertNull(_command);
    }

    #endregion

    #region Archive

    //#region Serialized Editor fields

    //[Range(1, TempGameValues.MaxFacilitiesPerBase)]
    //[SerializeField]
    //private int _elementQty = 8;

    //[SerializeField]
    //private DebugBaseFormation _formation = DebugBaseFormation.Random;

    //#endregion

    //private BaseCreatorEditorSettings _editorSettings;
    //public override AUnitCreatorEditorSettings EditorSettings {
    //    get {
    //        if (_editorSettings == null) {
    //            if (IsCompositionPreset) {
    //                var presetHullCats = gameObject.GetSafeComponentsInChildren<FacilityHull>().Select(hull => hull.HullCategory).ToList();
    //                _editorSettings = new BaseCreatorEditorSettings(_isOwnerUser, _ownerRelationshipWithUser, _countermeasuresPerCmd,
    //                _sensorsPerCmd, _activeCMsPerElement, DateToDeploy, _losWeaponsPerElement, _launchedWeaponsPerElement,
    //                _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement, presetHullCats);
    //            }
    //            else {
    //                _editorSettings = new BaseCreatorEditorSettings(_isOwnerUser, _elementQty, _ownerRelationshipWithUser,
    //                _countermeasuresPerCmd, _sensorsPerCmd, _activeCMsPerElement, DateToDeploy, _losWeaponsPerElement,
    //                _launchedWeaponsPerElement, _passiveCMsPerElement, _shieldGeneratorsPerElement, _srSensorsPerElement);
    //            }
    //        }
    //        return _editorSettings;
    //    }
    //}

    //private StarbaseCmdItem _command;
    //private IList<FacilityItem> _elements;

    //protected override void InitializeRootUnitName() {
    //    RootUnitName = !transform.name.IsNullOrEmpty() ? transform.name : "DebugStarbase";
    //}

    //protected override void MakeElements() {
    //    _elements = new List<FacilityItem>();

    //    if (IsCompositionPreset) {
    //        IList<FacilityDesign> designs = new List<FacilityDesign>();
    //        foreach (var designName in Configuration.ElementDesignNames) {
    //            FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
    //            designs.Add(design);
    //        }

    //        IList<FacilityHullCategory> designHullCategories = designs.Select(d => d.HullCategory).ToList();
    //        IList<FacilityItem> existingElements = gameObject.GetSafeComponentsInChildren<FacilityItem>();
    //        foreach (var hullCat in designHullCategories) {
    //            var categoryElements = existingElements.Where(e => e.gameObject.GetSingleComponentInChildren<FacilityHull>().HullCategory == hullCat);
    //            var categoryElementsStillAvailable = categoryElements.Except(_elements);
    //            FacilityItem element = categoryElementsStillAvailable.First();

    //            var design = designs.First(d => d.HullCategory == hullCat);
    //            designs.Remove(design);

    //            string name = _factory.__GetUniqueFacilityName(design.DesignName);
    //            _factory.PopulateInstance(Owner, Topography.OpenSpace, design, name, ref element);

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
    //            FacilityDesign design = _gameMgr.PlayersDesigns.GetFacilityDesign(Owner, designName);
    //            string name = _factory.__GetUniqueFacilityName(design.DesignName);
    //            _elements.Add(_factory.MakeFacilityInstance(Owner, Topography.OpenSpace, design, name, gameObject));
    //        }
    //    }
    //}

    //protected override void MakeCommand(Player owner) {
    //    CmdCameraStat cameraStat = MakeCmdCameraStat(TempGameValues.MaxFacilityRadius);
    //    if (IsCompositionPreset) {
    //        _command = gameObject.GetSingleComponentInChildren<StarbaseCmdItem>();
    //        _factory.PopulateInstance(owner, cameraStat, Configuration.CmdDesignName, ref _command, UnitName, _formation.Convert());
    //    }
    //    else {
    //        _command = _factory.MakeStarbaseCmdInstance(owner, cameraStat, Configuration.CmdDesignName, gameObject, UnitName, _formation.Convert());
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
    //    // Starbases don't need to be deployed. They are already on location
    //    PathfindingManager.Instance.Graph.AddToGraph(_command, SectorID);
    //}

    //protected override void CompleteUnitInitialization() {
    //    LogEvent();
    //    _elements.ForAll(e => e.FinalInitialize());
    //    _command.FinalInitialize();
    //}

    //protected override void AddUnitToGameKnowledge() {
    //    LogEvent();
    //    //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
    //    _gameMgr.GameKnowledge.AddUnit(_command, _elements.Cast<IUnitElement>());
    //}

    //protected override void BeginElementsOperations() {
    //    LogEvent();
    //    _elements.ForAll(e => e.CommenceOperations(isInitialConstructionNeeded: false));
    //}

    //protected override bool BeginCommandOperations() {
    //    LogEvent();
    //    _command.CommenceOperations();
    //    return true;
    //}

    //private CmdCameraStat MakeCmdCameraStat(float maxElementRadius) {
    //    float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
    //    float optViewDistanceAdder = Constants.ZeroF;
    //    return new CmdCameraStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    //}

    //[Obsolete("Moved to UnitFactory")]
    //private FollowableItemCameraStat MakeElementCameraStat(FacilityHullStat hullStat) {
    //    FacilityHullCategory hullCat = hullStat.HullCategory;
    //    float fov;
    //    switch (hullCat) {
    //        case FacilityHullCategory.CentralHub:
    //        case FacilityHullCategory.Defense:
    //            fov = 70F;
    //            break;
    //        case FacilityHullCategory.Economic:
    //        case FacilityHullCategory.Factory:
    //        case FacilityHullCategory.Laboratory:
    //        case FacilityHullCategory.ColonyHab:
    //        case FacilityHullCategory.Barracks:
    //            fov = 60F;
    //            break;
    //        case FacilityHullCategory.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
    //    }
    //    float radius = hullStat.HullDimensions.magnitude / 2F;
    //    //D.Log(ShowDebugLog, "Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
    //    float minViewDistance = radius * 2F;
    //    float optViewDistance = radius * 3F;
    //    return new FollowableItemCameraStat(minViewDistance, optViewDistance, fov);
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

