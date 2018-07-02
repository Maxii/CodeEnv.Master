// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitElement_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitElementItems.
    /// </summary>
    public interface IUnitElement_Ltd : IMortalItem_Ltd {

        /// <summary>
        /// Occurs when HQ status of this UnitElement changes.
        /// <remarks>3.25.17 Debated treating access to HQ status like Owner with restricted access based on
        /// IntelCoverage. Decided this creates more work than value. Instead,
        /// I'll restrict my usage to when the client has a need and a right to know.
        /// </remarks>
        /// </summary>
        event EventHandler isHQChanged;

        /// <summary>
        /// Returns <c>true</c> when this UnitElement is the HQ of the Unit, <c>false</c> otherwise.
        /// <remarks>3.25.17 Debated treating access to HQ status like Owner with restricted access based on
        /// IntelCoverage. Decided this creates more work than value. Instead,
        /// I'll restrict my usage to when the client has a need and a right to know.
        /// </remarks>
        /// </summary>
        bool IsHQ { get; }

        /// <summary>
        /// Returns the UnitCommand for this UnitElement.
        /// <remarks>3.25.17 Debated treating access to Command like Owner with restricted access based on
        /// IntelCoverage. Also considered restricting access to only HQElements - TryGetCmd(out cmd)
        /// or Command returns null if no access. Decided this creates more work than value. Instead,
        /// I'll restrict my usage to when the client has a need and a right to know.
        /// </remarks>
        /// </summary>
        IUnitCmd_Ltd Command { get; }

        bool __TryGetIsHQChangedEventSubscribers(out string targetNames);

        bool __IsOwnerChgUnderway { get; }
    }
}

