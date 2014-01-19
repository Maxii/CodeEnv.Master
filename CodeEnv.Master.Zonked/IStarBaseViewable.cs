// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStarbaseViewable.cs
// Interface used by StarbasePresenters to communicate with their associated StarbaseVIews.
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
    /// Interface used by StarbasePresenters to communicate with their associated StarbaseVIews.
    /// </summary>
    public interface IStarbaseViewable : ICommandViewable {
        //public interface IStarbaseViewable : IViewable {

        //event Action onShowCompletion;

        //Transform TrackingTarget { set; }

        //void ChangeFleetIcon(IIcon icon, GameColor color);

        //void ShowDying();


    }
}

