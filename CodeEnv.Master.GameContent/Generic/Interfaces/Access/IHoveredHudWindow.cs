// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHoveredHudWindow.cs
//  Interface for easy access to hovered HudWindows - Tooltip and HoveredItem HudWindows.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;

    /// <summary>
    /// Interface for easy access to hovered HudWindows - Tooltip and HoveredItem HudWindows.
    /// </summary>
    public interface IHoveredHudWindow : IHudWindow {

        /// <summary>
        /// Shows the hovering HudWindow using the provided text.
        /// </summary>
        /// <param name="text">The text.</param>
        void Show(string text);

        /// <summary>
        /// Shows the hovering HudWindow using the content of the StringBuilder.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder containing text.</param>
        void Show(StringBuilder stringBuilder);


        /// <summary>
        /// Shows the hovering HudWindow using the content of the ColoredStringBuilder.
        /// </summary>
        /// <param name="coloredStringBuilder">The ColoredStringBuilder containing colorized text.</param>
        void Show(ColoredStringBuilder coloredStringBuilder);


    }
}

