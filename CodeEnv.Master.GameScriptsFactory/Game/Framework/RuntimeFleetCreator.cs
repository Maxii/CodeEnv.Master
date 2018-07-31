// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RuntimeFleetCreator.cs
// ARuntimeUnitCreator that immediately deploys and commences operations of a fleet from the operational ships provided 
// at its current location in the scene.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// ARuntimeUnitCreator that immediately deploys and commences operations of a fleet from the operational ships provided 
/// at its current location in the scene.
/// </summary>
public class RuntimeFleetCreator : ARuntimeUnitCreator {

    public IEnumerable<ShipItem> Elements { get; set; }

    public FleetCmdModuleDesign CmdModDesign { get; set; }

    protected override Player Owner { get { return CmdModDesign.Player; } }

    private FleetCmdItem _command;

    protected override string InitializeRootUnitName() {
        return "RuntimeFleet";
    }

    protected override void MakeCommand() {
        Formation randomFormation = RandomExtended.Choice(TempGameValues.AcceptableFleetFormations);
        _command = _factory.MakeFleetCmdInstance(Owner, CmdModDesign, gameObject, UnitName, randomFormation);
    }

    protected override void AddElementsToCommand() {
        foreach (var element in Elements) {
            _command.AddElement(element);   // Validates owners haven't changed
        }
    }

    protected override void AssignHQElement() {
        _command.HQElement = _command.SelectHQElement();
    }

    protected override void PositionUnit() {
        // Fleets are initially positioned where this creator is located
    }

    protected override void CompleteUnitInitialization() {
        Elements.ForAll(e => D.Assert(e.IsOperational));
        _command.FinalInitialize();
    }

    protected override void AddUnitToGameKnowledge() {
        AllKnowledge.Instance.AddUnit(_command, Enumerable.Empty<IUnitElement>());  // elements added when created in hanger
    }

    protected override void BeginElementsOperations() {
        Elements.ForAll(e => {
            D.Assert(e.IsOperational);
            // 11.19.17 Can't cancel element's superiors orders as Ship.Join order could have caused this fleet's creation
        });
    }

    protected override bool BeginCommandOperations() {
        if (_command.__IsOwnerChgUnderway) {
            D.Warn("FYI. {0}: {1}.CommenceOperations() called while an owner change is underway.", DebugName, _command.DebugName);
            // 11.18.17 Added to see if this is still an issue. UNCLEAR what I did with LoneFleetCreator if owner change was underway?
        }
        _command.CommenceOperations();
        D.LogBold("{0} has been created in {1}! Frame = {2}.", DebugName, SectorGrid.Instance.GetSector(SectorID).DebugName, Time.frameCount);
        return true;
    }

    protected override void ClearElementReferences() {
        Elements = null;
    }


}

