// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IOnVisibleRelayTarget.cs
// Interface used on a GameObject that needs to know about another object's Renderer events.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface used on a GameObject that needs to know about another object's Renderer events.
    /// <remarks>Typically used on a parent GameObject that is separated from its mesh and renderer.</remarks>
    /// </summary>
    public interface IOnVisibleRelayTarget {

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

