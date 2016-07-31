// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShip.cs
// Interface for easy access to MonoBehaviours that are ShipItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are ShipItems.
    /// </summary>
    public interface IShip : IUnitElement {

        Vector3 CurrentHeading { get; }

        float ActualSpeedValue { get; }

        float CollisionDetectionZoneRadius { get; }

        void HandlePendingCollisionWith(IObstacle obstacle);

        void HandlePendingCollisionAverted(IObstacle obstacle);

    }
}

