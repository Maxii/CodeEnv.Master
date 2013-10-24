// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: __GuiCursorHudTextFactory.cs
// Factory that makes GuiCursorHudText instances.
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

    /// <summary>
    /// Factory that makes GuiCursorHudText instances.
    /// </summary>
    [Obsolete]
    public class __GuiCursorHudTextFactory {

        private static IDictionary<IntelLevel, IList<GuiHudLineKeys>> _hudLineKeyLookup = new Dictionary<IntelLevel, IList<GuiHudLineKeys>> {

        {IntelLevel.Unknown, new List<GuiHudLineKeys> { GuiHudLineKeys.ParentName,
                                                                       GuiHudLineKeys.IntelState,
                                                                       GuiHudLineKeys.Distance }},

        {IntelLevel.OutOfRange, new List<GuiHudLineKeys> {   GuiHudLineKeys.ParentName,
                                                                       GuiHudLineKeys.Capacity,
                                                                       GuiHudLineKeys.Resources,
                                                                       GuiHudLineKeys.Specials,
                                                                       GuiHudLineKeys.IntelState,
                                                                       GuiHudLineKeys.Distance }}
    };

        private IDictionary<IntelLevel, GuiHudText> _guiCursorHudTextLookup;

        private SystemData _data;

        public __GuiCursorHudTextFactory(SystemData data) {
            _data = data;
        }

        /// <summary>
        /// Makes or acquires an instance of GuiCursorHudText for the indicated IntelLevel.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>   
        /// <returns></returns>
        public GuiHudText MakeInstance_GuiCursorHudText(IntelLevel intelLevel) {
            if (_guiCursorHudTextLookup == null) {
                _guiCursorHudTextLookup = new Dictionary<IntelLevel, GuiHudText>();
            }

            if (_guiCursorHudTextLookup.ContainsKey(intelLevel)) {
                return _guiCursorHudTextLookup[intelLevel];
            }

            IList<GuiHudLineKeys> keys;
            if (_hudLineKeyLookup.TryGetValue(intelLevel, out keys)) {
                GuiHudText guiCursorHudText = new GuiHudText(intelLevel);

                foreach (GuiHudLineKeys key in keys) {
                    IList<ColoredText> coloredTextList = MakeInstance_ColoredTextList(intelLevel, key);
                    guiCursorHudText.Add(key, coloredTextList);
                }
                _guiCursorHudTextLookup.Add(intelLevel, guiCursorHudText);
                return guiCursorHudText;
            }
            D.Error("{0} {1} Key is not present in Lookup.", typeof(IntelLevel), intelLevel);
            return null;
        }

        /// <summary>
        /// Makes a new instance of ColoredTextList for the provided LineKey.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IList<ColoredText> MakeInstance_ColoredTextList(IntelLevel intelLevel, GuiHudLineKeys key) {
            ColoredText coloredText = null;
            IList<ColoredText> coloredTextList = null;
            switch (key) {
                case GuiHudLineKeys.ParentName:
                    coloredText = new ColoredText(_data.OptionalParentName);
                    break;
                case GuiHudLineKeys.Distance:
                    // TODO calculate from SystemData.Position and <code>static GetSelected()<code>
                    float distance = Vector3.Distance(_data.Position, TempGameValues.UniverseOrigin);
                    string distance_formatted = String.Format("{0:0.#}", distance);    // {0:0.#}     float with max one decimal place, rounded
                    coloredText = new ColoredText(distance_formatted);
                    break;
                case GuiHudLineKeys.Capacity:
                    string capacity_formatted = String.Format("{0:00}", _data.Capacity); // {0:00}     int with at least two digits
                    coloredText = new ColoredText(capacity_formatted);
                    break;
                case GuiHudLineKeys.Resources:
                    OpeYield ope = _data.Resources;
                    string organics_formatted = String.Format("{0:0.}", ope.Organics);  // {0:0.}    float with zero decimal places, rounded
                    string particulates_formatted = String.Format("{0:0.}", ope.Particulates);
                    string energy_formatted = String.Format("{0:0.}", ope.Energy);
                    coloredTextList = new List<ColoredText> {
                                new ColoredText(organics_formatted),
                                new ColoredText(particulates_formatted),
                                new ColoredText(energy_formatted)
                            };
                    break;
                case GuiHudLineKeys.Specials:
                    // TODO how to handle variable number of XResources
                    XYield x = _data.SpecialResources;
                    IList<XYield.XResourceValuePair> allX = x.GetAllResources();

                    string resourceName = allX[0].Resource.GetName();
                    float resourceYield = allX[0].Value;
                    string resourceYield_formatted = String.Format("{0:0.}", resourceYield);    // {0:0.}  float with zero decimal places, rounded

                    coloredTextList = new List<ColoredText> {
                                new ColoredText(resourceName),
                                new ColoredText(resourceYield_formatted)
                            };
                    break;
                case GuiHudLineKeys.IntelState:
                    // TODO fill out from HumanPlayer.GetIntelState(System) - need last intel date too
                    GameTimePeriod intelAge = new GameTimePeriod(_data.LastHumanPlayerIntelDate, GameTime.Date);
                    string intelText_formatted = ConstructIntelText(intelLevel, intelAge);
                    coloredText = new ColoredText(intelText_formatted);
                    break;
                case GuiHudLineKeys.Owner:
                    Players player = _data.Owner;
                    coloredText = new ColoredText(player.GetName(), player.PlayerColor());
                    break;
                case GuiHudLineKeys.Health:
                    float health = _data.Health; float maxHp = _data.MaxHitPoints;
                    float healthRatio = health / maxHp;
                    Color healthColor = (healthRatio > TempGameValues.InjuredHealthThreshold) ? Color.green :
                                ((healthRatio > TempGameValues.CriticalHealthThreshold) ? Color.yellow : Color.red);
                    string health_formatted = String.Format("{0:0.}", health);  // {0:0.}    float with zero decimal places, rounded
                    string maxHp_formatted = String.Format("{0:0.}", maxHp);
                    coloredTextList = new List<ColoredText> {
                                new ColoredText(health_formatted, healthColor),
                                new ColoredText(maxHp_formatted, Color.green)
                            };
                    break;
                case GuiHudLineKeys.CombatStrength:
                    string combatStrenth_formatted = String.Format("{0:0.}", _data.CombatStrength);       // {0:0.}    float with zero decimal places, rounded
                    coloredText = new ColoredText(combatStrenth_formatted);
                    break;
                case GuiHudLineKeys.Speed:
                    // TODO
                    float testSpeed = 23.5F;
                    string speed_formatted = String.Format("{0:00.0}", testSpeed);    // {0:00.0}     float with at least two digits before the one decimal place, rounded
                    coloredText = new ColoredText(speed_formatted);
                    break;
                case GuiHudLineKeys.Composition:
                    // TODO                        
                    coloredText = new ColoredText("Composition Test");
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
                    string addendum = String.Format(". Last Intel {0} days ago.", intelAge.FormattedPeriod);
                    intelMsg = intelMsg + addendum;
                    break;
                case IntelLevel.Nil:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(intelLevel));
            }
            return intelMsg;
        }



        //[Obsolete]
        //public IList<ColoredText> MakeInstance_ColoredTextList(IntelLevel intelLevel, GuiCursorHudDisplayLineKeys key) {
        //    IList<ColoredText> coloredTextList = null;
        //    switch (key) {
        //        case GuiCursorHudDisplayLineKeys.Name:
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.SystemName);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Distance:
        //            // TODO calculate from SystemData.Position and <code>static GetSelected()<code>
        //            float distanceFromOrigin = Vector3.Distance(_data.Position, TempGameValues.UniverseOrigin);
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, distanceFromOrigin);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Capacity:
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.Capacity);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Resources:
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.Resources);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Specials:
        //            // TODO how to handle variable number of XResources
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.SpecialResources);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.IntelState:
        //            // TODO fill out from HumanPlayer.GetIntelState(System) - need last intel date too
        //            GameTimePeriod intelAge = new GameTimePeriod(_data.DateHumanPlayerExplored, GameTime.Date);
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, intelLevel, intelAge);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Owner:
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.Owner);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Health:
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.Health, _data.MaxHitPoints);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.CombatStrength:
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, _data.CombatStrength);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Speed:
        //            // TODO
        //            float testSpeed = 23.5F;
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, testSpeed);
        //            break;
        //        case GuiCursorHudDisplayLineKeys.Composition:
        //            // TODO                        
        //            coloredTextList = _coloredTextFactory.CreateColoredTextList(key, "CompositionTest");
        //            break;
        //        case GuiCursorHudDisplayLineKeys.None:
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
        //    }
        //    return coloredTextList;
        //}

        //[Obsolete]
        //    private IList<ColoredText> CreateColoredTextList(GuiCursorHudDisplayLineKeys key, params System.Object[] args) {
        //        ColoredText coloredText = null;
        //        IList<ColoredText> coloredTextList = null;
        //        switch (key) {
        //            case GuiCursorHudDisplayLineKeys.Name:
        //                Arguments.ValidateTypeAndLength<string>(1, args);
        //                coloredText = new ColoredText(args[0] as string);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Distance:
        //                // TODO calculate from SystemData.Position and <code>static GetSelected()<code>
        //                Arguments.ValidateTypeAndLength<float>(1, args);
        //                string distance = String.Format("{0:0.#}", args[0]);    // {0:0.#}     float with max one decimal place, rounded
        //                coloredText = new ColoredText(distance);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Capacity:
        //                Arguments.ValidateTypeAndLength<int>(1, args);
        //                string capacity = String.Format("{0:00}", args[0]); // {0:00}     int with at least two digits
        //                coloredText = new ColoredText(capacity);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Resources:
        //                Arguments.ValidateTypeAndLength<OpeYield>(1, args);
        //                OpeYield ope = (OpeYield)args[0];
        //                string o = String.Format("{0:0.}", ope.Organics);  // {0:0.}    float with zero decimal places, rounded
        //                string p = String.Format("{0:0.}", ope.Particulates);
        //                string e = String.Format("{0:0.}", ope.Energy);
        //                coloredTextList = new List<ColoredText> {
        //                            new ColoredText(o),
        //                            new ColoredText(p),
        //                            new ColoredText(e)
        //                        };
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Specials:
        //                // TODO how to handle variable number of XResources
        //                Arguments.ValidateTypeAndLength<XYield>(1, args[0]);
        //                XYield x = (XYield)args[0];
        //                IList<XYield.XResourceValuePair> allX = x.GetAllResources();

        //                string resourceName = allX[0].Resource.GetName();
        //                float resourceYield = allX[0].Value;
        //                string resourceValue = String.Format("{0:0.}", resourceYield);    // {0:0.}  float with zero decimal places, rounded

        //                coloredTextList = new List<ColoredText> {
        //                            new ColoredText(resourceName),
        //                            new ColoredText(resourceValue)
        //                        };
        //                break;
        //            case GuiCursorHudDisplayLineKeys.IntelState:
        //                // TODO fill out from HumanPlayer.GetIntelState(System) - need last intel date too
        //                Arguments.ValidateTypeAndLength<IntelLevel>(1, args[0]);
        //                Arguments.ValidateTypeAndLength<GameTimePeriod>(1, args[1]);
        //                IntelLevel intelLevel = (IntelLevel)args[0];
        //                GameTimePeriod intelAge = (GameTimePeriod)args[1];
        //                string intelText = ConstructIntelText(intelLevel, intelAge);
        //                coloredText = new ColoredText(intelText);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Owner:
        //                Arguments.ValidateTypeAndLength<Players>(1, args);
        //                Players player = (Players)args[0];
        //                coloredText = new ColoredText(player.GetName(), player.PlayerColor());
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Health:
        //                Arguments.ValidateTypeAndLength<float>(2, args);
        //                float healthValue = (float)args[0]; float maxHpValue = (float)args[1];
        //                float healthRatio = healthValue / maxHpValue;
        //                Color healthColor = (healthRatio > TempGameValues.InjuredHealthThreshold) ? Color.green :
        //                            ((healthRatio > TempGameValues.CriticalHealthThreshold) ? Color.yellow : Color.red);
        //                string health = String.Format("{0:0.}", healthValue);  // {0:0.}    float with zero decimal places, rounded
        //                string maxHp = String.Format("{0:0.}", maxHpValue);
        //                coloredTextList = new List<ColoredText> {
        //                            new ColoredText(health, healthColor),
        //                            new ColoredText(maxHp, Color.green)
        //                        };
        //                break;
        //            case GuiCursorHudDisplayLineKeys.CombatStrength:
        //                Arguments.ValidateTypeAndLength<float>(1, args);
        //                string combatStrenth = String.Format("{0:0.}", args[0]);       // {0:0.}    float with zero decimal places, rounded
        //                coloredText = new ColoredText(combatStrenth);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Speed:
        //                // TODO                                                                         // {0:00.0}     float with at least two digits before the one decimal place, rounded
        //                Arguments.ValidateTypeAndLength<float>(1, args);
        //                string speed = String.Format("{0:00.0}", args[0]);
        //                coloredText = new ColoredText(speed);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.Composition:
        //                // TODO
        //                Arguments.ValidateTypeAndLength<string>(1, args);
        //                coloredText = new ColoredText(args[0] as string);
        //                break;
        //            case GuiCursorHudDisplayLineKeys.None:
        //            default:
        //                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
        //        }
        //        if (coloredText != null) {
        //            coloredTextList = new List<ColoredText> { coloredText };
        //        }
        //        if (coloredTextList == null) {
        //            D.Error("List of {0} cannot be null.", typeof(ColoredText));
        //        }
        //        return coloredTextList;
        //    }


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

