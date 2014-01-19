// --------------------------------------------------------------------------------------------------------------------
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

namespace CodeEnv.Master.GameContent {

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
        /// Shows or hides a current GuiCursorHudText 
        /// instance containing the text to display at the cursor.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> shows the hud, otherwise hides it.</param>
        /// <param name="intel">The intel.</param>
        void ShowHud(bool toShow, Intel intel);

        /// <summary>
        /// Clients can optionally provide additional GuiCursorHudLineKeys they wish to routinely update whenever GetHudText is called.
        /// LineKeys already automatically handled for all managers include Distance and IntelState.
        /// </summary>
        /// <param name="optionalKeys">The optional keys.</param>
        void SetOptionalUpdateKeys(params GuiHudLineKeys[] optionalKeys);

    }
}

