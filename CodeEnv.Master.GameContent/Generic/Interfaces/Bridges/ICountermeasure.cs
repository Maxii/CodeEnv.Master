﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICountermeasure.cs
// Interface for Countermeasures - passive, active and shields.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Countermeasures - passive, active and shields.
    /// <remarks>Needed to tie together ActiveCountermeasure, PassiveCountermeasure and
    /// ShieldGenerator which don't derive from a common Countermeasure base class.</remarks>
    /// </summary>
    public interface ICountermeasure {

        bool IsDamaged { get; }

        DamageStrength DmgMitigation { get; }

    }
}

