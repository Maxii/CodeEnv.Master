﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiHudPublisher.cs
// Interface for the multiple GuiHudPublisher&lt;DataType&gt; types that publish IGuiHuds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System.Collections;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for the multiple GuiHudPublisher&lt;DataType&gt; types that
    /// publish IGuiHuds.
    /// </summary>
    public interface IGuiHudPublisher {

        /// <summary>
        /// Gets a value indicating whether the IGuiHud
        /// is currently being shown by this Publisher.
        /// </summary>
        bool IsHudShowing { get; }

        /// <summary>
        /// Displays a new, updated or already existing GuiCursorHudText instance containing 
        /// the text to display at the cursor.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        void DisplayHudAtCursor(IntelLevel intelLevel);

        /// <summary>
        /// Coroutine compatible method that keeps the hud text current. 
        /// </summary>
        /// <returns></returns>
        IEnumerator KeepHudCurrent();

        /// <summary>
        /// Clients can optionally provide additional GuiCursorHudLineKeys they wish to routinely update whenever GetHudText is called.
        /// LineKeys already automatically handled for all managers include Distance and IntelState.
        /// </summary>
        /// <param name="optionalKeys">The optional keys.</param>
        void SetOptionalUpdateKeys(params GuiHudLineKeys[] optionalKeys);

        /// <summary>
        /// Clears the IGuiHud.
        /// </summary>
        void ClearHud();

    }
}

