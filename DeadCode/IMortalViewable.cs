// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalViewable.cs
// Interface used by AMortalItemPresenters to communicate with their associated AMortalItemViews.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    ///  Interface used by AMortalItemPresenters to communicate with their associated AMortalItemViews.
    /// </summary>
    public interface IMortalViewable : IViewable {

        event Action onShowCompletion;

        void ShowAnimation(EffectID animation);

        void StopAnimation(EffectID animation);

        void OnDeath();

    }
}

