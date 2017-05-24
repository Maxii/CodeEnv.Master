// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALoneUnitCreator.cs
// Abstract Unit Creator that builds and deploys a Unit configured using a basic Cmd and LoneElement at its current location in the scene.
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
/// Abstract Unit Creator that builds and deploys a Unit configured using a basic Cmd and LoneElement at its current location in the scene.
/// </summary>
public abstract class ALoneUnitCreator : AUnitCreator {

    internal AUnitElementItem LoneElement { get; set; }

    protected AUnitCmdItem _command;

    protected sealed override void AttemptToAuthorizeDeploymentOnStart() {
        // Don't build and authorize as its already been done by UnitFactory
    }

    protected sealed override void MakeElements() {
        D.Assert(!Configuration.ElementDesignNames.Any());
    }

    protected sealed override void AddElementsToCommand() {
        LogEvent();
        D.AssertNotNull(LoneElement);
        _command.AddElement(LoneElement);
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    protected sealed override void AssignHQElement() {
        LogEvent();
        _command.HQElement = LoneElement;
    }

    protected sealed override void CompleteUnitInitialization() {
        LogEvent();
        D.Assert(LoneElement.IsOperational);
        _command.FinalInitialize();
    }

    protected sealed override void AddUnitToGameKnowledge() {
        LogEvent();
        //D.Log(ShowDebugLog, "{0} is adding Unit {1} to GameKnowledge.", DebugName, UnitName);
        _gameMgr.GameKnowledge.AddUnit(_command, Enumerable.Empty<IUnitElement>()); // element already present
    }

    protected sealed override void BeginElementsOperations() { }

    protected sealed override bool BeginCommandOperations() {
        LogEvent();
        bool isOpsBegun = true;
        if (LoneElement.IsOwnerChangeUnderway) {
            LoneElement.ownerChanged += CommenceCmdOperationsAfterOwnerChangedEventHandler;
            isOpsBegun = false;
        }
        else {
            _command.CommenceOperations();
        }
        return isOpsBegun;
    }


    #region Event and Property Change Handlers

    private void CommenceCmdOperationsAfterOwnerChangedEventHandler(object sender, EventArgs e) {
        LoneElement.ownerChanged -= CommenceCmdOperationsAfterOwnerChangedEventHandler;
        D.Log("{0} is Commencing Operations after owner change completed in Frame {1}.", DebugName, Time.frameCount);
        _command.CommenceOperations();
        ClearElementReferences();
    }

    #endregion

    protected sealed override void ClearElementReferences() {
        LoneElement = null;
    }

    #region Debug

    #endregion

}

