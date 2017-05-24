// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitCmd_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitCmdItems.
    /// </summary>
    public interface IUnitCmd_Ltd : IMortalItem_Ltd {

        /// <summary>
        /// Indicates whether this Unit is in the process of attacking <c>unitCmd</c>.
        /// </summary>
        /// <param name="unitCmd">The UnitCommand potentially under attack by this Unit.</param>
        /// <returns></returns>
        bool IsAttacking(IUnitCmd_Ltd unitCmd);

        bool IsLoneCmd { get; }

        /// <summary>
        /// Indicates whether this operational Cmd has commenced operations.
        /// </summary>
        bool __IsActivelyOperating { get; }

        bool IsOwnerChangeUnderway { get; }

    }
}

