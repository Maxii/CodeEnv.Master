﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemGuiHudTextFactory.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Systems.
    /// </summary>
    public class SystemGuiHudTextFactory : AGuiHudTextFactory<SystemGuiHudTextFactory, SystemData> {

        private SystemGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, IIntel intel, SystemData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.Name);
                case GuiHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    return new ColoredTextList_Intel(intel);
                case GuiHudLineKeys.Owner:
                    return data.Owner != null ? new ColoredTextList_Owner(data.Owner) : _emptyColoredTextList;
                case GuiHudLineKeys.Health:
                    return (data.SettlementData != null) ? new ColoredTextList_Health(data.SettlementData.UnitHealth, data.SettlementData.MaxHitPoints) : _emptyColoredTextList;
                case GuiHudLineKeys.CombatStrength:
                    return (data.SettlementData != null) ? new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.SettlementData.Strength.Combined) : _emptyColoredTextList;
                case GuiHudLineKeys.CombatStrengthDetails:
                    return (data.SettlementData != null) ? new ColoredTextList_Combat(data.SettlementData.Strength) : _emptyColoredTextList;
                case GuiHudLineKeys.Capacity:
                    return new ColoredTextList<int>(Constants.FormatInt_2DMin, data.Capacity);
                case GuiHudLineKeys.Resources:
                    return new ColoredTextList_Resources(data.Resources);
                case GuiHudLineKeys.Specials:
                    return (data.SpecialResources != null) ? new ColoredTextList_Specials(data.SpecialResources) : _emptyColoredTextList;
                case GuiHudLineKeys.Category:
                    return (data.SettlementData != null) ? new ColoredTextList_String(data.SettlementData.Category.GetName(), data.SettlementData.Category.GetDescription()) : _emptyColoredTextList;
                case GuiHudLineKeys.SettlementDetails:
                    return (data.SettlementData != null) ? new ColoredTextList_Settlement(data.SettlementData) : _emptyColoredTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.ParentName: // systems do not have parent names
                case GuiHudLineKeys.Composition:
                case GuiHudLineKeys.CompositionDetails:
                case GuiHudLineKeys.ShipDetails:
                case GuiHudLineKeys.SectorIndex:
                case GuiHudLineKeys.Density:
                case GuiHudLineKeys.Speed:
                    return _emptyColoredTextList;

                case GuiHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

