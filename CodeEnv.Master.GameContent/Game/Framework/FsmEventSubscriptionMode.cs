// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FsmEventSubscriptionMode.cs
// The different modes of event subscription used by the Unit Item FSM and the FsmEventSubscriptionManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The different modes of event subscription used by the Unit Item FSM and the FsmEventSubscriptionManager.
    /// </summary>
    public enum FsmEventSubscriptionMode {

        None,

        FsmTgtDeath,
        FsmTgtInfoAccessChg,
        FsmTgtOwnerChg,

        OwnerAwareChg_Fleet,
        OwnerAwareChg_Ship,
        OwnerAwareChg_Base,
        OwnerAwareChg_Facility,

        OwnerAware_Planet,

        // 6.12.18 Currently covers vacancy state changes for StationaryLocation Starbase Stations in Sectors. Requires a Sector target.
        FsmTgtVacancyChg

    }
}

