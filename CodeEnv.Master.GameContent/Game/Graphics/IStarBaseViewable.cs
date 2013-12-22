// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStarBaseViewable.cs
// Interface used by a StarBasePresenter to communicate with their associated StarBaseView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface used by a StarBasePresenter to communicate with their associated StarBaseView.
    /// </summary>
    public interface IStarBaseViewable : IViewable {

        event Action onShowCompletion;

        void ShowAttacking();

        void ShowHit();

        void ShowDying();

        void ShowRepairing();

        void ShowRefitting();

        void StopShowing();

        void HighlightTrackingLabel(bool toHighlight);

    }
}

