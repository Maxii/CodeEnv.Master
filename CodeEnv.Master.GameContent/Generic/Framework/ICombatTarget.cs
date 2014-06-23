// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICombatTarget.cs
// Interface for a target that can engage in combat, causing weapons to fire.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for a target that can engage in combat, causing weapons to fire.
    /// </summary>
    public interface ICombatTarget : IMortalTarget {

        float MaxWeaponsRange { get; }

    }
}

