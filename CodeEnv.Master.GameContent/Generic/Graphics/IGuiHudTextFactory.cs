// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuiHudTextFactory.cs
// Interface for GuiHudTextFactorys that make GuiHudText instances for display by the IGuiHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for GuiHudTextFactorys that make GuiHudText instances for display by the IGuiHud.
    /// </summary>
    /// <typeparam name="DataType">The type of Data.</typeparam>
    public interface IGuiHudTextFactory<DataType> where DataType : AData {

        /// <summary>
        /// Makes or acquires an instance of GuiCursorHudText for the IntelLevel derived from the data provided.
        /// </summary>
        /// <param name="intel">The intel.</param>
        /// <param name="data">The _data.</param>
        /// <returns></returns>
        GuiHudText MakeInstance(Intel intel, DataType data);

        /// <summary>
        /// Makes an instance of IColoredTextList for display by the IGuiHud.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="intel">The intel.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        IColoredTextList MakeInstance(GuiHudLineKeys key, Intel intel, DataType data);

    }
}

