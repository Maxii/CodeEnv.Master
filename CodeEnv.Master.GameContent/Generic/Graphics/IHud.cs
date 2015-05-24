// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ITooltip.cs
// Interface for easy access to HUDs - Tooltips, ItemHuds and SelectionHuds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;

    /// <summary>
    /// Interface for easy access to HUDs - Tooltips, ItemHuds and SelectionHuds.
    /// </summary>
    public interface IHud {

        /// <summary>
        /// Indicates whether the tooltip is visible to the user.
        /// </summary>
        bool IsShowing { get; }

        /// <summary>
        /// Shows the tooltip at the mouse screen position using the provided content.
        /// </summary>
        /// <param name="content">The content.</param>
        void Show(AHudElementContent content);

        /// <summary>
        /// Shows the tooltip at the mouse screen position using the provided text.
        /// </summary>
        /// <param name="text">The text.</param>
        void Show(string text);

        /// <summary>
        /// Shows the tooltip at the mouse screen position using the content of the StringBuilder.
        /// </summary>
        /// <param name="coloredStringBuilder">The StringBuilder containing text.</param>
        void Show(StringBuilder stringBuilder);


        /// <summary>
        /// Shows the tooltip at the mouse screen position using the content of the ColoredStringBuilder.
        /// </summary>
        /// <param name="coloredStringBuilder">The ColoredStringBuilder containing colorized text.</param>
        void Show(ColoredStringBuilder coloredStringBuilder);

        /// <summary>
        /// Hide the tooltip.
        /// </summary>
        void Hide();

    }
}

