// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetGuiHudTextFactory.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Fleets.
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
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Fleets.
    /// </summary>
    public class FleetGuiHudTextFactory : AGuiHudTextFactory<FleetGuiHudTextFactory, FleetCmdData> {

        private FleetGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, FleetCmdData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.ParentName); // the name of the Fleet is the parentName of the Command
                case GuiHudLineKeys.CameraDistance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    //return new ColoredTextList_Intel(data.PlayerIntel);
                    return new ColoredTextList_Intel(data.HumanPlayerIntel());
                case GuiHudLineKeys.Speed:
                    return new ColoredTextList_Speed(data.CurrentSpeed, data.RequestedSpeed);  // fleet will always display speed, even if zero
                case GuiHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiHudLineKeys.Health:
                    return new ColoredTextList_Health(data.UnitHealth, data.UnitMaxHitPoints);
                case GuiHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.UnitOffensiveStrength.Combined + data.UnitDefensiveStrength.Combined);
                case GuiHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.UnitOffensiveStrength, data.UnitDefensiveStrength);
                case GuiHudLineKeys.Category:
                    return new ColoredTextList_String(data.Category.GetValueName(), data.Category.GetEnumAttributeText());
                case GuiHudLineKeys.Composition:
                    //return new ColoredTextList_Composition(data.Composition);
                    return new ColoredTextList_String(data.UnitComposition.ToString());
                case GuiHudLineKeys.CompositionDetails:
                    // TODO
                    //return new ColoredTextList_Composition(data.Composition);
                    return new ColoredTextList_String(data.UnitComposition.ToString());
                case GuiHudLineKeys.TargetName:
                    return data.Target != null ? new ColoredTextList_String(data.Target.FullName) : _emptyTextList;
                case GuiHudLineKeys.TargetDistance:
                    return data.Target != null ? new ColoredTextList_Distance(data.Position, data.Target.Position) : _emptyTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.ParentName: // fleets do not have parent names
                case GuiHudLineKeys.Capacity:
                case GuiHudLineKeys.Resources:
                case GuiHudLineKeys.Specials:
                case GuiHudLineKeys.SettlementDetails:
                case GuiHudLineKeys.SectorIndex:
                case GuiHudLineKeys.Density:
                case GuiHudLineKeys.ShipDetails:
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

