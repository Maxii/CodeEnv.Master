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
        /// The name to use as the root name of these gameObjects. Optional.
        /// If not set, the root name will be the DisplayName of the Target.
        /// The name of this gameObject will be the root name supplemented 
        /// by the Type name. The name of the child gameObject holding the widget 
        /// will be the root name supplemented by the widget Type name.
        /// </summary>
        string OptionalRootName { set; }

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

        int DrawDepth { get; set; }

        void Set(string text);

        void SetShowDistance(float min, float max = Mathf.Infinity);

        void Show(bool toShow);

        void RefreshWidgetValues();

        void Clear();

    }
}

