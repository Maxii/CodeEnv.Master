// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShip_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are ShipItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are ShipItems.
    /// </summary>
    public interface IShip_Ltd : IUnitElement_Ltd {

        float CollisionDetectionZoneRadius_Debug { get; }
    }
}

