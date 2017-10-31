// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitBaseCmd.cs
// Interface for easy access to MonoBehaviours that are AUnitBaseCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are AUnitBaseCmdItems.
    /// </summary>
    public interface IUnitBaseCmd : IUnitCmd {

        event EventHandler resourcesChanged;

        BaseOrder CurrentOrder { get; }

        ResourcesYield Resources { get; }

    }
}

