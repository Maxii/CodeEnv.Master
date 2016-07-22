// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlanet_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are PlanetItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are PlanetItems.
    /// </summary>
    public interface IPlanet_Ltd : IPlanetoid_Ltd {

        IList<StationaryLocation> LocalAssemblyStations { get; }


    }
}

