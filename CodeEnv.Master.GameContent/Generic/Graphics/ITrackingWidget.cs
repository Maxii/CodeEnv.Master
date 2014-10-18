// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITrackingWidget.cs
//  Interface for all tracking widgets (labels, sprites, other?) that track IWidgetTrackable objects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for all tracking widgets (labels, sprites, other?) that track IWidgetTrackable objects.
    /// </summary>
    public interface ITrackingWidget {

        bool IsShowing { get; }

        /// <summary>
        /// The name of this tracking widget gameObject.
        /// </summary>
        string Name { get; set; }

        GameColor Color { get; set; }

        GameColor HighlightColor { get; set; }

        bool IsHighlighted { get; set; }

        /// <summary>
        /// The transform of the underlying UIWidget which allows
        /// tracking of the icon itself, including any pivot offset value.
        /// </summary>
        Transform WidgetTransform { get; }

        WidgetPlacement Placement { get; set; }

        IWidgetTrackable Target { get; set; }

        void Set(string text);

        void SetShowDistance(float min, float max = Mathf.Infinity);

        void Show(bool toShow);

        void RefreshWidgetValues();

        void Clear();

    }
}

