// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiHudPublisher.cs
// Interface for GuiHudPublisher instances so classes that are not in the scripts
// project can talk to them.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections;

    /// <summary>
    /// Interface for GuiHudPublisher instances so classes that are not in the scripts
    /// project can talk to them.
    /// </summary>
    public interface IGuiHudPublisher {

        /// <summary>
        /// Displays a new, updated or already existing GuiCursorHudText instance containing 
        /// the text to display at the cursor.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        void DisplayHudAtCursor(IntelLevel intelLevel);

        /// <summary>
        /// Coroutine compatible method that keeps the hud text current. 
        /// </summary>
        /// <param name="updateFrequency">Seconds between updates.</param>
        /// <returns></returns>
        IEnumerator KeepHudCurrent(float updateFrequency);

        /// <summary>
        /// Clients can optionally provide additional GuiCursorHudLineKeys they wish to routinely update whenever GetHudText is called.
        /// LineKeys already automatically handled for all managers include Distance and IntelState.
        /// </summary>
        /// <param name="optionalKeys">The optional keys.</param>
        void SetOptionalUpdateKeys(params GuiHudLineKeys[] optionalKeys);

        /// <summary>
        /// Clears the hud and stops any coroutines keeping it updated.
        /// </summary>
        void ClearHud();

    }
}

