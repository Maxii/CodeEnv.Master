// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiCursorHudTextFactory.cs
// Abstract base class for factories that make GuiCursorHudText and IColoredTextList instances.
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
    /// Abstract base class for factories that make GuiCursorHudText and IColoredTextList instances.
    /// </summary>
    [Obsolete]
    public abstract class AGuiCursorHudTextFactory {

        private static IDictionary<IntelLevel, IList<GuiCursorHudLineKeys>> _hudLineKeyLookup = new Dictionary<IntelLevel, IList<GuiCursorHudLineKeys>> {

        {IntelLevel.Unknown, new List<GuiCursorHudLineKeys> { GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                       GuiCursorHudLineKeys.Distance }},

        {IntelLevel.OutOfDate, new List<GuiCursorHudLineKeys> {   GuiCursorHudLineKeys.PieceName,
                                                                       GuiCursorHudLineKeys.IntelState,
                                                                       GuiCursorHudLineKeys.Capacity,
                                                                       GuiCursorHudLineKeys.Resources,
                                                                       GuiCursorHudLineKeys.Specials,
                                                                       GuiCursorHudLineKeys.Distance }},

        {IntelLevel.LongRangeSensors, new List<GuiCursorHudLineKeys> {GuiCursorHudLineKeys.PieceName,
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

         {IntelLevel.ShortRangeSensors, new List<GuiCursorHudLineKeys> {GuiCursorHudLineKeys.PieceName,
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

       {IntelLevel.Complete, new List<GuiCursorHudLineKeys> {GuiCursorHudLineKeys.PieceName,
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

        protected static IColoredTextList _emptyIColoredTextList = new ColoredTextList();

        public IntelLevel IntelLevel { get; set; }

        protected Data _data;
        private IDictionary<IntelLevel, GuiCursorHudText> _guiCursorHudTextCache;

        public AGuiCursorHudTextFactory(Data data, IntelLevel intelLevel) {
            Arguments.ValidateNotNull(data);
            IntelLevel = intelLevel;
            _data = data;
        }

        /// <summary>
        /// Makes or acquires an instance of GuiCursorHudText for the IntelLevel.
        /// </summary>
        /// <param name="currentSpeed">Current speed of the game object. Can be zero.</param>
        /// <returns></returns>
        public GuiCursorHudText MakeInstance(float currentSpeed) {
            if (_guiCursorHudTextCache == null) {
                _guiCursorHudTextCache = new Dictionary<IntelLevel, GuiCursorHudText>();
            }

            if (_data.IsDirty) {
                _guiCursorHudTextCache.Clear();
                _data.IsDirty = false;
            }

            if (_guiCursorHudTextCache.ContainsKey(IntelLevel)) {
                return _guiCursorHudTextCache[IntelLevel];
            }

            IList<GuiCursorHudLineKeys> keys;
            if (_hudLineKeyLookup.TryGetValue(IntelLevel, out keys)) {
                GuiCursorHudText guiCursorHudText = new GuiCursorHudText(IntelLevel);

                IColoredTextList textList;
                foreach (GuiCursorHudLineKeys key in keys) {
                    textList = MakeInstance(key, currentSpeed);
                    guiCursorHudText.Add(key, textList);
                }
                //_guiCursorHudTextCache[intelLevel] = guiCursorHudText;  
                _guiCursorHudTextCache.Add(IntelLevel, guiCursorHudText);   // .Add() throws exception if key already has another value
                return guiCursorHudText;
            }
            D.Error("{0} {1} Key is not present in Lookup.", typeof(IntelLevel), IntelLevel);
            return null;
        }

        /// <summary>
        /// Makes an instance of IColoredTextList for the provided key from the data contained within the factory.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="currentSpeed">Optional current speed of the game object.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual IColoredTextList MakeInstance(GuiCursorHudLineKeys key, float currentSpeed = -1F) {
            switch (key) {
                case GuiCursorHudLineKeys.PieceName:
                    return new ColoredTextList_String(_data.PieceName);
                case GuiCursorHudLineKeys.Distance:
                    return new ColoredTextList_Distance(_data.Position);    // returns empty if nothing is selected thereby making distance n/a
                case GuiCursorHudLineKeys.IntelState:
                    return (_data.LastHumanPlayerIntelDate != null) ? new ColoredTextList_Intel(_data.LastHumanPlayerIntelDate, IntelLevel) : _emptyIColoredTextList;

                // The following is a fall through catcher for line keys that aren't processed in derived factories. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiCursorHudLineKeys.Owner:
                case GuiCursorHudLineKeys.Health:
                case GuiCursorHudLineKeys.CombatStrength:
                case GuiCursorHudLineKeys.CombatStrengthDetails:
                case GuiCursorHudLineKeys.Capacity:
                case GuiCursorHudLineKeys.Resources:
                case GuiCursorHudLineKeys.Specials:
                case GuiCursorHudLineKeys.Composition:
                case GuiCursorHudLineKeys.CompositionDetails:
                case GuiCursorHudLineKeys.SettlementSize:
                case GuiCursorHudLineKeys.SettlementDetails:
                case GuiCursorHudLineKeys.ShipSize:
                case GuiCursorHudLineKeys.ShipDetails:
                    return _emptyIColoredTextList;

                //Speed should be processed by all derived factories
                case GuiCursorHudLineKeys.Speed:
                case GuiCursorHudLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        protected bool ValidateKeyAgainstIntelLevel(GuiCursorHudLineKeys key) {
            return _hudLineKeyLookup[IntelLevel].Contains<GuiCursorHudLineKeys>(key);
        }

        #region IColoredTextList Strategy Classes

        protected class ColoredTextList : IColoredTextList {
            protected IList<ColoredText> _list = new List<ColoredText>(6);

            #region IColoredTextList Members
            public IList<ColoredText> GetList() {
                return _list;
            }
            #endregion
        }

        protected class ColoredTextList<T> : ColoredTextList where T : struct {

            public ColoredTextList(params T[] values) : this(Constants.FormatNumber_Default, values) { }
            public ColoredTextList(string format, params T[] values) {
                foreach (T v in values) {
                    _list.Add(new ColoredText(format.Inject(v)));
                }
            }
        }

        protected class ColoredTextList_String : ColoredTextList {

            public ColoredTextList_String(params string[] values) {
                foreach (string v in values) {
                    _list.Add(new ColoredText(v));
                }
            }
        }

        protected class ColoredTextList_Distance : ColoredTextList {

            public ColoredTextList_Distance(Vector3 position, string format = Constants.FormatFloat_1DpMax) {
                // TODO calculate from Data.Position and <code>static GetSelected()<code>
                //if(nothing selected) return empty
                float distance = Vector3.Distance(position, Camera.main.transform.position);
                _list.Add(new ColoredText(format.Inject(distance)));
            }
        }

        protected class ColoredTextList_Resources : ColoredTextList {

            public ColoredTextList_Resources(OpeYield ope, string format = Constants.FormatFloat_0Dp) {
                string organics_formatted = format.Inject(ope.Organics);
                string particulates_formatted = format.Inject(ope.Particulates);
                string energy_formatted = format.Inject(ope.Energy);
                _list.Add(new ColoredText(organics_formatted));
                _list.Add(new ColoredText(particulates_formatted));
                _list.Add(new ColoredText(energy_formatted));
            }
        }

        protected class ColoredTextList_Specials : ColoredTextList {

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

        protected class ColoredTextList_Combat : ColoredTextList {

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

        protected class ColoredTextList_Intel : ColoredTextList {

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

        protected class ColoredTextList_Owner : ColoredTextList {

            public ColoredTextList_Owner(Players player) {
                _list.Add(new ColoredText(player.GetName(), player.PlayerColor()));
            }
        }

        protected class ColoredTextList_Health : ColoredTextList {

            public ColoredTextList_Health(float health, float maxHp, string format = Constants.FormatFloat_1DpMax) {
                float healthRatio = health / maxHp;
                Color healthColor = (healthRatio > TempGameValues.InjuredHealthThreshold) ? Color.green :
                            ((healthRatio > TempGameValues.CriticalHealthThreshold) ? Color.yellow : Color.red);
                string health_formatted = format.Inject(health);
                string maxHp_formatted = format.Inject(maxHp);
                _list.Add(new ColoredText(health_formatted, healthColor));
                _list.Add(new ColoredText(maxHp_formatted, Color.green));
            }
        }

        protected class ColoredTextList_Settlement : ColoredTextList {

            public ColoredTextList_Settlement(SettlementData settlement) {
                _list.Add(new ColoredText(settlement.SettlementSize.GetName()));
                _list.Add(new ColoredText(settlement.Population.ToString()));
                _list.Add(new ColoredText(settlement.CapacityUsed.ToString()));
                _list.Add(new ColoredText("TBD"));  // OPE Used need format
                _list.Add(new ColoredText("TBD"));  // X Used need format
            }
        }

        protected class ColoredTextList_Ship : ColoredTextList {

            public ColoredTextList_Ship(ShipData ship, string valueFormat = Constants.FormatFloat_1DpMax) {
                _list.Add(new ColoredText(ship.Hull.GetName()));
                _list.Add(new ColoredText(valueFormat.Inject(ship.Mass)));
                _list.Add(new ColoredText(valueFormat.Inject(ship.MaxTurnRate)));
            }
        }

        protected class ColoredTextList_Composition : ColoredTextList {

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

        protected class ColoredTextList_Speed : ColoredTextList {

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

