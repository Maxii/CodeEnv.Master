﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISystem_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are SystemItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are SystemItems.
    /// </summary>
    public interface ISystem_Ltd : IIntelItem_Ltd {

        IntVector3 SectorID { get; }

        float Radius { get; }

        IEnumerable<IPlanet_Ltd> Planets { get; }

        IStar_Ltd Star { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IEnumerable<StationaryLocation> LocalAssemblyStations { get; }

        bool IsFoundingSettlementAllowedBy(Player player);

    }
}

