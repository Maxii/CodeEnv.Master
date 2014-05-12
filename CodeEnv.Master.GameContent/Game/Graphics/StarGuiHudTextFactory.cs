// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarGuiHudTextFactory.cs
//  Factory that makes GuiCursorHudText and IColoredTextList instances for Stars.
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
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Stars.
    /// </summary>
    public class StarGuiHudTextFactory : AGuiHudTextFactory<StarGuiHudTextFactory, StarData> {

        private StarGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, IIntel intel, StarData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.Name);
                case GuiHudLineKeys.ParentName: // a rouge star may not have a parent
                    return data.ParentName != string.Empty ? new ColoredTextList_String(data.ParentName) : _emptyColoredTextList;
                case GuiHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    return new ColoredTextList_Intel(intel);
                case GuiHudLineKeys.Category:
                    return new ColoredTextList_String(data.Category.GetName(), data.Category.GetDescription());
                case GuiHudLineKeys.Capacity:
                    return new ColoredTextList<int>(Constants.FormatInt_2DMin, data.Capacity);
                case GuiHudLineKeys.Resources:
                    return new ColoredTextList_Resources(data.Resources);
                case GuiHudLineKeys.Specials:
                    return (data.SpecialResources != TempGameValues.NoSpecialResourceYield) ? new ColoredTextList_Specials(data.SpecialResources)
                        : _emptyColoredTextList;
                case GuiHudLineKeys.Owner:
                    return data.Owner != TempGameValues.NoPlayer ? new ColoredTextList_Owner(data.Owner) : _emptyColoredTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.Health:
                case GuiHudLineKeys.CombatStrength:
                case GuiHudLineKeys.CombatStrengthDetails:
                case GuiHudLineKeys.SettlementDetails:
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

