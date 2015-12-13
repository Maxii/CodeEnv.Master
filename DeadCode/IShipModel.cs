// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipModel.cs
// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
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
    /// Interface family that supports non-MonoBehaviour class access to AItemModel-derived MonoBehaviour classes.
    /// </summary>
    [Obsolete]
    public interface IShipModel : IElementModel {

        //event Action onDestinationReached;

        //ShipOrder CurrentOrder { get; set; }

        //ShipState CurrentState { get; }

        //IFleetCmdModel UnitCommand { get; } //{ get; set; }

        //bool IsBearingConfirmed { get; }

        //void OnTopographicBoundaryTransition(SpaceTopography newTopography);

    }
}

