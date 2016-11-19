// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitBaseCmd_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitBaseCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitBaseCmdItems.
    /// </summary>
    public interface IUnitBaseCmd_Ltd : IUnitCmd_Ltd {

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

    }
}

