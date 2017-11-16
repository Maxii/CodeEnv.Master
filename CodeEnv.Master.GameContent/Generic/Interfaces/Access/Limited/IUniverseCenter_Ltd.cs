// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUniverseCenter_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are UniverseCenterItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are UniverseCenterItems.
    /// </summary>
    public interface IUniverseCenter_Ltd : IIntelItem_Ltd {

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

    }
}

