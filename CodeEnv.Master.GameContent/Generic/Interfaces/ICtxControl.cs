// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICtxControl.cs
//  Interface for Context Menu Controls.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

using System;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for Context Menu Controls.
    /// </summary>
    public interface ICtxControl {

        event EventHandler showBegun;

        event EventHandler hideComplete;

        /// <summary>
        /// Flag indicating whether this context menu is currently showing.
        /// </summary>
        bool IsShowing { get; }

        /// <summary>
        /// Tries to show this Item's context menu appropriate to the Item that is 
        /// currently selected, if any. Returns <c>true</c> if the context menu was shown.
        /// </summary>
        /// <returns></returns>
        bool TryShowContextMenu();

        void Hide();

    }
}

