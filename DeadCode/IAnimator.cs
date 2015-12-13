// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Microsoft
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IAnimator.cs
// Interface for Item Animators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for Item Animators.
    /// </summary>
    [Obsolete]
    public interface IAnimator {

        /// <summary>
        /// Occurs when [on animation finished].
        /// </summary>
        event Action onAnimationFinished;

        /// <summary>
        /// Starts the specified animation.
        /// </summary>
        /// <param name="animationID">The animation identifier.</param>
        void Start(EffectID animationID);

        /// <summary>
        /// Stops the specified animation.
        /// </summary>
        /// <param name="animationID">The animation identifier.</param>
        void Stop(EffectID animationID);

    }
}

