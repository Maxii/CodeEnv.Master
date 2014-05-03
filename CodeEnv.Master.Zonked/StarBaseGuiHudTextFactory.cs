﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBaseGuiHudTextFactory.cs
// Factory that makes GuiCursorHudText and IColoredTextList instances for StarBases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Factory that makes GuiCursorHudText and IColoredTextList instances for StarBases.
    /// </summary>
    public class StarBaseGuiHudTextFactory : AGuiHudTextFactory<StarBaseGuiHudTextFactory, StarBaseData> {

        private StarBaseGuiHudTextFactory() {
            Initialize();
        }

        protected override IColoredTextList MakeTextInstance(GuiHudLineKeys key, Intel intel, StarBaseData data) {
            switch (key) {
                case GuiHudLineKeys.Name:
                    // ships donot show name if IntelScope is simply just Aware
                    return intel.CurrentCoverage != IntelCoverage.Aware ? new ColoredTextList_String(data.Name) : _emptyColoredTextList;
                case GuiHudLineKeys.ParentName:
                    return data.ParentName != string.Empty ? new ColoredTextList_String(data.ParentName) : _emptyColoredTextList;
                case GuiHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiHudLineKeys.IntelState:
                    return (intel.DateStamp != null) ? new ColoredTextList_Intel(intel) : _emptyColoredTextList;
                case GuiHudLineKeys.Category:
                    return new ColoredTextList_String(data.Size.GetDescription());
                case GuiHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);

                case GuiHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Strength.Combined);
                case GuiHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.Strength);

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiHudLineKeys.SettlementDetails:
                case GuiHudLineKeys.Capacity:
                case GuiHudLineKeys.Resources:
                case GuiHudLineKeys.Specials:
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

