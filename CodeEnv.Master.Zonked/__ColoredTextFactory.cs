// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: __ColoredTextFactory.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    [Obsolete]
    public class __ColoredTextFactory {

        public __ColoredTextFactory() { }

        public IList<ColoredText> CreateColoredTextList(GuiHudLineKeys key, params System.Object[] args) {
            ColoredText coloredText = null;
            IList<ColoredText> coloredTextList = null;
            switch (key) {
                case GuiHudLineKeys.ParentName:
                    Arguments.ValidateTypeAndLength<string>(1, args);
                    coloredText = new ColoredText(args[0] as string);
                    break;
                case GuiHudLineKeys.Distance:
                    // TODO calculate from SystemData.Position and <code>static GetSelected()<code>
                    Arguments.ValidateTypeAndLength<float>(1, args);
                    string distance = String.Format("{0:0.#}", args[0]);    // {0:0.#}     float with max one decimal place, rounded
                    coloredText = new ColoredText(distance);
                    break;
                case GuiHudLineKeys.Capacity:
                    Arguments.ValidateTypeAndLength<int>(1, args);
                    string capacity = String.Format("{0:00}", args[0]); // {0:00}     int with at least two digits
                    coloredText = new ColoredText(capacity);
                    break;
                case GuiHudLineKeys.Resources:
                    Arguments.ValidateTypeAndLength<OpeYield>(1, args);
                    OpeYield ope = (OpeYield)args[0];
                    string o = String.Format("{0:0.}", ope.Organics);  // {0:0.}    float with zero decimal places, rounded
                    string p = String.Format("{0:0.}", ope.Particulates);
                    string e = String.Format("{0:0.}", ope.Energy);
                    coloredTextList = new List<ColoredText> {
                                new ColoredText(o),
                                new ColoredText(p),
                                new ColoredText(e)
                            };
                    break;
                case GuiHudLineKeys.Specials:
                    // TODO how to handle variable number of XResources
                    Arguments.ValidateTypeAndLength<XYield>(1, args[0]);
                    XYield x = (XYield)args[0];
                    IList<XYield.XResourceValuePair> allX = x.GetAllResources();

                    string resourceName = allX[0].Resource.GetName();
                    float resourceYield = allX[0].Value;
                    string resourceValue = String.Format("{0:0.}", resourceYield);    // {0:0.}  float with zero decimal places, rounded

                    coloredTextList = new List<ColoredText> {
                                new ColoredText(resourceName),
                                new ColoredText(resourceValue)
                            };
                    break;
                case GuiHudLineKeys.IntelState:
                    // TODO fill out from HumanPlayer.GetIntelState(System) - need last intel date too
                    Arguments.ValidateTypeAndLength<IntelLevel>(1, args[0]);
                    Arguments.ValidateTypeAndLength<GameTimePeriod>(1, args[1]);
                    IntelLevel intelLevel = (IntelLevel)args[0];
                    GameTimePeriod intelAge = (GameTimePeriod)args[1];
                    string intelText = ConstructIntelText(intelLevel, intelAge);
                    coloredText = new ColoredText(intelText);
                    break;
                case GuiHudLineKeys.Owner:
                    Arguments.ValidateTypeAndLength<Players>(1, args);
                    Players player = (Players)args[0];
                    coloredText = new ColoredText(player.GetName(), player.PlayerColor());
                    break;
                case GuiHudLineKeys.Health:
                    Arguments.ValidateTypeAndLength<float>(2, args);
                    float healthValue = (float)args[0]; float maxHpValue = (float)args[1];
                    float healthRatio = healthValue / maxHpValue;
                    Color healthColor = (healthRatio > TempGameValues.InjuredHealthThreshold) ? Color.green :
                                ((healthRatio > TempGameValues.CriticalHealthThreshold) ? Color.yellow : Color.red);
                    string health = String.Format("{0:0.}", healthValue);  // {0:0.}    float with zero decimal places, rounded
                    string maxHp = String.Format("{0:0.}", maxHpValue);
                    coloredTextList = new List<ColoredText> {
                                new ColoredText(health, healthColor),
                                new ColoredText(maxHp, Color.green)
                            };
                    break;
                case GuiHudLineKeys.CombatStrength:
                    Arguments.ValidateTypeAndLength<float>(1, args);
                    string combatStrenth = String.Format("{0:0.}", args[0]);       // {0:0.}    float with zero decimal places, rounded
                    coloredText = new ColoredText(combatStrenth);
                    break;
                case GuiHudLineKeys.Speed:
                    // TODO                                                                         // {0:00.0}     float with at least two digits before the one decimal place, rounded
                    Arguments.ValidateTypeAndLength<float>(1, args);
                    string speed = String.Format("{0:00.0}", args[0]);
                    coloredText = new ColoredText(speed);
                    break;
                case GuiHudLineKeys.Composition:
                    // TODO
                    Arguments.ValidateTypeAndLength<string>(1, args);
                    coloredText = new ColoredText(args[0] as string);
                    break;
                case GuiHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
            if (coloredText != null) {
                coloredTextList = new List<ColoredText> { coloredText };
            }
            if (coloredTextList == null) {
                D.Error("List of {0} cannot be null.", typeof(ColoredText));
            }
            return coloredTextList;
        }

        private string ConstructIntelText(IntelLevel intelLevel, GameTimePeriod intelAge) {
            string intelMsg = intelLevel.GetName();
            switch (intelLevel) {
                case IntelLevel.Unknown:
                case IntelLevel.LongRangeSensors:
                case IntelLevel.ShortRangeSensors:
                    // no data to add
                    break;
                case IntelLevel.OutOfRange:
                    string addendum = String.Format("Last Intel {0} days ago.", intelAge.FormattedPeriod);
                    intelMsg = intelMsg + addendum;
                    break;
                case IntelLevel.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(intelLevel));
            }
            return intelMsg;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

