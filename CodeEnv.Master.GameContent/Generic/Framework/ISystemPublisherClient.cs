// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISystemPublisherClient.cs
// Interface that SystemPublishers use to communicate with their SystemItem clients.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    ///  Interface that SystemPublishers use to communicate with their SystemItem clients.
    /// </summary>
    public interface ISystemPublisherClient {

        StarReport GetStarReport(Player player);

        PlanetoidReport[] GetPlanetoidReports(Player player);

        /// <summary>
        /// Gets the settlement report if a settlement is present. Can be null.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        SettlementReport GetSettlementReport(Player player);

    }
}

