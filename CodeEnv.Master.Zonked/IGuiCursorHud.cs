// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiCursorHud.cs
// Interface for GuiCursorHud so non-scripts can refer to it.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

//namespace CodeEnv.Master.Common.Unity {

using System.Text;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Interface for GuiCursorHud so non-MonoBehaviours can refer to it.
/// </summary>
public interface IGuiCursorHud {

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
    /// Populate the HUD with text from the GuiCursorHudText.
    /// </summary>
    /// <param name="guiCursorHudText">The GUI cursor hud text.</param>
    void Set(GuiCursorHudText guiCursorHudText);

    /// <summary>
    /// Clear the HUD so only the cursor shows.
    /// </summary>
    void Clear();

    void SetPivot(UIWidget.Pivot pivot);

}
//}

