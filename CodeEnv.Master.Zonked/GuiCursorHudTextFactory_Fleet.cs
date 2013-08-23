// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudTextFactory_Fleet.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Fleets.
    /// </summary>
    [Obsolete]
    public class GuiCursorHudTextFactory_Fleet : AGuiCursorHudTextFactory {

        public GuiCursorHudTextFactory_Fleet(FleetData data, IntelLevel intelLevel) : base(data, intelLevel) { }

        public override IColoredTextList MakeInstance(GuiHudLineKeys key, float currentSpeed = -1F) {
            if (!ValidateKeyAgainstIntelLevel(key)) {
                return _emptyIColoredTextList;
            }
            FleetData data = _data as FleetData;
            switch (key) {
                case GuiHudLineKeys.Speed:
                    return new ColoredTextList_Speed(currentSpeed, data.MaxSpeed);  // fleet will always display speed, even if zero
                case GuiHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);
                case GuiHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Strength.Combined);
                case GuiHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.Strength);
                case GuiHudLineKeys.Composition:
                    return new ColoredTextList_Composition(data.Composition);
                case GuiHudLineKeys.CompositionDetails:
                    // TODO
                    return new ColoredTextList_Composition(data.Composition);
                default:
                    return base.MakeInstance(key);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

