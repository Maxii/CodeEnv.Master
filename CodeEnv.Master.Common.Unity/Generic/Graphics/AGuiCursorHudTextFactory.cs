// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StrategyFactory.cs
// Abstract Factory that makes GuiCursorHudText and IColoredTextList instances using the Strategy pattern.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Abstract Factory that makes GuiCursorHudText and IColoredTextList instances using the Strategy pattern.
    /// </summary>
    public abstract class AGuiCursorHudTextFactory {

        private static IDictionary<IntelLevel, IList<GuiCursorHudDisplayLineKeys>> _hudLineKeyLookup = new Dictionary<IntelLevel, IList<GuiCursorHudDisplayLineKeys>> {

        {IntelLevel.Unknown, new List<GuiCursorHudDisplayLineKeys> { GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                       GuiCursorHudDisplayLineKeys.Distance }},

        {IntelLevel.OutOfDate, new List<GuiCursorHudDisplayLineKeys> {   GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                       GuiCursorHudDisplayLineKeys.Capacity,
                                                                       GuiCursorHudDisplayLineKeys.Resources,
                                                                       GuiCursorHudDisplayLineKeys.Specials,
                                                                       GuiCursorHudDisplayLineKeys.Distance }},

        {IntelLevel.LongRangeSensors, new List<GuiCursorHudDisplayLineKeys> {GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                            GuiCursorHudDisplayLineKeys.Capacity,
                                                                          GuiCursorHudDisplayLineKeys.Resources,
                                                                       GuiCursorHudDisplayLineKeys.Specials,
                                                                       GuiCursorHudDisplayLineKeys.SettlementSize,
                                                                            GuiCursorHudDisplayLineKeys.Owner,
                                                                           GuiCursorHudDisplayLineKeys.CombatStrength,
                                                                           GuiCursorHudDisplayLineKeys.Composition,
                                                                           GuiCursorHudDisplayLineKeys.Speed,
                                                                           GuiCursorHudDisplayLineKeys.Distance }},

         {IntelLevel.ShortRangeSensors, new List<GuiCursorHudDisplayLineKeys> {GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                            GuiCursorHudDisplayLineKeys.Capacity,
                                                                          GuiCursorHudDisplayLineKeys.Resources,
                                                                       GuiCursorHudDisplayLineKeys.Specials,
                                                                       GuiCursorHudDisplayLineKeys.SettlementDetails,
                                                                            GuiCursorHudDisplayLineKeys.Owner,
                                                                           GuiCursorHudDisplayLineKeys.CombatStrengthDetails,
                                                                           GuiCursorHudDisplayLineKeys.CompositionDetails,
                                                                           GuiCursorHudDisplayLineKeys.Speed,
                                                                           GuiCursorHudDisplayLineKeys.ShipDetails,
                                                                           GuiCursorHudDisplayLineKeys.Distance }},



       {IntelLevel.Complete, new List<GuiCursorHudDisplayLineKeys> {GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                            GuiCursorHudDisplayLineKeys.Capacity,
                                                                          GuiCursorHudDisplayLineKeys.Resources,
                                                                       GuiCursorHudDisplayLineKeys.Specials,
                                                                       GuiCursorHudDisplayLineKeys.SettlementDetails,
                                                                            GuiCursorHudDisplayLineKeys.Owner,
                                                                           GuiCursorHudDisplayLineKeys.CombatStrengthDetails,
                                                                           GuiCursorHudDisplayLineKeys.CompositionDetails,
                                                                           GuiCursorHudDisplayLineKeys.Speed,
                                                                           GuiCursorHudDisplayLineKeys.ShipDetails,
                                                                           GuiCursorHudDisplayLineKeys.Distance }}

    };

        private IDictionary<IntelLevel, GuiCursorHudText> _guiCursorHudTextCache;

        protected AData _data;

        public AGuiCursorHudTextFactory(AData data) {
            _data = data;
        }

        /// <summary>
        /// Makes or acquires an instance of GuiCursorHudText for the indicated IntelLevel.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>   
        /// <returns></returns>
        public GuiCursorHudText MakeInstance_GuiCursorHudText(IntelLevel intelLevel) {
            if (_guiCursorHudTextCache == null) {
                _guiCursorHudTextCache = new Dictionary<IntelLevel, GuiCursorHudText>();
            }

            if (_guiCursorHudTextCache.ContainsKey(intelLevel)) {
                return _guiCursorHudTextCache[intelLevel];
            }

            IList<GuiCursorHudDisplayLineKeys> keys;
            if (_hudLineKeyLookup.TryGetValue(intelLevel, out keys)) {
                GuiCursorHudText guiCursorHudText = new GuiCursorHudText(intelLevel);

                foreach (GuiCursorHudDisplayLineKeys key in keys) {
                    IColoredTextList textList = MakeInstance_ColoredTextList(intelLevel, key);
                    guiCursorHudText.Add(key, textList);
                }
                _guiCursorHudTextCache.Add(intelLevel, guiCursorHudText);
                return guiCursorHudText;
            }
            D.Error("{0} {1} Key is not present in Lookup.", typeof(IntelLevel), intelLevel);
            return null;
        }

        /// <summary>
        /// Makes a strategy instance of IColoredTextList. Only the keys that use data from this AData are implemented
        /// in this base class. The other keys are implemented to act as a catchall for line keys that are processed in derived factories, returning an empty ColoredTextList which is ignored by GuiCursorHudText
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual IColoredTextList MakeInstance_ColoredTextList(IntelLevel intelLevel, GuiCursorHudDisplayLineKeys key) {

            switch (key) {
                case GuiCursorHudDisplayLineKeys.Name:
                    return (_data.Name != null) ? new ColoredTextList_String(_data.Name) : new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.Distance:
                    // FIXME Vector3 default value is (0,0,0) not null. A System located at (0.0.0) will not get past this test
                    return (_data.Position != Vector3.zero) ? new ColoredTextList_Distance(_data.Position) : new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.Owner:
                    return (_data.Owner != Players.None) ? new ColoredTextList_Owner(_data.Owner) : new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.Health:
                    return (_data.MaxHitPoints != Constants.ZeroF) ? new ColoredTextList_Health(_data.Health, _data.MaxHitPoints) : new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.CombatStrength:
                    // {0:0.}    float with zero decimal places, rounded                    
                    return (_data.CombatStrength != null) ? new ColoredTextList<float>("{0:0.}", _data.CombatStrength.Combined) : new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.CombatStrengthDetails:
                    return (_data.CombatStrength != null) ? new ColoredTextList_CombatDetails(_data.CombatStrength) : new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.IntelState:
                    return (_data.DateHumanPlayerExplored != null) ? new ColoredTextList_Intel(_data.DateHumanPlayerExplored, intelLevel) : new ColoredTextList();

                // The following is a catchall for line keys that are processed in derived factories. An empty ColoredTextList will be returned which will be ignored by GuiCursorHudText
                case GuiCursorHudDisplayLineKeys.Capacity:
                case GuiCursorHudDisplayLineKeys.Composition:
                case GuiCursorHudDisplayLineKeys.CompositionDetails:
                case GuiCursorHudDisplayLineKeys.Resources:
                case GuiCursorHudDisplayLineKeys.SettlementDetails:
                case GuiCursorHudDisplayLineKeys.SettlementSize:
                case GuiCursorHudDisplayLineKeys.ShipDetails:
                case GuiCursorHudDisplayLineKeys.Specials:
                case GuiCursorHudDisplayLineKeys.Speed:
                    return new ColoredTextList();
                case GuiCursorHudDisplayLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
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

            public ColoredTextList(params T[] values) : this("{0:G3}", values) { }
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

            public ColoredTextList_Distance(Vector3 position, string format = "{0:0.#}") {
                // TODO calculate from Data.Position and <code>static GetSelected()<code>
                float distance = Vector3.Distance(position, Camera.main.transform.position);
                _list.Add(new ColoredText(format.Inject(distance))); // {0:0.#}     float with max one decimal place, rounded
            }
        }

        public class ColoredTextList_Resources : ColoredTextList {

            public ColoredTextList_Resources(OpeYield ope, string format = "{0:0.}") {
                string organics_formatted = format.Inject(ope.Organics);  // {0:0.}    float with zero decimal places, rounded
                string particulates_formatted = format.Inject(ope.Particulates);
                string energy_formatted = format.Inject(ope.Energy);
                _list.Add(new ColoredText(organics_formatted));
                _list.Add(new ColoredText(particulates_formatted));
                _list.Add(new ColoredText(energy_formatted));
            }
        }

        public class ColoredTextList_Specials : ColoredTextList {

            public ColoredTextList_Specials(XYield x, string valueFormat = "{0:0.}") {
                // TODO how to handle variable number of XResources
                IList<XYield.XResourceValuePair> allX = x.GetAllResources();

                string resourceName = allX[0].Resource.GetName();
                float resourceYield = allX[0].Value;
                string resourceYield_formatted = valueFormat.Inject(resourceYield);    // {0:0.}  float with zero decimal places, rounded
                _list.Add(new ColoredText(resourceName));
                _list.Add(new ColoredText(resourceYield_formatted));
            }
        }

        public class ColoredTextList_CombatDetails : ColoredTextList {

            public ColoredTextList_CombatDetails(CombatStrength cs, string format = "{0:0.}") {
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
                // TODO fill out from HumanPlayer.GetIntelState(System) - need last intel date too
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

            public ColoredTextList_Owner(Players player) {
                _list.Add(new ColoredText(player.GetName(), player.PlayerColor()));
            }
        }

        public class ColoredTextList_Health : ColoredTextList {

            public ColoredTextList_Health(float health, float maxHp, string format = "{0:0.}") {
                float healthRatio = health / maxHp;
                Color healthColor = (healthRatio > TempGameValues.InjuredHealthThreshold) ? Color.green :
                            ((healthRatio > TempGameValues.CriticalHealthThreshold) ? Color.yellow : Color.red);
                string health_formatted = format.Inject(health);  // {0:0.}    float with zero decimal places, rounded
                string maxHp_formatted = format.Inject(maxHp);
                _list.Add(new ColoredText(health_formatted, healthColor));
                _list.Add(new ColoredText(maxHp_formatted, Color.green));
            }
        }

        #endregion

    }
}

