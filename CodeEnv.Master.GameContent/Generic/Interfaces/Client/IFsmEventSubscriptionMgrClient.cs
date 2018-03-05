// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFsmEventSubscriptionMgrClient.cs
// Interface for clients of FsmEventSubscriptionManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for clients of FsmEventSubscriptionManager.
    /// <remarks>4.20.17 Currently includes all UnitItems.</remarks>
    /// </summary>
    public interface IFsmEventSubscriptionMgrClient {

        string DebugName { get; }

        bool ShowDebugLog { get; }

        Player Owner { get; }

        PlayerAIManager OwnerAiMgr { get; }


        void HandleFsmTgtInfoAccessChgd(IOwnerItem_Ltd fsmTgt);

        void HandleFsmTgtOwnerChgd(IOwnerItem_Ltd fsmTgt);

        void HandleFsmTgtDeath(IMortalItem_Ltd deadFsmTgt);

        void HandleAwarenessChgd(IMortalItem_Ltd item);

    }
}

