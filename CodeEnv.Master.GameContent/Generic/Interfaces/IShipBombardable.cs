// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipBombardable.cs
// Interface for Items that can be attacked by Ships from very long range with 'bombard' weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for Items that can be attacked by Ships from very long range with 'bombard' weapons.
    /// <remarks>3.26.17 TODO Planetoids should be 'bombardable' by Ships with specialized PlanetBuster Weapons.
    /// Perhaps also entire units could be 'bombardable' by the same weapon type.</remarks>
    /// </summary>
    public interface IShipBombardable : IElementBombardable {

        ApBesiegeDestinationProxy GetApBesiegeTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, IShip ship);

    }
}

