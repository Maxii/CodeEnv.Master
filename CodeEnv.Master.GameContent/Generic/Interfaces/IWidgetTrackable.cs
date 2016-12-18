﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWidgetTrackable.cs
// Interface for GameObjects that are capable of having their position tracked by UI or World Widgets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for GameObjects that are capable of having their position tracked by UI or World Widgets.
    /// </summary>
    public interface IWidgetTrackable {

        string DebugName { get; }

        Vector3 Position { get; }

        /// <summary>
        /// Gets the transform of this IWidgetTrackable object.
        /// </summary>
        Transform transform { get; }

        /// <summary>
        /// Gets the offset used to place the widget, relative to this IWidgetTrackable object.
        /// </summary>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        Vector3 GetOffset(WidgetPlacement placement);

    }
}

