// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipModel.cs
// Interface for ShipModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for ShipModels.
    /// </summary>
    public interface IShipModel : IElementModel {

        event Action onDestinationReached;

        //new ShipData Data { get; set; }

        ShipOrder CurrentOrder { get; set; }

        ShipState CurrentState { get; }

        new IFleetCmdModel Command { get; set; }

        bool IsBearingConfirmed { get; }

        /// <summary>
        /// Called by the ship's FormationStation when the ship arrives or leaves its station.
        /// </summary>
        /// <param name="isOnStation">if set to <c>true</c> [is on station].</param>
        void OnShipOnStation(bool isOnStation);

        void OnTopographicBoundaryTransition(SpaceTopography newTopography);

    }
}

