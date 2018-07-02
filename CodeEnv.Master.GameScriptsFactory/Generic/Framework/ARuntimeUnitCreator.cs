// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ARuntimeUnitCreator.cs
// Abstract base class for Unit Creators that are created and deployed during runtime.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;
using System;

/// <summary>
/// Abstract base class for Unit Creators that are created and deployed during runtime.
/// </summary>
public abstract class ARuntimeUnitCreator : AUnitCreator {

    public override GameDate DeployDate { get { return GameTime.Instance.CurrentDate; } }

    protected sealed override void AttemptToAuthorizeDeploymentOnStart() {
        // The user of this creator always immediately authorizes deployment 
    }

    protected sealed override void PrepareUnitForDeployment_Internal() {
        MakeCommand();
        AddElementsToCommand();
        AssignHQElement();
        PositionUnit();
        HandleUnitPositioned();
    }

    protected abstract void MakeCommand();

    protected abstract void AddElementsToCommand();

    protected abstract void AssignHQElement();

    protected abstract void PositionUnit();

    protected virtual void HandleUnitPositioned() {
        LogEvent();
    }

    #region Debug

    protected sealed override void __ValidateNotDuplicateDeployment() {
        // not needed as this derived class overrides AttemptToAuthorizeDeploymentOnStart
        throw new NotImplementedException();
    }

    #endregion


}

