// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NewSystemGuiHudTextFactory.cs
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

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, AIntel intel, SystemData data) {
            var settleData = data.SettlementData;
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.Name);
                case GuiHudLineKeys.SectorIndex:
                    return new ColoredTextList_String(data.SectorIndex.ToString());
                case GuiHudLineKeys.CameraDistance:
                    return new ColoredTextList_Distance();
                case GuiHudLineKeys.IntelState:
                    return new ColoredTextList_Intel(intel);
                case GuiHudLineKeys.Owner:
                    return data.Owner != TempGameValues.NoPlayer ? new ColoredTextList_Owner(data.Owner) : _emptyTextList;
                case GuiHudLineKeys.Health:
                    return (settleData != null) ? new ColoredTextList_Health(settleData.UnitHealth, settleData.MaxHitPoints) : _emptyTextList;
                case GuiHudLineKeys.CombatStrength:
                    return (settleData != null) ? new ColoredTextList<float>(Constants.FormatFloat_0Dp, settleData.UnitOffensiveStrength.Combined + settleData.UnitDefensiveStrength.Combined) : _emptyTextList;
                case GuiHudLineKeys.CombatStrengthDetails:
                    return (settleData != null) ? new ColoredTextList_Combat(settleData.UnitOffensiveStrength, settleData.UnitDefensiveStrength) : _emptyTextList;
                case GuiHudLineKeys.Capacity:
                    return new ColoredTextList<int>(Constants.FormatInt_2DMin, data.Capacity);
                case GuiHudLineKeys.Resources:
                    return new ColoredTextList_Resources(data.Resources);
                case GuiHudLineKeys.Specials:
                    return (data.SpecialResources != TempGameValues.NoSpecialResources) ? new ColoredTextList_Specials(data.SpecialResources)
                        : _emptyTextList;
                case GuiHudLineKeys.Category:
                    return (settleData != null) ? new ColoredTextList_String(settleData.Category.GetName(), settleData.Category.GetDescription()) : _emptyTextList;
                case GuiHudLineKeys.SettlementDetails:
                    return (settleData != null) ? new ColoredTextList_Settlement(settleData) : _emptyTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.ParentName: // systems do not have parent names
                case GuiHudLineKeys.Composition:
                case GuiHudLineKeys.CompositionDetails:
                case GuiHudLineKeys.ShipDetails:
                case GuiHudLineKeys.Density:
                case GuiHudLineKeys.Speed:
                case GuiHudLineKeys.TargetName:
                case GuiHudLineKeys.TargetDistance:
                    return _emptyTextList;

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

