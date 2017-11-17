// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCreator.cs
// Unit Creator that deploys a fleet from the operational ships provided at its current location in the scene.
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
/// Unit Creator that deploys a fleet from the operational ships provided at its current location in the scene.
/// </summary>
public class FleetCreator : AUnitCreator {

    public override GameDate DeployDate { get { return GameTime.Instance.CurrentDate; } }

    public IEnumerable<ShipItem> Elements { get; set; }

    public string CmdDesignName { get; set; }

    protected override Player Owner { get { return Elements.First().Owner; } }

    private FleetCmdItem _command;

    protected override void InitializeRootUnitName() {
        RootUnitName = "FleetFromShips";
    }

    protected override void AttemptToAuthorizeDeploymentOnStart() {
        // The user of this creator always immediately authorizes deployment 
    }

    public override void PrepareUnitForDeployment() {
        _command = MakeCommand();
        foreach (var element in Elements) {
            _command.AddElement(element);
        }
        _command.HQElement = _command.SelectHQElement();
    }

    private FleetCmdItem MakeCommand() {
        return _factory.MakeFleetCmdInstance(Owner, CmdDesignName, gameObject, UnitName);
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
            bool isCanceled = e.CancelSuperiorsOrder();   // will restart Idling and thereby notify Cmd its available
            D.Assert(isCanceled);   // 11.15.17 New fleets are not created via Ship Captain override orders, not even FleeAndRepair
        });
    }

    protected override bool BeginCommandOperations() {
        _command.CommenceOperations();
        return true;
    }

    protected override void ClearElementReferences() {
        Elements = null;
    }

    #region Debug

    protected override void __ValidateNotDuplicateDeployment() {
        // not needed as this derived class overrides AttemptToAuthorizeDeploymentOnStart
        throw new NotImplementedException();
    }

    #endregion

}

