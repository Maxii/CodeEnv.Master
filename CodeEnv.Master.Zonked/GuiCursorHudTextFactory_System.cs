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
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Systems.
    /// </summary>
    [Obsolete]
    public class GuiCursorHudTextFactory_System : AGuiCursorHudTextFactory {

        public GuiCursorHudTextFactory_System(SystemData data, IntelLevel intelLevel) : base(data, intelLevel) { }

        public override IColoredTextList MakeInstance(GuiCursorHudLineKeys key, float currentSpeed = -1F) {
            if (!ValidateKeyAgainstIntelLevel(key)) {
                return _emptyIColoredTextList;
            }
            SystemData data = _data as SystemData;
            switch (key) {
                case GuiCursorHudLineKeys.Speed:
                    return currentSpeed > Constants.ZeroF ? new ColoredTextList_Speed(currentSpeed) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Owner:
                    return (data.Settlement != null) ? new ColoredTextList_Owner(data.Settlement.Owner) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Health:
                    return (data.Settlement != null) ? new ColoredTextList_Health(data.Settlement.Health, data.Settlement.MaxHitPoints) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.CombatStrength:
                    return (data.Settlement != null) ? new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Settlement.Strength.Combined) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                    return (data.Settlement != null) ? new ColoredTextList_Combat(data.Settlement.Strength) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Capacity:
                    return new ColoredTextList<int>(Constants.FormatInt_2DMin, data.Capacity);
                case GuiCursorHudLineKeys.Resources:
                    return new ColoredTextList_Resources(data.Resources);
                case GuiCursorHudLineKeys.Specials:
                    return (data.SpecialResources != null) ? new ColoredTextList_Specials(data.SpecialResources) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.SettlementSize:
                    return (data.Settlement != null) ? new ColoredTextList_String(data.Settlement.SettlementSize.GetName()) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.SettlementDetails:
                    return (data.Settlement != null) ? new ColoredTextList_Settlement(data.Settlement) : _emptyIColoredTextList;
                default:
                    return base.MakeInstance(key);
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

