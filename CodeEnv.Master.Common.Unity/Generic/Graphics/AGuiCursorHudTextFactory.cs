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

        {IntelLevel.Unexplored, new List<GuiCursorHudDisplayLineKeys> { GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                       GuiCursorHudDisplayLineKeys.Distance }},

        {IntelLevel.OutOfRange, new List<GuiCursorHudDisplayLineKeys> {   GuiCursorHudDisplayLineKeys.Name,
                                                                       GuiCursorHudDisplayLineKeys.Capacity,
                                                                       GuiCursorHudDisplayLineKeys.Resources,
                                                                       GuiCursorHudDisplayLineKeys.Specials,
                                                                       GuiCursorHudDisplayLineKeys.IntelState,
                                                                       GuiCursorHudDisplayLineKeys.Distance }},

       {IntelLevel.ShortRangeSensors, new List<GuiCursorHudDisplayLineKeys> {GuiCursorHudDisplayLineKeys.Name,
                                                                            GuiCursorHudDisplayLineKeys.Owner,
                                                                           GuiCursorHudDisplayLineKeys.Speed,
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
        /// in this base class.
        /// </summary>
        /// <param name="intelLevel">The intel level.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual IColoredTextList MakeInstance_ColoredTextList(IntelLevel intelLevel, GuiCursorHudDisplayLineKeys key) {

            switch (key) {
                case GuiCursorHudDisplayLineKeys.Name:
                    return (_data.Name != null) ? new ColoredTextList_String(_data.Name) : new ColoredTextList_String();
                case GuiCursorHudDisplayLineKeys.Distance:
                    // FIXME Vector3 default value is (0,0,0) not null. A System located at (0.0.0) will not get past this test
                    return (_data.Position != Vector3.zero) ? new ColoredTextList_Distance(_data.Position) : new ColoredTextList_Distance();
                case GuiCursorHudDisplayLineKeys.Owner:
                    return (_data.Owner != Players.None) ? new ColoredTextList_Owner(_data.Owner) : new ColoredTextList_Owner();
                case GuiCursorHudDisplayLineKeys.Health:
                    return (_data.MaxHitPoints != Constants.ZeroF) ? new ColoredTextList_Health(_data.Health, _data.MaxHitPoints) : new ColoredTextList_Health();
                case GuiCursorHudDisplayLineKeys.CombatStrength:
                    // {0:0.}    float with zero decimal places, rounded                    
                    return (_data.CombatStrength != Constants.ZeroF) ? new ColoredTextList<float>(_data.CombatStrength, "{0:0.}") : new ColoredTextList<float>();
                case GuiCursorHudDisplayLineKeys.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(key));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IColoredTextList Strategy Classes

        public abstract class AColoredTextList : IColoredTextList {
            protected IList<ColoredText> _list = new List<ColoredText>(3);

            #region IColoredTextList Members
            public IList<ColoredText> GetList() {
                return _list;
            }
            #endregion
        }

        public class ColoredTextList<T> : AColoredTextList where T : struct {

            public ColoredTextList() { }    // used to create an empty list
            public ColoredTextList(T value, string format = "{0:G3}") {
                _list.Add(new ColoredText(format.Inject(value)));
            }
        }

        public class ColoredTextList_String : AColoredTextList {

            public ColoredTextList_String() { }    // used to create an empty list
            public ColoredTextList_String(string value, string format = "{0}") {
                _list.Add(new ColoredText(format.Inject(value)));
            }
        }

        public class ColoredTextList_Distance : AColoredTextList {

            public ColoredTextList_Distance() { }    // used to create an empty list
            public ColoredTextList_Distance(Vector3 position) {
                // TODO calculate from SystemData.Position and <code>static GetSelected()<code>
                float distance = Vector3.Distance(position, TempGameValues.UniverseOrigin);
                _list.Add(new ColoredText("{0:0.#}".Inject(distance))); // {0:0.#}     float with max one decimal place, rounded
            }
        }

        public class ColoredTextList_Resources : AColoredTextList {

            public ColoredTextList_Resources() { }    // used to create an empty list
            public ColoredTextList_Resources(OpeYield ope) {
                string organics_formatted = String.Format("{0:0.}", ope.Organics);  // {0:0.}    float with zero decimal places, rounded
                string particulates_formatted = String.Format("{0:0.}", ope.Particulates);
                string energy_formatted = String.Format("{0:0.}", ope.Energy);
                _list.Add(new ColoredText(organics_formatted));
                _list.Add(new ColoredText(particulates_formatted));
                _list.Add(new ColoredText(energy_formatted));
            }
        }

        public class ColoredTextList_Specials : AColoredTextList {

            public ColoredTextList_Specials() { }    // used to create an empty list
            public ColoredTextList_Specials(XYield x) {
                // TODO how to handle variable number of XResources
                IList<XYield.XResourceValuePair> allX = x.GetAllResources();

                string resourceName = allX[0].Resource.GetName();
                float resourceYield = allX[0].Value;
                string resourceYield_formatted = String.Format("{0:0.}", resourceYield);    // {0:0.}  float with zero decimal places, rounded
                _list.Add(new ColoredText(resourceName));
                _list.Add(new ColoredText(resourceYield_formatted));
            }
        }

        public class ColoredTextList_Intel : AColoredTextList {

            public ColoredTextList_Intel() { }    // used to create an empty list
            public ColoredTextList_Intel(IGameDate exploredDate, IntelLevel intelLevel) {
                // TODO fill out from HumanPlayer.GetIntelState(System) - need last intel date too
                GameTimePeriod intelAge = new GameTimePeriod(exploredDate, GameTime.Date);
                string intelText_formatted = ConstructIntelText(intelLevel, intelAge);
                _list.Add(new ColoredText(intelText_formatted));
            }

            private string ConstructIntelText(IntelLevel intelLevel, GameTimePeriod intelAge) {
                string intelMsg = intelLevel.GetName();
                switch (intelLevel) {
                    case IntelLevel.Unexplored:
                    case IntelLevel.LongRangeSensors:
                    case IntelLevel.ShortRangeSensors:
                        // no data to add
                        break;
                    case IntelLevel.OutOfRange:
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

        public class ColoredTextList_Owner : AColoredTextList {

            public ColoredTextList_Owner() { }    // used to create an empty list
            public ColoredTextList_Owner(Players player) {
                _list.Add(new ColoredText(player.GetName(), player.PlayerColor()));
            }
        }

        public class ColoredTextList_Health : AColoredTextList {

            public ColoredTextList_Health() { }    // used to create an empty list
            public ColoredTextList_Health(float health, float maxHp) {
                float healthRatio = health / maxHp;
                Color healthColor = (healthRatio > TempGameValues.InjuredHealthThreshold) ? Color.green :
                            ((healthRatio > TempGameValues.CriticalHealthThreshold) ? Color.yellow : Color.red);
                string health_formatted = String.Format("{0:0.}", health);  // {0:0.}    float with zero decimal places, rounded
                string maxHp_formatted = String.Format("{0:0.}", maxHp);
                _list.Add(new ColoredText(health_formatted, healthColor));
                _list.Add(new ColoredText(maxHp_formatted, Color.green));
            }
        }

        #endregion

    }
}

