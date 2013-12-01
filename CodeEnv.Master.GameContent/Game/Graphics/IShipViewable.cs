// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipViewable.cs
// Interface used by ShipPresenters to communicate with their associated ShipViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    ///  Interface used by ShipPresenters to communicate with their associated ShipViews.
    /// </summary>
    public interface IShipViewable : IViewable {

        event Action onShowCompletion;

        void ShowAttacking();

        void ShowHit();

        void ShowDying();

        void ShowEntrenching();

        void ShowRepairing();

        void ShowRefitting();

        void StopShowing();

    }
}

