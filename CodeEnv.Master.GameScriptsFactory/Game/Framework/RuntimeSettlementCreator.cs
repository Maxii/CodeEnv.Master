// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RuntimeSettlementCreator.cs
// ARuntimeUnitCreator that immediately deploys and commences operations of a settlement composed of one
// CentralHub facility under initial construction.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ARuntimeUnitCreator that immediately deploys and commences operations of a settlement composed of one
/// CentralHub facility under initial construction.
/// </summary>
public class RuntimeSettlementCreator : ARuntimeUnitCreator {

    public ShipItem ColonyShip { get; set; }

    public SettlementCmdModuleDesign CmdModDesign { get; set; }

    public FacilityDesign CentralHubDesign { get; set; }

    protected override Player Owner { get { return CmdModDesign.Player; } }

    private SettlementCmdItem _command;

    private FacilityItem _initialFacilityToConstruct;

    protected override string InitializeRootUnitName() {
        return "RuntimeSettlement";
    }

    protected override void MakeCommand() {
        __ValidateColonyShipState();
        Formation randomFormation = RandomExtended.Choice(TempGameValues.AcceptableBaseFormations);
        _command = _factory.MakeSettlementCmdInstance(Owner, CmdModDesign, gameObject, UnitName, randomFormation);
    }

    protected override void AddElementsToCommand() {
        _initialFacilityToConstruct = MakeCentralHubFacility();
        _command.AddElement(_initialFacilityToConstruct);
    }

    private FacilityItem MakeCentralHubFacility() {
        D.AssertNotNull(CentralHubDesign);
        return _factory.MakeFacilityInstance(Owner, Topography.System, CentralHubDesign, "InitialCentralHub", gameObject);
    }

    protected override void AssignHQElement() {
        _command.HQElement = _command.SelectHQElement();
        D.Assert(_initialFacilityToConstruct.IsHQ);
    }

    protected override void PositionUnit() {
        // RuntimeSettlementCreator is already orbiting the System in the proper orbit slot
        SystemItem parentSystem = gameObject.GetSingleComponentInParents<SystemItem>();
        parentSystem.Settlement = _command;
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
        D.LogBold("{0} has been founded in {1}! Frame = {2}.", DebugName, _command.ParentSystem.DebugName, Time.frameCount);
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

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateColonyShipState() {
        D.AssertNotNull(ColonyShip);
        D.AssertEqual(ShipHullCategory.Colonizer, ColonyShip.HullCategory);
        D.AssertEqual(Owner, ColonyShip.Owner);
        D.AssertNotNull(ColonyShip.gameObject); // not yet destroyed
        // ColonyShip will kill itself once this creator deploys the Unit
    }

    #endregion

}

