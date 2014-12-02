// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetGuiHudTextFactory.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Planets.
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
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Planets.
    /// </summary>
    public class PlanetGuiHudTextFactory : AGuiHudTextFactory<PlanetGuiHudTextFactory, PlanetData> {

        private PlanetGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, AIntel intel, PlanetData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.Name);
                case GuiHudLineKeys.ParentName:
                    return data.ParentName != string.Empty ? new ColoredTextList_String(data.ParentName) : _emptyTextList;
                case GuiHudLineKeys.CameraDistance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    return new ColoredTextList_Intel(intel);
                case GuiHudLineKeys.Category:
                    return new ColoredTextList_String(data.Category.GetName(), data.Category.GetDescription());
                case GuiHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);
                case GuiHudLineKeys.Capacity:
                    return new ColoredTextList<int>(Constants.FormatInt_2DMin, data.Capacity);
                case GuiHudLineKeys.Resources:
                    return new ColoredTextList_Resources(data.Resources);
                case GuiHudLineKeys.Specials:
                    return (data.SpecialResources != TempGameValues.NoSpecialResources) ? new ColoredTextList_Specials(data.SpecialResources)
                        : _emptyTextList;
                case GuiHudLineKeys.Owner:
                    return data.Owner != TempGameValues.NoPlayer ? new ColoredTextList_Owner(data.Owner) : _emptyTextList;
                case GuiHudLineKeys.Speed:
                    return new ColoredTextList_Speed(data.OrbitalSpeed);

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.CombatStrength:
                case GuiHudLineKeys.CombatStrengthDetails:
                case GuiHudLineKeys.SettlementDetails:
                case GuiHudLineKeys.Composition:
                case GuiHudLineKeys.CompositionDetails:
                case GuiHudLineKeys.ShipDetails:
                case GuiHudLineKeys.SectorIndex:
                case GuiHudLineKeys.Density:
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

