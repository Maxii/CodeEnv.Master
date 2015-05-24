// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipGuiHudTextFactory.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Ships.
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
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Ships.
    /// </summary>
    public class ShipGuiHudTextFactory : AGuiHudTextFactory<ShipGuiHudTextFactory, ShipData> {

        private ShipGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, ShipData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.Name);
                case GuiHudLineKeys.ParentName:
                    return new ColoredTextList_String(data.ParentName);
                case GuiHudLineKeys.CameraDistance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    //return new ColoredTextList_Intel(data.PlayerIntel);
                    return new ColoredTextList_Intel(data.HumanPlayerIntel());
                case GuiHudLineKeys.Speed:
                    return new ColoredTextList_Speed(data.CurrentSpeed, data.RequestedSpeed);
                case GuiHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);
                case GuiHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.OffensiveStrength.Combined + data.DefensiveStrength.Combined);
                case GuiHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.OffensiveStrength, data.DefensiveStrength);
                case GuiHudLineKeys.Category:
                    return new ColoredTextList_String(data.Category.GetName(), data.Category.GetEnumAttributeText());
                case GuiHudLineKeys.ShipDetails:
                    return new ColoredTextList_Ship(data);
                case GuiHudLineKeys.TargetName:
                    return data.Target != null ? new ColoredTextList_String(data.Target.FullName) : _emptyTextList;
                case GuiHudLineKeys.TargetDistance:
                    return data.Target != null ? new ColoredTextList_Distance(data.Position, data.Target.Position) : _emptyTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.Capacity:
                case GuiHudLineKeys.Resources:
                case GuiHudLineKeys.Specials:
                case GuiHudLineKeys.Composition:
                case GuiHudLineKeys.CompositionDetails:
                case GuiHudLineKeys.SectorIndex:
                case GuiHudLineKeys.Density:
                case GuiHudLineKeys.SettlementDetails:
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

