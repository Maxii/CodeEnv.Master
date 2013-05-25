// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiCursorHud.cs
// Interface provided methods to talk to the GuiCursorHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

using System.Text;
namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface provided methods to talk to the GuiCursorHud.
    /// </summary>
    public interface IGuiCursorHud {

        /// <summary>
        /// Clear the HUD so only the cursor shows.
        /// </summary>
        void Clear();

        /// <summary>
        /// Populate the HUD with text.
        /// </summary>
        /// <param name="hudText">The text to place in the HUD.</param>
        void Set(string hudText);

        /// <summary>
        /// Populate the HUD with text from the StringBuilder.
        /// </summary>
        /// <param name="sbHudText">The StringBuilder containing the text.</param>
        void Set(StringBuilder sbHudText);


    }
}

