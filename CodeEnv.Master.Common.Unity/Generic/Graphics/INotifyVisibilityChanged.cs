// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INotifyVisibilityChangedcs
// Interface used on a GameObject that needs to know about another object's Visibility changes
// that come from its Renderer.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using UnityEngine;

    /// <summary>
    /// Interface used on a GameObject that needs to know about another object's Visibility changes that come
    /// from its Renderer.
    /// <remarks>Commonly used on a parent GameObject that is separated from its mesh and renderer.</remarks>
    /// </summary>
    public interface INotifyVisibilityChanged {

        bool IsVisible { get; set; }

        void NotifyVisibilityChanged(Transform sender, bool isVisible);

    }
}

