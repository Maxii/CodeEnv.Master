// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiTrackingLabel.cs
// Interface used on a 2D GuiTrackingLabel supporting communication with the GameObject
// it is tracking.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

using UnityEngine;

namespace CodeEnv.Master.Common.Unity {

    /// <summary>
    /// Interface used on a 2D GuiTrackingLabel supporting communication with the GameObject
    /// it is tracking.
    /// </summary>
    public interface IGuiTrackingLabel {

        /// <summary>
        /// Target game object this label tracks.
        /// </summary>
        Transform Target { get; set; }

        /// <summary>
        /// Gets or sets whether this <see cref="IGuiTrackingLabel"/> is enabled.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether this <see cref="IGuiTrackingLabel"/> is highlighted.
        /// </summary>
        bool IsHighlighted { get; set; }

        /// <summary>
        /// Populate the label with the provided text and displays it.
        /// </summary>
        /// <param name="text">The text in label.</param>
        void Set(string text);

        /// <summary>
        /// Clears the label so it is not visible.
        /// </summary>
        void Clear();
    }
}

