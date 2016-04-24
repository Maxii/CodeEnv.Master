﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipItem.cs
// Interface for all items that are ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Interface for all items that are ships.
    /// </summary>
    public interface IShipItem : IUnitElementItem {

        Vector3 CurrentHeading { get; }

        float ActualSpeedValue { get; }

        Speed CurrentSpeed { get; }

        float CollisionDetectionZoneRadius { get; }

        void HandlePendingCollisionWith(IObstacle obstacle);

        void HandlePendingCollisionAverted(IObstacle obstacle);

    }
}

