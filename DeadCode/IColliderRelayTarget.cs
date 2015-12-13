// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IColliderRelayTarget.cs
// Interface used on a GameObject that needs to know about another object's Collider events.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using System;

namespace CodeEnv.Master.Common {


    /// <summary>
    /// Interface used on a GameObject that needs to know about another object's Collider events.
    ///<remarks>Typically used on a parent GameObject that is separated from its collider.</remarks>
    /// </summary>
    [Obsolete]
    public interface IColliderRelayTarget {

        void OnHover(bool isOver);

        void OnClick();

        void OnDoubleClick();

    }
}

