// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IHud.cs
// Interface for GuiHuds so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System.Text;

    /// <summary>
    /// Interface for GuiHuds so non-scripts can refer to it.
    /// </summary>
    public interface IHud {

        /// <summary>
        /// Populate the HUD with text.
        /// </summary>
        /// <param name="text">The text to place in the HUD.</param>
        void Set(string text);

        /// <summary>
        /// Populate the HUD with text from the StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder containing the text.</param>
        void Set(StringBuilder sb);

        /// <summary>
        /// Clear the HUD.
        /// </summary>
        void Clear();

    }
}

