// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiCursorHudTextFactory.cs
// Static factory class that makes GuiCursorHudText and IColoredTextList instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;


    /// <summary>
    /// Static factory class that makes GuiCursorHudText and IColoredTextList instances.
    /// </summary>
    public static class GuiCursorHudTextFactory {

        private static IDictionary<IntelLevel, IList<GuiCursorHudLineKeys>> _hudLineKeyLookup = new Dictionary<IntelLevel, IList<GuiCursorHudLineKeys>> {

        {IntelLevel.Unknown, new List<GuiCursorHudLineKeys> { GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                       GuiCursorHudLineKeys.Distance }},

        {IntelLevel.OutOfDate, new List<GuiCursorHudLineKeys> {   GuiCursorHudLineKeys.ItemName,
                                                                       GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                       GuiCursorHudLineKeys.Capacity,
                                                                       GuiCursorHudLineKeys.Resources,
                                                                       GuiCursorHudLineKeys.Specials,
                                                                       GuiCursorHudLineKeys.Distance }},

        {IntelLevel.LongRangeSensors, new List<GuiCursorHudLineKeys> { GuiCursorHudLineKeys.ItemName,
                                                                        GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                            GuiCursorHudLineKeys.Capacity,
                                                                          GuiCursorHudLineKeys.Resources,
                                                                       GuiCursorHudLineKeys.Specials,
                                                                       GuiCursorHudLineKeys.SettlementSize,
                                                                            GuiCursorHudLineKeys.Owner,
                                                                           GuiCursorHudLineKeys.CombatStrength,
                                                                           GuiCursorHudLineKeys.Composition,
                                                                           GuiCursorHudLineKeys.Speed,
                                                                           GuiCursorHudLineKeys.ShipSize,
                                                                           GuiCursorHudLineKeys.Distance }},

         {IntelLevel.ShortRangeSensors, new List<GuiCursorHudLineKeys> { GuiCursorHudLineKeys.ItemName,
                                                                        GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                            GuiCursorHudLineKeys.Capacity,
                                                                          GuiCursorHudLineKeys.Resources,
                                                                       GuiCursorHudLineKeys.Specials,
                                                                       GuiCursorHudLineKeys.SettlementDetails,
                                                                            GuiCursorHudLineKeys.Owner,
                                                                            GuiCursorHudLineKeys.Health,
                                                                           GuiCursorHudLineKeys.CombatStrengthDetails,
                                                                           GuiCursorHudLineKeys.CompositionDetails,
                                                                           GuiCursorHudLineKeys.Speed,
                                                                           GuiCursorHudLineKeys.ShipDetails,
                                                                           GuiCursorHudLineKeys.Distance }},

       {IntelLevel.Complete, new List<GuiCursorHudLineKeys> { GuiCursorHudLineKeys.ItemName,
                                                                        GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                            GuiCursorHudLineKeys.Capacity,
                                                                          GuiCursorHudLineKeys.Resources,
                                                                       GuiCursorHudLineKeys.Specials,
                                                                       GuiCursorHudLineKeys.SettlementDetails,
                                                                            GuiCursorHudLineKeys.Owner,
                                                                            GuiCursorHudLineKeys.Health,
                                                                           GuiCursorHudLineKeys.CombatStrengthDetails,
                                                                           GuiCursorHudLineKeys.CompositionDetails,
                                                                           GuiCursorHudLineKeys.Speed,
                                                                           GuiCursorHudLineKeys.ShipDetails,
                                                                           GuiCursorHudLineKeys.Distance }}
    };

        private static IColoredTextList _emptyIColoredTextList = new ColoredTextList();

        /// <summary>
        /// Makes or acquires an instance of GuiCursorHudText for the IntelLevel derived from the data provided.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="data">The _data.</param>
        /// <returns></returns>
        public static GuiCursorHudText MakeInstance(IntelLevel intelLevel, Data data) {
            Arguments.ValidateNotNull(data);
            IList<GuiCursorHudLineKeys> keys;
            if (_hudLineKeyLookup.TryGetValue(intelLevel, out keys)) {
                GuiCursorHudText guiCursorHudText = new GuiCursorHudText(intelLevel);
                IColoredTextList textList;

                foreach (GuiCursorHudLineKeys key in keys) {
                    textList = MakeInstance(key, intelLevel, data);
                    guiCursorHudText.Add(key, textList);
                }

                //if (data is SystemData) {
                //    SystemData systemData = data as SystemData;
                //    foreach (GuiCursorHudLineKeys key in keys) {
                //        textList = MakeSystemInstance(key, intelLevel, systemData);
                //        guiCursorHudText.Add(key, textList);
                //    }
                //}
                //else if (data is ShipData) {
                //    ShipData shipData = data as ShipData;
                //    foreach (GuiCursorHudLineKeys key in keys) {
                //        textList = MakeShipInstance(key, intelLevel, shipData);
                //        guiCursorHudText.Add(key, textList);
                //    }
                //}
                //else if (data is FleetData) {
                //    FleetData fleetData = data as FleetData;
                //    foreach (GuiCursorHudLineKeys key in keys) {
                //        textList = MakeFleetInstance(key, intelLevel, fleetData);
                //        guiCursorHudText.Add(key, textList);
                //    }
                //}
                //else {
                //    System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
                //    D.Error(ErrorMessages.TypeNotExpected.Inject(data.GetType(), stackFrame.GetMethod().Name));
                //}
                return guiCursorHudText;
            }
            D.Error("{0} {1} Key is not present in Lookup.", typeof(IntelLevel), intelLevel);
            return null;
        }

        public static IColoredTextList MakeInstance(GuiCursorHudLineKeys key, IntelLevel intelLevel, Data data) {
            if ((data as SystemData) != null) { return MakeSystemInstance(key, intelLevel, data as SystemData); }
            if ((data as ShipData) != null) { return MakeShipInstance(key, intelLevel, data as ShipData); }
            if ((data as FleetData) != null) { return MakeFleetInstance(key, intelLevel, data as FleetData); }
            return MakeBaseInstance(key, intelLevel, data);
        }

        private static IColoredTextList MakeBaseInstance(GuiCursorHudLineKeys key, IntelLevel intelLevel, Data data) {
            if (!ValidateKeyAgainstIntelLevel(key, intelLevel)) {
                return _emptyIColoredTextList;
            }
            switch (key) {
                case GuiCursorHudLineKeys.ItemName:
                    return data.ItemName != string.Empty ? new ColoredTextList_String(data.ItemName) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.PieceName:
                    return new ColoredTextList_String(data.PieceName);
                case GuiCursorHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiCursorHudLineKeys.IntelState:
                    return (data.LastHumanPlayerIntelDate != null) ? new ColoredTextList_Intel(data.LastHumanPlayerIntelDate, intelLevel) : _emptyIColoredTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiCursorHudLineKeys.Owner:
                case GuiCursorHudLineKeys.Health:
                case GuiCursorHudLineKeys.CombatStrength:
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                case GuiCursorHudLineKeys.Capacity:
                case GuiCursorHudLineKeys.Resources:
                case GuiCursorHudLineKeys.Specials:
                case GuiCursorHudLineKeys.SettlementSize:
                case GuiCursorHudLineKeys.SettlementDetails:
                case GuiCursorHudLineKeys.Composition:
                case GuiCursorHudLineKeys.CompositionDetails:
                case GuiCursorHudLineKeys.ShipSize:
                case GuiCursorHudLineKeys.ShipDetails:
                case GuiCursorHudLineKeys.Speed:
                    return _emptyIColoredTextList;

                case GuiCursorHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        /// <summary>
        /// Makes an instance of IColoredTextList for the provided key from the data provided.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static IColoredTextList MakeSystemInstance(GuiCursorHudLineKeys key, IntelLevel intelLevel, SystemData data) {
            if (!ValidateKeyAgainstIntelLevel(key, intelLevel)) {
                return _emptyIColoredTextList;
            }
            switch (key) {
                case GuiCursorHudLineKeys.ItemName:
                    return data.ItemName != string.Empty ? new ColoredTextList_String(data.ItemName) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.PieceName:
                    return new ColoredTextList_String(data.PieceName);
                case GuiCursorHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiCursorHudLineKeys.IntelState:
                    return (data.LastHumanPlayerIntelDate != null) ? new ColoredTextList_Intel(data.LastHumanPlayerIntelDate, intelLevel) : _emptyIColoredTextList;

                case GuiCursorHudLineKeys.Owner:
                    return (data.Settlement != null) ? new ColoredTextList_Owner(data.Settlement.Owner) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Health:
                    return (data.Settlement != null) ? new ColoredTextList_Health(data.Settlement.Health, data.Settlement.MaxHitPoints) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.CombatStrength:
                    return (data.Settlement != null) ? new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Settlement.Strength.Combined) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                    return (data.Settlement != null) ? new ColoredTextList_Combat(data.Settlement.Strength) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Capacity:
                    return new ColoredTextList<int>(Constants.FormatInt_2DMin, data.Capacity);
                case GuiCursorHudLineKeys.Resources:
                    return new ColoredTextList_Resources(data.Resources);
                case GuiCursorHudLineKeys.Specials:
                    return (data.SpecialResources != null) ? new ColoredTextList_Specials(data.SpecialResources) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.SettlementSize:
                    return (data.Settlement != null) ? new ColoredTextList_String(data.Settlement.SettlementSize.GetName()) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.SettlementDetails:
                    return (data.Settlement != null) ? new ColoredTextList_Settlement(data.Settlement) : _emptyIColoredTextList;

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiCursorHudLineKeys.Composition:
                case GuiCursorHudLineKeys.CompositionDetails:
                case GuiCursorHudLineKeys.ShipSize:
                case GuiCursorHudLineKeys.ShipDetails:
                case GuiCursorHudLineKeys.Speed:
                    return _emptyIColoredTextList;

                case GuiCursorHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        /// <summary>
        /// Makes an instance of IColoredTextList for the provided key from the data provided.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static IColoredTextList MakeFleetInstance(GuiCursorHudLineKeys key, IntelLevel intelLevel, FleetData data) {
            if (!ValidateKeyAgainstIntelLevel(key, intelLevel)) {
                return _emptyIColoredTextList;
            }
            switch (key) {
                case GuiCursorHudLineKeys.PieceName:
                    // ships and fleets donot show name if IntelLevel is Unknown
                    return intelLevel != IntelLevel.Unknown ? new ColoredTextList_String(data.PieceName) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiCursorHudLineKeys.IntelState:
                    return (data.LastHumanPlayerIntelDate != null) ? new ColoredTextList_Intel(data.LastHumanPlayerIntelDate, intelLevel) : _emptyIColoredTextList;

                case GuiCursorHudLineKeys.Speed:
                    return new ColoredTextList_Speed(data.CurrentSpeed, data.MaxSpeed);  // fleet will always display speed, even if zero
                case GuiCursorHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiCursorHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);
                case GuiCursorHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Strength.Combined);
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.Strength);
                case GuiCursorHudLineKeys.Composition:
                    return new ColoredTextList_Composition(data.Composition);
                case GuiCursorHudLineKeys.CompositionDetails:
                    // TODO
                    return new ColoredTextList_Composition(data.Composition);

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiCursorHudLineKeys.ItemName: // fleets donot have itemNames
                case GuiCursorHudLineKeys.Capacity:
                case GuiCursorHudLineKeys.Resources:
                case GuiCursorHudLineKeys.Specials:
                case GuiCursorHudLineKeys.SettlementSize:
                case GuiCursorHudLineKeys.SettlementDetails:
                case GuiCursorHudLineKeys.ShipSize:
                case GuiCursorHudLineKeys.ShipDetails:
                    return _emptyIColoredTextList;

                case GuiCursorHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        /// <summary>
        /// Makes an instance of IColoredTextList for the provided key from the data provided.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static IColoredTextList MakeShipInstance(GuiCursorHudLineKeys key, IntelLevel intelLevel, ShipData data) {
            if (!ValidateKeyAgainstIntelLevel(key, intelLevel)) {
                return _emptyIColoredTextList;
            }
            switch (key) {
                case GuiCursorHudLineKeys.ItemName:
                    // ships donot show name if IntelLevel is Unknown
                    return intelLevel != IntelLevel.Unknown ? new ColoredTextList_String(data.ItemName) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.PieceName:
                    // ships and fleets donot show name if IntelLevel is Unknown
                    return intelLevel != IntelLevel.Unknown ? new ColoredTextList_String(data.PieceName) : _emptyIColoredTextList;
                case GuiCursorHudLineKeys.Distance:
                    return new ColoredTextList_Distance(data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiCursorHudLineKeys.IntelState:
                    return (data.LastHumanPlayerIntelDate != null) ? new ColoredTextList_Intel(data.LastHumanPlayerIntelDate, intelLevel) : _emptyIColoredTextList;

                case GuiCursorHudLineKeys.Speed:
                    return new ColoredTextList_Speed(data.CurrentSpeed, data.MaxSpeed);
                case GuiCursorHudLineKeys.Owner:
                    return new ColoredTextList_Owner(data.Owner);
                case GuiCursorHudLineKeys.Health:
                    return new ColoredTextList_Health(data.Health, data.MaxHitPoints);
                case GuiCursorHudLineKeys.CombatStrength:
                    return new ColoredTextList<float>(Constants.FormatFloat_0Dp, data.Strength.Combined);
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                    return new ColoredTextList_Combat(data.Strength);
                case GuiCursorHudLineKeys.ShipSize:
                    return new ColoredTextList_String(data.Hull.GetDescription());
                case GuiCursorHudLineKeys.ShipDetails:
                    return new ColoredTextList_Ship(data);

                // The following is a fall through catcher for line keys that aren't processed. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiCursorHudLineKeys.Capacity:
                case GuiCursorHudLineKeys.Resources:
                case GuiCursorHudLineKeys.Specials:
                case GuiCursorHudLineKeys.Composition:
                case GuiCursorHudLineKeys.CompositionDetails:
                case GuiCursorHudLineKeys.SettlementSize:
                case GuiCursorHudLineKeys.SettlementDetails:
                    return _emptyIColoredTextList;

                case GuiCursorHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        private static bool ValidateKeyAgainstIntelLevel(GuiCursorHudLineKeys key, IntelLevel intelLevel) {
            return _hudLineKeyLookup[intelLevel].Contains<GuiCursorHudLineKeys>(key);
        }

        #region IColoredTextList Strategy Classes

        public class ColoredTextList : IColoredTextList {
            protected IList<ColoredText> _list = new List<ColoredText>(6);

            #region IColoredTextList Members
            public IList<ColoredText> GetList() {
                return _list;
            }
            #endregion
        }

        public class ColoredTextList<T> : ColoredTextList where T : struct {

            public ColoredTextList(params T[] values) : this(Constants.FormatNumber_Default, values) { }
            public ColoredTextList(string format, params T[] values) {
                foreach (T v in values) {
                    _list.Add(new ColoredText(format.Inject(v)));
                }
            }
        }

        public class ColoredTextList_String : ColoredTextList {

            public ColoredTextList_String(params string[] values) {
                foreach (string v in values) {
                    _list.Add(new ColoredText(v));
                }
            }
        }

        public class ColoredTextList_Distance : ColoredTextList {

            public ColoredTextList_Distance(Vector3 position, string format = Constants.FormatFloat_1DpMax) {
                // TODO calculate from Data.Position and <code>static GetSelected()<code>
                //if(nothing selected) return empty
                float distance = Vector3.Distance(position, Camera.main.transform.position);
                _list.Add(new ColoredText(format.Inject(distance)));
            }
        }

        public class ColoredTextList_Resources : ColoredTextList {

            public ColoredTextList_Resources(OpeYield ope, string format = Constants.FormatFloat_0Dp) {
                string organics_formatted = format.Inject(ope.GetYield(OpeResource.Organics));
                string particulates_formatted = format.Inject(ope.GetYield(OpeResource.Particulates));
                string energy_formatted = format.Inject(ope.GetYield(OpeResource.Energy));
                _list.Add(new ColoredText(organics_formatted));
                _list.Add(new ColoredText(particulates_formatted));
                _list.Add(new ColoredText(energy_formatted));
            }
        }

        public class ColoredTextList_Specials : ColoredTextList {

            public ColoredTextList_Specials(XYield x, string valueFormat = Constants.FormatFloat_1DpMax) {
                // TODO how to handle variable number of XResources
                IList<XYield.XResourceValuePair> allX = x.GetAllResources();

                string resourceName = allX[0].Resource.GetName();
                float resourceYield = allX[0].Value;
                string resourceYield_formatted = valueFormat.Inject(resourceYield);
                _list.Add(new ColoredText(resourceName));
                _list.Add(new ColoredText(resourceYield_formatted));
            }
        }

        public class ColoredTextList_Combat : ColoredTextList {

            public ColoredTextList_Combat(CombatStrength cs, string format = Constants.FormatFloat_0Dp) {
                _list.Add(new ColoredText(format.Inject(cs.Combined)));
                _list.Add(new ColoredText(format.Inject(cs.Beam_Offense)));
                _list.Add(new ColoredText(format.Inject(cs.Beam_Defense)));
                _list.Add(new ColoredText(format.Inject(cs.Particle_Offense)));
                _list.Add(new ColoredText(format.Inject(cs.Particle_Defense)));
                _list.Add(new ColoredText(format.Inject(cs.Missile_Offense)));
                _list.Add(new ColoredText(format.Inject(cs.Missile_Defense)));
            }
        }

        public class ColoredTextList_Intel : ColoredTextList {

            public ColoredTextList_Intel(IGameDate exploredDate, IntelLevel intelLevel) {
                GameTimePeriod intelAge = new GameTimePeriod(exploredDate, GameTime.Date);
                string intelText_formatted = ConstructIntelText(intelLevel, intelAge);
                _list.Add(new ColoredText(intelText_formatted));
            }

            private string ConstructIntelText(IntelLevel intelLevel, GameTimePeriod intelAge) {
                string intelMsg = intelLevel.GetName();
                switch (intelLevel) {
                    case IntelLevel.Unknown:
                    case IntelLevel.LongRangeSensors:
                    case IntelLevel.ShortRangeSensors:
                    case IntelLevel.Complete:
                        // no data to add
                        break;
                    case IntelLevel.OutOfDate:
                        string addendum = String.Format(". Last Intel {0} days ago.", intelAge.FormattedPeriod);
                        intelMsg = intelMsg + addendum;
                        break;
                    case IntelLevel.None:
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(intelLevel));
                }
                return intelMsg;
            }
        }

        public class ColoredTextList_Owner : ColoredTextList {

            public ColoredTextList_Owner(IPlayer player) {
                _list.Add(new ColoredText(player.LeaderName, player.Color));
            }
        }

        public class ColoredTextList_Health : ColoredTextList {

            public ColoredTextList_Health(float health, float maxHp, string format = Constants.FormatFloat_1DpMax) {
                float healthRatio = health / maxHp;
                GameColor healthColor = (healthRatio > GeneralSettings.Instance.InjuredHealthThreshold) ? GameColor.Green :
                            ((healthRatio > GeneralSettings.Instance.CriticalHealthThreshold) ? GameColor.Yellow : GameColor.Red);
                string health_formatted = format.Inject(health);
                string maxHp_formatted = format.Inject(maxHp);
                _list.Add(new ColoredText(health_formatted, healthColor));
                _list.Add(new ColoredText(maxHp_formatted, GameColor.Green));
            }
        }

        public class ColoredTextList_Settlement : ColoredTextList {

            public ColoredTextList_Settlement(SettlementData settlement) {
                _list.Add(new ColoredText(settlement.SettlementSize.GetName()));
                _list.Add(new ColoredText(settlement.Population.ToString()));
                _list.Add(new ColoredText(settlement.CapacityUsed.ToString()));
                _list.Add(new ColoredText("TBD"));  // OPE Used need format
                _list.Add(new ColoredText("TBD"));  // X Used need format
            }
        }

        public class ColoredTextList_Ship : ColoredTextList {

            public ColoredTextList_Ship(ShipData ship, string valueFormat = Constants.FormatFloat_1DpMax) {
                _list.Add(new ColoredText(ship.Hull.GetName()));
                _list.Add(new ColoredText(valueFormat.Inject(ship.Mass)));
                _list.Add(new ColoredText(valueFormat.Inject(ship.MaxTurnRate)));
            }
        }

        public class ColoredTextList_Composition : ColoredTextList {

            private static StringBuilder CompositionText = new StringBuilder();

            public ColoredTextList_Composition(IDictionary<ShipHull, IList<ShipData>> composition, string format = Constants.FormatInt_1DMin) {
                _list.Add(new ColoredText(ConstructCompositionText(composition, format).ToString()));
            }

            private StringBuilder ConstructCompositionText(IDictionary<ShipHull, IList<ShipData>> composition, string format) {
                CompositionText.Clear();
                CompositionText.Append(CommonTerms.Composition);
                CompositionText.Append(": ");
                IList<ShipHull> shipHulls = composition.Keys.ToList();
                foreach (var hull in shipHulls) {
                    int count = composition[hull].Count;
                    CompositionText.AppendFormat("{0}[", hull.GetDescription());
                    CompositionText.AppendFormat(format, count);

                    if (hull != shipHulls.First<ShipHull>() && hull != shipHulls.Last<ShipHull>()) {
                        CompositionText.Append("], ");
                        continue;
                    }
                    CompositionText.Append("]");
                }
                return CompositionText;
            }
        }

        public class ColoredTextList_Speed : ColoredTextList {

            private static string normalSpeedText = "{0}/{1}";
            private static string speedNoMaxText = "{0}";

            public ColoredTextList_Speed(float currentSpeed, float maxSpeed = Constants.ZeroF, string format = Constants.FormatFloat_1DpMax) {
                D.Assert(currentSpeed >= Constants.ZeroF, "Current Speed is {0}.".Inject(currentSpeed));
                string speedText;
                if (maxSpeed > Constants.ZeroF) {
                    speedText = normalSpeedText;
                    speedText = speedText.Inject(format.Inject(currentSpeed), format.Inject(maxSpeed));
                }
                else {
                    speedText = speedNoMaxText;
                    speedText = speedText.Inject(format.Inject(currentSpeed));
                }
                _list.Add(new ColoredText(speedText));
            }
        }

        #endregion

    }
}

