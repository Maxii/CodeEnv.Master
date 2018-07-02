// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RuntimeStarbaseCreator.cs
// ARuntimeUnitCreator that immediately deploys and commences operations of a starbase composed of one
// CentralHub facility under initial construction.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// ARuntimeUnitCreator that immediately deploys and commences operations of a starbase composed of one
/// CentralHub facility under initial construction.
/// </summary>
public class RuntimeStarbaseCreator : ARuntimeUnitCreator {

    public ShipItem ColonyShip { get; set; }

    public StarbaseCmdModuleDesign CmdModDesign { get; set; }

    public FacilityDesign CentralHubDesign { get; set; }

    protected override Player Owner { get { return ColonyShip.Owner; } }

    private StarbaseCmdItem _command;

    private FacilityItem _initialFacilityToConstruct;

    protected override string InitializeRootUnitName() {
        return "RuntimeStarbase";
    }

    protected override void MakeCommand() {
        __ValidateColonyShipState();
        _command = _factory.MakeStarbaseCmdInstance(Owner, CmdModDesign, gameObject, UnitName);
    }

    protected override void AddElementsToCommand() {
        _initialFacilityToConstruct = MakeCentralHubFacility();
        _command.AddElement(_initialFacilityToConstruct);
    }

    private FacilityItem MakeCentralHubFacility() {
        D.AssertNotNull(CentralHubDesign);
        Topography topography = _gameMgr.GameKnowledge.GetSpaceTopography(transform.position);
        if (topography != Topography.OpenSpace) {
            D.Warn("FYI. {0} is being deployed in {1}!", DebugName, topography.GetValueName());
        }
        return _factory.MakeFacilityInstance(Owner, topography, CentralHubDesign, "InitialCentralHub", gameObject);
    }

    protected override void AssignHQElement() {
        _command.HQElement = _command.SelectHQElement();
        D.Assert(_initialFacilityToConstruct.IsHQ);
    }

    protected override void PositionUnit() {
        // RuntimeStarbaseCreator is already on its station
    }

    protected override void HandleUnitPositioned() {
        base.HandleUnitPositioned();
        PathfindingManager.Instance.Graph.AddToGraph(_command, SectorID);
        var sector = SectorGrid.Instance.GetSector(SectorID);
        sector.Add(_command);
    }

    protected override void CompleteUnitInitialization() {
        PopulateCmdWithColonists();
        _initialFacilityToConstruct.FinalInitialize();
        _command.FinalInitialize();
    }

    protected override void AddUnitToGameKnowledge() {
        AllKnowledge.Instance.AddUnit(_command, new FacilityItem[] { _initialFacilityToConstruct });
    }

    protected override void BeginElementsOperations() {
        _initialFacilityToConstruct.CommenceOperations();
    }

    protected override bool BeginCommandOperations() {
        __ValidateColonyShipState();    // Make sure ColonyShip not taken over during time it takes to deploy
        _command.CommenceOperations();

        _command.ConstructionMgr.AddToQueue(CentralHubDesign, _initialFacilityToConstruct);

        OrderSource source = Owner.IsUser ? OrderSource.User : OrderSource.PlayerAI;
        FacilityOrder initialConstructionOrder = new FacilityOrder(FacilityDirective.Construct, source);
        _initialFacilityToConstruct.CurrentOrder = initialConstructionOrder;
        D.LogBold("{0} has been founded in {1}! Frame = {2}.", DebugName, SectorGrid.Instance.GetSector(SectorID).DebugName, Time.frameCount);
        return true;
    }

    protected override void ClearElementReferences() {
        _initialFacilityToConstruct = null;
        ColonyShip = null;
    }

    private void PopulateCmdWithColonists() {
        Level colonyShipLevel = ColonyShip.Data.Design.HullStat.Level;
        _command.Data.Population = colonyShipLevel.GetInitialColonistPopulation();
    }

    #region Debug

    private void __ValidateColonyShipState() {
        D.AssertNotNull(ColonyShip);
        D.AssertEqual(ShipHullCategory.Colonizer, ColonyShip.HullCategory);
        D.AssertEqual(Owner, ColonyShip.Owner);
        D.AssertNotNull(ColonyShip.gameObject); // not yet destroyed
        // ColonyShip will kill itself once this creator deploys the Unit
    }

    #endregion

}

