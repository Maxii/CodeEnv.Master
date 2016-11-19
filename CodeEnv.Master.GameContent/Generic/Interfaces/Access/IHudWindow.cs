// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHudWindow.cs
//  Interface for easy access to HudWindows - Tooltip, HoveredItem and SelectedItem HudWindows.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Text;

    /// <summary>
    /// Interface for easy access to HudWindows - Tooltips, HoveredItem and SelectedItem HudWindows.
    /// </summary>
    public interface IHudWindow {

        /// <summary>
        /// Indicates whether the HudWIndow is visible to the user.
        /// </summary>
        bool IsShowing { get; }

        /// <summary>
        /// Hides the HudWindow.
        /// </summary>
        void Hide();

    }
}

