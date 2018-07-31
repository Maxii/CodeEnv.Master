﻿// --------------------------------------------------------------------------------------------------------------------
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

    using CodeEnv.Master.Common;
    using System.Collections.Generic;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitBaseCmdItems.
    /// </summary>
    public interface IUnitBaseCmd_Ltd : IUnitCmd_Ltd {

        IntVector3 SectorID { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IEnumerable<StationaryLocation> LocalAssemblyStations { get; }

        //IHanger_Ltd Hanger { get; }   11.8.17 IHanger_Ltd is currently obsolete

    }
}

