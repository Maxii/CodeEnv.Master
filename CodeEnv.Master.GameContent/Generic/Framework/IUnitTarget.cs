// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitTarget.cs
//  Interface for a target that is a Unit Element or Command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for a target that is a Unit Element or Command.
    /// </summary>
    public interface IUnitTarget : IMortalTarget {

        float MaxWeaponsRange { get; }

    }
}

