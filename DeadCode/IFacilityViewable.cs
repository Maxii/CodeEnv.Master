// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFacilityViewable.cs
// Interface used by a FacilityPresenter to communicate with their associated FacilityView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface used by a FacilityPresenter to communicate with their associated FacilityView.
    /// </summary>
    public interface IFacilityViewable : IViewable {

        event Action onShowCompletion;

        void ShowAttacking();

        void ShowHit();

        void ShowDying();

        void ShowRepairing();

        void ShowRefitting();

        void StopShowing();

    }
}

