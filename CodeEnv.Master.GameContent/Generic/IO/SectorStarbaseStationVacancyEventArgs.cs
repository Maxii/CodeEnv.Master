// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorStarbaseStationVacancyEventArgs.cs
// EventArgs supporting a Sector's stationVacancyChgd event for Starbase Stations.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// EventArgs supporting a Sector's stationVacancyChgd event for Starbase Stations.
    /// </summary>
    public class SectorStarbaseStationVacancyEventArgs : EventArgs {

        public StationaryLocation Station { get; private set; }

        public bool IsVacant { get; private set; }

        public SectorStarbaseStationVacancyEventArgs(StationaryLocation station, bool isVacant) {
            Station = station;
            IsVacant = isVacant;
        }

    }
}

