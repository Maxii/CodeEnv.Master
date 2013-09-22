// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IColoredTextList.cs
// Interface for strategy objects that provide lists of Colored Text for GuiCursorHudText.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for strategy objects that provide lists of Colored Text for GuiCursorHudText.
    /// </summary>
    public interface IColoredTextList {

        /// <summary>
        /// Readonly, gets the list.
        /// </summary>
        IList<ColoredText> List { get; }

    }
}

