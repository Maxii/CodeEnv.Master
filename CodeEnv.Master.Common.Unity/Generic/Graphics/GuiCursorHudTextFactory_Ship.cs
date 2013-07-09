// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudTextFactory_Ship.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;

    public class GuiCursorHudTextFactory_Ship : AGuiCursorHudTextFactory {

        public GuiCursorHudTextFactory_Ship(ShipData data) : base(data) { }

        /// <summary>
        /// Makes a strategy instance of IColoredTextList. Only the keys that use data from this FleetData are implemented
        /// in this derived class.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override IColoredTextList MakeInstance_ColoredTextList(IntelLevel intelLevel, GuiCursorHudDisplayLineKeys key) {
            ShipData data = _data as ShipData;
            switch (key) {
                case GuiCursorHudDisplayLineKeys.Speed:
                    return new ColoredTextList<float>("{0:0.#}", data.SpeedReadout, data.MaxSpeed);  // ship will always display speed, even if zero
                case GuiCursorHudDisplayLineKeys.ShipDetails:
                    // test with max turn rate as ships will always have a non zero value
                    return data.MaxTurnRate != Constants.ZeroF ? new ColoredTextList<float>("{0:0.}", data.Mass, data.MaxTurnRate) : new ColoredTextList();
                default:
                    return base.MakeInstance_ColoredTextList(intelLevel, key);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

