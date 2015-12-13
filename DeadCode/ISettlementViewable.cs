// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISettlementViewable.cs
// Interface used by a SettlementPresenter to communicate with their associated SettlementView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface used by a SettlementPresenter to communicate with their associated SettlementView.
    /// </summary>
    public interface ISettlementViewable : IViewable {

        event Action onShowCompletion;

        void ShowAttacking();

        void ShowHit();

        void ShowDying();

        void ShowRepairing();

        void ShowRefitting();

        void StopShowing();

    }
}

