// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICountermeasure.cs
// Interface for Countermeasures, both passive and active.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Countermeasures, both passive and active.
    /// <remarks>Needed to tie together ActiveCountermeasure and PassiveCountermeasure 
    /// which don't derive from a common Countermeasure base class.</remarks>
    /// </summary>
    public interface ICountermeasure {

        event Action<AEquipment> onIsOperationalChanged;

        bool IsOperational { get; set; }

        DamageStrength DamageMitigation { get; }

    }
}

