// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudTextFactory_System.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Systems.
    /// </summary>
    public class GuiCursorHudTextFactory_System : AGuiCursorHudTextFactory {

        public GuiCursorHudTextFactory_System(SystemData data) : base(data) { }

        /// <summary>
        /// Makes a strategy instance of IColoredTextList. Only the keys that use data from this SystemData are implemented
        /// in this derived class.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override IColoredTextList MakeInstance_ColoredTextList(IntelLevel intelLevel, GuiCursorHudDisplayLineKeys key) {
            SystemData data = _data as SystemData;
            switch (key) {
                case GuiCursorHudDisplayLineKeys.Capacity:  // {0:00}     int with at least two digits
                    return (data.Capacity != Constants.Zero) ? new ColoredTextList<int>(data.Capacity, "{0:00}") : new ColoredTextList<int>();
                case GuiCursorHudDisplayLineKeys.Resources: // {0:0.}    float with zero decimal places, rounded
                    return (data.Resources != null) ? new ColoredTextList_Resources(data.Resources) : new ColoredTextList_Resources();
                case GuiCursorHudDisplayLineKeys.Specials:
                    return (data.SpecialResources != null) ? new ColoredTextList_Specials(data.SpecialResources) : new ColoredTextList_Specials();
                case GuiCursorHudDisplayLineKeys.IntelState:
                    return (data.DateHumanPlayerExplored != null) ? new ColoredTextList_Intel(data.DateHumanPlayerExplored, intelLevel) : new ColoredTextList_Intel();
                default:
                    return base.MakeInstance_ColoredTextList(intelLevel, key);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

