// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IConstructionManagerClient.cs
// Interface for AUnitBaseCmd when their ConstructionManager needs to communicate with them.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Interface for AUnitBaseCmd when their ConstructionManager needs to communicate with them.
/// </summary>
public interface IConstructionManagerClient {

    void HandleUncompletedConstructionRemovedFromQueue(Construction construction);

    void HandleConstructionCompleted(Construction construction);

}

