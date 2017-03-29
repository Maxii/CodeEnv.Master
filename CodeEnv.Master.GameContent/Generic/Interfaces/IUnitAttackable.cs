﻿// --------------------------------------------------------------------------------------------------------------------
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
    /// </summary>
    public interface IUnitAttackable : IFleetNavigable, IAttackable {

        Player Owner_Debug { get; }

    }
}

