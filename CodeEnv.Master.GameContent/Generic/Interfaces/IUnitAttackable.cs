// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitAttackable.cs
// Interface for Items that UnitCmds are allowed to designate as a target to be attacked.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Items that UnitCmds are allowed to designate as a target to be attacked.
    /// <remarks>4.3.17 Currently FleetCmds, BaseCmds. TODO: Planets (not Moons) will be included when
    /// Bombard Weapons appear.</remarks>
    /// </summary>
    public interface IUnitAttackable : IFleetNavigableDestination, IAttackable {

        Player Owner_Debug { get; }

    }
}

