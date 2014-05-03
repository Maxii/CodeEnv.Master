﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseGuiHudTextFactory.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for Starbases.
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
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for Starbases.
    /// </summary>
    public class StarbaseGuiHudTextFactory : AGuiHudTextFactory<StarbaseGuiHudTextFactory, StarbaseCmdData> {

        private StarbaseGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, IIntel intel, StarbaseCmdData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    return new ColoredTextList_String(data.ParentName);   // the name of the Starbase is the parentName of the Command
                case GuiHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    return new ColoredTextList_Intel(intel);
                case GuiHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiHudLineKeys.Health:
                    return new ColoredTextList_Health(data.UnitHealth, data.UnitMaxHitPoints);
                case GuiHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Strength.Combined);
                case GuiHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.Strength);
                case GuiHudLineKeys.Category:
                    return new ColoredTextList_String(data.Category.GetName(), data.Category.GetDescription());

                case GuiHudLineKeys.Composition:
                //return new ColoredTextList_Composition(data.Composition); // FIXME ColoredTextList_Composition is Fleet-specific
                case GuiHudLineKeys.CompositionDetails:
                // TODO
                //return new ColoredTextList_Composition(data.Composition);

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.ParentName: // bases do not have parent names
                case GuiHudLineKeys.Speed:
                case GuiHudLineKeys.Capacity:
                case GuiHudLineKeys.Resources:
                case GuiHudLineKeys.Specials:
                case GuiHudLineKeys.SettlementDetails:
                case GuiHudLineKeys.SectorIndex:
                case GuiHudLineKeys.Density:
                case GuiHudLineKeys.ShipDetails:
                    return _emptyColoredTextList;

                case GuiHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

    }
}

