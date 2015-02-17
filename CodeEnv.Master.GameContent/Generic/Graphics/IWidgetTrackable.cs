// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWidgetTrackable.cs
//  Interface for GameObjects that are trackable by UI or World Widgets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Interface for GameObjects that are trackable by UI or World Widgets.
    /// </summary>
    public interface IWidgetTrackable {

        string DisplayName { get; }

        /// <summary>
        /// Gets the transform of this IWidgetTrackable target.
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Gets the offset used to place the widget, relative to this IWidgetTrackable target.
        /// </summary>
        /// <param name="placement">The placement.</param>
        /// <returns></returns>
        Vector3 GetOffset(WidgetPlacement placement);

    }
}

