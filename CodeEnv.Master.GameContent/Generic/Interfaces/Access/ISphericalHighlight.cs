// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISphericalHighlight.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface ISphericalHighlight {

        bool IsShowing { get; }

        float Alpha { get; set; }

        GameColor Color { get; set; }

        /// <summary>
        /// Gets the Target's name. Will be None if target is null.
        /// </summary>
        string TargetName { get; }

        ///// <summary>
        ///// Sets the target to highlight.
        ///// </summary>
        ///// <param name="target">The target.</param>
        ///// <param name="labelPlacement">The label placement.</param>
        void SetTarget(IWidgetTrackable target, WidgetPlacement labelPlacement = WidgetPlacement.Below);

        /// <summary>
        /// Sets the radius of the highlighting sphere.
        /// </summary>
        /// <param name="sphereRadius">The sphere radius.</param>
        void SetRadius(float sphereRadius);

        /// <summary>
        /// Shows or hides the sphere highlight.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> [to show].</param>
        void Show(bool toShow);

        Transform transform { get; }

    }
}

