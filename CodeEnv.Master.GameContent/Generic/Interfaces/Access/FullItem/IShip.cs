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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are ShipItems.
    /// </summary>
    public interface IShip : IUnitElement {

        new IFleetCmd Command { get; }

        ShipOrder CurrentOrder { get; }

        Vector3 CurrentHeading { get; }

        ShipCombatStance CombatStance { get; }

        bool IsTurning { get; }

        float MaxTurnRate { get; }

        float ActualSpeedValue { get; }

        float CollisionDetectionZoneRadius { get; }

        IFleetFormationStation FormationStation { get; }

        void HandlePendingCollisionWith(IObstacle obstacle);

        void HandlePendingCollisionAverted(IObstacle obstacle);

    }
}

