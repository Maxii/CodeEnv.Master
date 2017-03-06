// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPropellable.cs
// Interface for Items that have engines.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Items that have engines.
    /// </summary>
    public interface IPropellable : IDetectable {

        bool IsFtlCapable { get; }

        void HandleFtlDampenerActivated(IUnitCmd_Ltd source, RangeCategory rangeCat);  // ship can be affected by more than one dampener, ala sensors

        void HandleFtlDampenerDeactivated(IUnitCmd_Ltd source, RangeCategory rangeCat);

        bool TryGetOwner(Player requestingPlayer, out Player owner);

        bool IsOwnerAccessibleTo(Player player);

    }
}

