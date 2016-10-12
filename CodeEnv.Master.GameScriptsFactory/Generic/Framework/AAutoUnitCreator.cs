// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AAutoUnitCreator.cs
// Abstract base class for Unit Creators whose configuration is determined automatically by NewGameUnitConfigurator. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Unit Creators whose configuration is determined automatically by NewGameUnitConfigurator. 
/// </summary>
public abstract class AAutoUnitCreator : AUnitCreator {

    //protected override void InitializeDeploymentSystem() {
    //    if (_gameMgr.IsRunning) {
    //        HandleGameIsRunning();
    //    }
    //    else {
    //        Subscribe();
    //    }
    //}

    //protected override void HandleGameIsRunning() {
    //    D.Assert(Configuration != null);    // AutoCreator would only be deployed with a Configuration
    //    base.HandleGameIsRunning();
    //}

    public override void InitiateDeployment() {
        D.Assert(Configuration != null);    // AutoCreator would only be deployed with a Configuration
        base.InitiateDeployment();
    }
}

