// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOnVisible.cs
// Interface used on a GameObject that needs to know when its visibility state changes even if
// it doesn't have a renderer, implementing OnBecameVisible and OnBecameInvisible.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface used on a GameObject that needs to know when its visibility state changes even if it doesn't have
    /// a renderer, implementing OnBecameVisible and OnBecameInvisible.
    /// <remarks>Used on a parent GameObject that is separated from its mesh and renderer.</remarks>
    /// </summary>
    public interface IOnVisible {

        /// <summary>
        /// Property that tracks the VisibilityState of the implementing GameObject.
        /// </summary>
        Visibility VisibilityState { get; set; }

        /// <summary>
        /// Called by the child renderer's VisiblityRelay when the renderer becomes visible.
        /// </summary>
        void OnBecameVisible();

        /// <summary>
        /// Called by the child renderer's VisiblityRelay when the renderer becomes invisible. 
        /// </summary>
        void OnBecameInvisible();
    }
}

