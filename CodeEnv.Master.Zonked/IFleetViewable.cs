// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetViewable.cs
//  Interface used by FleetPresenters to communicate with their associated FleetViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Interface used by FleetPresenters to communicate with their associated FleetViews.
    /// </summary>
    public interface IFleetViewable : IViewable {

        event Action onShowCompletion;

        Transform TrackingTarget { set; }

        void ChangeFleetIcon(IIcon icon, GameColor color);

        void ShowDying();

    }
}

