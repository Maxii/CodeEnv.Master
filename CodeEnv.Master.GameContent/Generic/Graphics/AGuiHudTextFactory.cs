// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiHudTextFactory.cs
// Singleton. Abstract base class for Factories that make GuiCursorHudText and IColoredTextList instances for display by IGuiHuds.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Singleton. Abstract base class for Factories that make GuiCursorHudText and IColoredTextList instances for display by IGuiHuds.
    /// </summary>
    /// <typeparam name="ClassType">The derived class type.</typeparam>
    /// <typeparam name="DataType">The type of Data.</typeparam>
    public abstract class AGuiHudTextFactory<ClassType, DataType> : AGenericSingleton<ClassType>, IGuiHudTextFactory<DataType>
        where ClassType : class
        where DataType : AItemData {

        private static IDictionary<IntelCoverage, IList<GuiHudLineKeys>> _hudLineKeyLookup = new Dictionary<IntelCoverage, IList<GuiHudLineKeys>> {

        {IntelCoverage.None, new List<GuiHudLineKeys> { GuiHudLineKeys.SectorIndex }},

        {IntelCoverage.Aware, new List<GuiHudLineKeys> { GuiHudLineKeys.SectorIndex,

                                                                       GuiHudLineKeys.IntelState,
                                                                       GuiHudLineKeys.Distance }},

        {IntelCoverage.Minimal, new List<GuiHudLineKeys> { GuiHudLineKeys.SectorIndex,
                                                                        GuiHudLineKeys.IntelState,
                                                                        GuiHudLineKeys.Distance, 
                                                                        
                                                                        GuiHudLineKeys.Name,
                                                                        GuiHudLineKeys.ParentName,
                                                                       GuiHudLineKeys.Category,
                                                                        GuiHudLineKeys.Capacity,
                                                                          GuiHudLineKeys.Resources,
                                                                       GuiHudLineKeys.Specials,
                                                                            GuiHudLineKeys.Owner,
                                                                       GuiHudLineKeys.Density,
                                                                           GuiHudLineKeys.Speed
                                                                            }},

         {IntelCoverage.Moderate, new List<GuiHudLineKeys> { GuiHudLineKeys.SectorIndex,
                                                                        GuiHudLineKeys.IntelState,
                                                                        GuiHudLineKeys.Distance, 
                                                                        GuiHudLineKeys.Name,
                                                                        GuiHudLineKeys.ParentName,
                                                                       GuiHudLineKeys.Category,
                                                                        GuiHudLineKeys.Capacity,
                                                                          GuiHudLineKeys.Resources,
                                                                       GuiHudLineKeys.Specials,
                                                                            GuiHudLineKeys.Owner,
                                                                       GuiHudLineKeys.Density,
                                                                           GuiHudLineKeys.Speed,
             
                                                                            GuiHudLineKeys.Health,
                                                                           GuiHudLineKeys.CombatStrength,
                                                                           GuiHudLineKeys.Composition
                                                                         }},

       {IntelCoverage.Comprehensive, new List<GuiHudLineKeys> { GuiHudLineKeys.SectorIndex,
                                                                        GuiHudLineKeys.IntelState,
                                                                        GuiHudLineKeys.Distance,
                                                                        GuiHudLineKeys.Name,
                                                                        GuiHudLineKeys.ParentName,
                                                                       GuiHudLineKeys.Category,
                                                                        GuiHudLineKeys.Capacity,
                                                                          GuiHudLineKeys.Resources,
                                                                       GuiHudLineKeys.Specials,
                                                                            GuiHudLineKeys.Owner,
                                                                       GuiHudLineKeys.Density,
                                                                           GuiHudLineKeys.Speed,
             
                                                                            GuiHudLineKeys.Health,
                                                                           GuiHudLineKeys.CombatStrengthDetails,
                                                                           GuiHudLineKeys.CompositionDetails,
           
                                                                       GuiHudLineKeys.SettlementDetails,
                                                                           GuiHudLineKeys.ShipDetails }}
    };


        protected static IColoredTextList _emptyColoredTextList = new ColoredTextListBase();

        protected override void Initialize() {
            // TODO do any initialization here
        }

        public GuiHudText MakeInstance(IIntel intel, DataType data) {
            Arguments.ValidateNotNull(data);
            IList<GuiHudLineKeys> keys;
            if (_hudLineKeyLookup.TryGetValue(intel.CurrentCoverage, out keys)) {
                GuiHudText guiCursorHudText = new GuiHudText(intel.CurrentCoverage);
                IColoredTextList textList;
                D.Log("IntelCoverage = {0}, Keys = {1}.", intel.CurrentCoverage.GetName(), keys.Concatenate());

                foreach (GuiHudLineKeys key in keys) {
                    textList = MakeInstance(key, intel, data);
                    guiCursorHudText.Add(key, textList);
                }
                return guiCursorHudText;
            }
            D.Error("{0} {1} Key is not present in Lookup.", typeof(IntelCoverage), intel.CurrentCoverage.GetName());
            return null;
        }


        public IColoredTextList MakeInstance(GuiHudLineKeys key, IIntel intel, DataType data) {
            if (!ValidateKeyAgainstIntelLevel(key, intel)) {
                return _emptyColoredTextList;
            }
            return MakeTextInstance(key, intel, data);
        }

        protected abstract IColoredTextList MakeTextInstance(GuiHudLineKeys key, IIntel intel, DataType data);

        /// <summary>
        /// Validates the key against intel, warning and returning false if the key provided is not a key
        /// that is present in the key lookup list associated with this Intel. Usage:
        /// <code>if(!ValidateKeyAgainstIntelLevel(key, intel) {
        /// return _emptyColoredTextList
        /// }</code>
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="intel">The intel.</param>
        /// <returns></returns>
        private static bool ValidateKeyAgainstIntelLevel(GuiHudLineKeys key, IIntel intel) {
            return _hudLineKeyLookup[intel.CurrentCoverage].Contains<GuiHudLineKeys>(key);
        }


        #region IColoredTextList Strategy Classes

        // Note: ColoredTextListBase, ColoredTextList<T> and ColoredTextList_String all in separate class files under Common

        public class ColoredTextList_Distance : ColoredTextListBase {

            public ColoredTextList_Distance(Vector3 position, string format = Constants.FormatFloat_1DpMax) {
                // TODO calculate from Data.Position and <code>static GetSelected()<code>
                //if(nothing selected) return empty
                float distance = position.DistanceToCamera();
                _list.Add(new ColoredText(format.Inject(distance)));
            }
        }

        public class ColoredTextList_Resources : ColoredTextListBase {

            public ColoredTextList_Resources(OpeYield ope, string format = Constants.FormatFloat_0Dp) {
                string organics_formatted = format.Inject(ope.GetYield(OpeResource.Organics));
                string particulates_formatted = format.Inject(ope.GetYield(OpeResource.Particulates));
                string energy_formatted = format.Inject(ope.GetYield(OpeResource.Energy));
                _list.Add(new ColoredText(organics_formatted));
                _list.Add(new ColoredText(particulates_formatted));
                _list.Add(new ColoredText(energy_formatted));
            }
        }

        public class ColoredTextList_Specials : ColoredTextListBase {

            public ColoredTextList_Specials(XYield x, string valueFormat = Constants.FormatFloat_1DpMax) {
                var resourcesPresent = x.GetAllResources();
                foreach (var resource in resourcesPresent) {
                    string resourceName = resource.GetName();
                    float resourceYield = x.GetYield(resource);
                    string resourceYield_formatted = valueFormat.Inject(resourceYield);
                    _list.Add(new ColoredText(resourceName));
                    _list.Add(new ColoredText(resourceYield_formatted));
                    if (_list.Count == 2) {
                        //  HACK only shows 1 xResource. How to display variable number of XResources?
                        break;
                    }

                }
            }
        }

        public class ColoredTextList_Combat : ColoredTextListBase {

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

        public class ColoredTextList_Intel : ColoredTextListBase {

            public ColoredTextList_Intel(IIntel intel) {
                string intelText_formatted = ConstructIntelText(intel);
                _list.Add(new ColoredText(intelText_formatted));
            }

            private string ConstructIntelText(IIntel intel) {
                string intelMsg = intel.CurrentCoverage.GetName();
                string addendum = ". Intel is current.";
                if (!(intel is FixedIntel) && !(intel is ImprovingIntel)) { // IMPROVE avoid having to inspect Intel Types
                    if (intel.DatedCoverage != IntelCoverage.None) {  // OutOfDateScope is None if there is no previous record, DateStamp is null
                        //D.Log("DateStamp = {0}, CurrentDate = {1}.", intel.DateStamp, GameTime.Date);

                        GameTimeDuration intelAge = new GameTimeDuration(intel.DateStamp, GameTime.CurrentDate);
                        addendum = String.Format(". Intel age {0}.", intelAge.ToString());
                    }
                }
                intelMsg = intelMsg + addendum;
                D.Log(intelMsg);
                return intelMsg;
            }
        }

        public class ColoredTextList_Owner : ColoredTextListBase {

            public ColoredTextList_Owner(IPlayer player) {
                _list.Add(new ColoredText(player.LeaderName, player.Color));
            }
        }

        public class ColoredTextList_Health : ColoredTextListBase {

            public ColoredTextList_Health(float health, float maxHp, string format = Constants.FormatFloat_1DpMax) {
                GameColor healthColor = (health > GeneralSettings.Instance.InjuredHealthThreshold) ? GameColor.Green :
                            ((health > GeneralSettings.Instance.CriticalHealthThreshold) ? GameColor.Yellow : GameColor.Red);
                string health_formatted = format.Inject(health * 100);
                string maxHp_formatted = format.Inject(maxHp);
                _list.Add(new ColoredText(health_formatted, healthColor));
                _list.Add(new ColoredText(maxHp_formatted, GameColor.Green));
            }
        }

        public class ColoredTextList_Settlement : ColoredTextListBase {

            public ColoredTextList_Settlement(SettlementCmdData settlement) {
                _list.Add(new ColoredText(settlement.Category.GetName()));
                _list.Add(new ColoredText(settlement.Population.ToString()));
                _list.Add(new ColoredText(settlement.CapacityUsed.ToString()));
                _list.Add(new ColoredText("TBD"));  // OPE Used need format
                _list.Add(new ColoredText("TBD"));  // X Used need format
            }
        }

        public class ColoredTextList_Ship : ColoredTextListBase {

            public ColoredTextList_Ship(ShipData ship, string valueFormat = Constants.FormatFloat_1DpMax) {
                _list.Add(new ColoredText(ship.Category.GetName()));
                _list.Add(new ColoredText(valueFormat.Inject(ship.Mass)));
                _list.Add(new ColoredText(valueFormat.Inject(ship.MaxTurnRate)));
            }
        }

        public class ColoredTextList_Composition : ColoredTextListBase {

            private static StringBuilder CompositionText = new StringBuilder();

            public ColoredTextList_Composition(FleetComposition composition, string format = Constants.FormatInt_1DMin) {
                _list.Add(new ColoredText(ConstructCompositionText(composition, format).ToString()));
            }

            private StringBuilder ConstructCompositionText(FleetComposition composition, string format) {
                CompositionText.Clear();
                CompositionText.Append(CommonTerms.Composition);
                CompositionText.Append(": ");
                IList<ShipCategory> shipHulls = composition.Categories;
                foreach (var hull in shipHulls) {
                    int count = composition.GetData(hull).Count;
                    CompositionText.AppendFormat("{0}[", hull.GetDescription());
                    CompositionText.AppendFormat(format, count);

                    if (hull != shipHulls.First<ShipCategory>() && hull != shipHulls.Last<ShipCategory>()) {
                        CompositionText.Append("], ");
                        continue;
                    }
                    CompositionText.Append("]");
                }
                return CompositionText;
            }
        }

        public class ColoredTextList_Speed : ColoredTextListBase {

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


