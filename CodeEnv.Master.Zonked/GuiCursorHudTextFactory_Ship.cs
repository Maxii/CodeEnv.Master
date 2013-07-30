﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudTextFactory_Ship.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Ships.
    /// </summary>
    [Obsolete]
    public class GuiCursorHudTextFactory_Ship : AGuiCursorHudTextFactory {

        public GuiCursorHudTextFactory_Ship(ShipData data, IntelLevel intelLevel) : base(data, intelLevel) { }

        public override IColoredTextList MakeInstance(GuiCursorHudLineKeys key, float currentSpeed = -1F) {
            if (!ValidateKeyAgainstIntelLevel(key)) {
                return _emptyIColoredTextList;
            }
            ShipData data = _data as ShipData;
            switch (key) {
                case GuiCursorHudLineKeys.Speed:
                    return new ColoredTextList_Speed(currentSpeed, data.MaxSpeed);
                case GuiCursorHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiCursorHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);  // ship should always have MaxHitPoints, even if health is virtually zero
                case GuiCursorHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Strength.Combined);  // ship should always have CombatStrength even if health is virtually zero
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.Strength);  // ship should always have CombatStrength even if health is virtually zero
                case GuiCursorHudLineKeys.ShipSize:
                    return new ColoredTextList_String(data.Hull.GetDescription());
                case GuiCursorHudLineKeys.ShipDetails:
                    return new ColoredTextList_Ship(data);
                default:
                    return base.MakeInstance(key);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

