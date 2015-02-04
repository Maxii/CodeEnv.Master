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
                                                                       GuiHudLineKeys.CameraDistance 
        }},

        {IntelCoverage.Minimal, new List<GuiHudLineKeys> { GuiHudLineKeys.SectorIndex,
                                                                        GuiHudLineKeys.IntelState,
                                                                        GuiHudLineKeys.CameraDistance, 
                                                                        
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
                                                                        GuiHudLineKeys.CameraDistance, 
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
                                                                        GuiHudLineKeys.CameraDistance,
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
                                                                           GuiHudLineKeys.ShipDetails,
                                                                            GuiHudLineKeys.TargetName,
                                                                            GuiHudLineKeys.TargetDistance
      }}
    };

        protected static IColoredTextList _emptyTextList = new ColoredTextListBase();

        protected override void Initialize() { }

        public GuiHudText MakeInstance(DataType data) {
            Arguments.ValidateNotNull(data);
            var intelCoverage = data.HumanPlayerIntelCoverage();
            //var intelCoverage = data.PlayerIntel.CurrentCoverage;
            IList<GuiHudLineKeys> keys;
            if (_hudLineKeyLookup.TryGetValue(intelCoverage, out keys)) {
                GuiHudText guiCursorHudText = new GuiHudText(intelCoverage);
                IColoredTextList textList;
                D.Log("IntelCoverage = {0}, Keys = {1}.", intelCoverage.GetName(), keys.Concatenate());

                foreach (GuiHudLineKeys key in keys) {
                    textList = MakeInstance(key, data);
                    guiCursorHudText.Add(key, textList);
                }
                return guiCursorHudText;
            }
            D.Error("{0} {1} Key is not present in Lookup.", typeof(IntelCoverage), intelCoverage.GetName());
            return null;
        }

        public IColoredTextList MakeInstance(GuiHudLineKeys key, DataType data) {
            if (!ValidateKeyAgainstIntelCoverage(key, data.HumanPlayerIntelCoverage())) {
                return _emptyTextList;
            }
            return MakeTextInstance(key, data);
        }
        //public IColoredTextList MakeInstance(GuiHudLineKeys key, DataType data) {
        //    if (!ValidateKeyAgainstIntelCoverage(key, data.PlayerIntel.CurrentCoverage)) {
        //        return _emptyTextList;
        //    }
        //    return MakeTextInstance(key, data);
        //}

        protected abstract IColoredTextList MakeTextInstance(GuiHudLineKeys key, DataType data);

        /// <summary>
        /// Validates the key against the intel coverage, warning and returning false if the key provided is not a key
        /// that is present in the key lookup list associated with this Coverage. Usage:
        /// <code>if(!ValidateKeyAgainstIntelCoveragel(key, intelCoverage) {
        /// return _emptyColoredTextList
        /// }</code>
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="intelCoverage">The intel coverage.</param>
        /// <returns></returns>
        private static bool ValidateKeyAgainstIntelCoverage(GuiHudLineKeys key, IntelCoverage intelCoverage) {
            return _hudLineKeyLookup[intelCoverage].Contains<GuiHudLineKeys>(key);
        }

        #region IColoredTextList Strategy Classes

        // Note: ColoredTextListBase, ColoredTextList<T> and ColoredTextList_String all in separate class files under Common

        public class ColoredTextList_Distance : ColoredTextListBase {

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
            /// generates a ColoredTextList containing the distance from the camera's focal plane to the provided
            /// point in world space.
            /// </summary>
            /// <param name="point">The point in world space..</param>
            /// <param name="format">The format.</param>
            public ColoredTextList_Distance(Vector3 point, string format = Constants.FormatFloat_1DpMax) {
                // TODO calculate from Data.Position and <code>static GetSelected()<code>
                //if(nothing selected) return empty
                float distance = point.DistanceToCamera();
                _list.Add(new ColoredText(format.Inject(distance)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
            /// generates a ColoredTextList containing the distance from the camera's focal plane to the camera's 
            /// current target point.
            /// </summary>
            /// <param name="format">The format.</param>
            public ColoredTextList_Distance(string format = Constants.FormatFloat_1DpMax) {
                float distance = References.MainCameraControl.DistanceToCamera;
                _list.Add(new ColoredText(format.Inject(distance)));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ColoredTextList_Distance"/> class. This version
            /// generates a ColoredTextList containing the distance between 2 points in world space.
            /// </summary>
            /// <param name="pointA">The first point in world space.</param>
            /// <param name="pointB">The second point in world space.</param>
            /// <param name="format">The format.</param>
            public ColoredTextList_Distance(Vector3 pointA, Vector3 pointB, string format = Constants.FormatFloat_1DpMax) {
                float distance = Vector3.Distance(pointA, pointB);
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

            public ColoredTextList_Combat(CombatStrength offense, CombatStrength defense, string format = Constants.FormatFloat_0Dp) {
                _list.Add(new ColoredText(format.Inject(offense.Combined + defense.Combined)));
                _list.Add(new ColoredText(format.Inject(offense.GetValue(ArmamentCategory.Beam))));
                _list.Add(new ColoredText(format.Inject(defense.GetValue(ArmamentCategory.Beam))));
                _list.Add(new ColoredText(format.Inject(offense.GetValue(ArmamentCategory.Missile))));
                _list.Add(new ColoredText(format.Inject(defense.GetValue(ArmamentCategory.Missile))));
                _list.Add(new ColoredText(format.Inject(offense.GetValue(ArmamentCategory.Particle))));
                _list.Add(new ColoredText(format.Inject(defense.GetValue(ArmamentCategory.Particle))));
            }
        }

        public class ColoredTextList_Intel : ColoredTextListBase {

            public ColoredTextList_Intel(AIntel intel) {
                string intelText_formatted = ConstructIntelText(intel);
                _list.Add(new ColoredText(intelText_formatted));
            }

            private string ConstructIntelText(AIntel intel) {
                string intelMsg = intel.CurrentCoverage.GetName();
                string addendum = ". Intel is current.";
                var intelWithDatedCoverage = intel as Intel;
                if (intelWithDatedCoverage != null && intelWithDatedCoverage.IsDatedCoverageValid) {
                    //D.Log("DateStamp = {0}, CurrentDate = {1}.", intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
                    GameTimeDuration intelAge = new GameTimeDuration(intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
                    addendum = String.Format(". Intel age {0}.", intelAge.ToString());
                }
                intelMsg = intelMsg + addendum;
                D.Log(intelMsg);
                return intelMsg;
            }
        }

        public class ColoredTextList_Owner : ColoredTextListBase {

            public ColoredTextList_Owner(Player player) {
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

        //public class ColoredTextList_Composition : ColoredTextListBase {

        //    private static StringBuilder CompositionText = new StringBuilder();

        //    public ColoredTextList_Composition(FleetComposition composition, string format = Constants.FormatInt_1DMin) {
        //        _list.Add(new ColoredText(ConstructCompositionText(composition, format).ToString()));
        //    }

        //    private StringBuilder ConstructCompositionText(FleetComposition composition, string format) {
        //        CompositionText.Clear();
        //        CompositionText.Append(CommonTerms.Composition);
        //        CompositionText.Append(": ");
        //        IList<ShipCategory> shipHulls = composition.Categories;
        //        foreach (var hull in shipHulls) {
        //            int count = composition.GetData(hull).Count;
        //            CompositionText.AppendFormat("{0}[", hull.GetDescription());
        //            CompositionText.AppendFormat(format, count);

        //            if (hull != shipHulls.First<ShipCategory>() && hull != shipHulls.Last<ShipCategory>()) {
        //                CompositionText.Append("], ");
        //                continue;
        //            }
        //            CompositionText.Append("]");
        //        }
        //        return CompositionText;
        //    }
        //}

        public class ColoredTextList_Speed : ColoredTextListBase {

            private static string normalSpeedText = "{0:0.##}/{1:0.##}";
            private static string speedNoRequestedText = "{0:0.##}";

            public ColoredTextList_Speed(float currentSpeed, float requestedSpeed = Constants.ZeroF, string format = Constants.FormatFloat_2DpMax) {
                D.Assert(currentSpeed >= Constants.ZeroF, "Current Speed is {0}.".Inject(currentSpeed));
                string speedText;
                if (requestedSpeed > Constants.ZeroF) {
                    speedText = normalSpeedText;
                    speedText = speedText.Inject(format.Inject(currentSpeed), format.Inject(requestedSpeed));
                }
                else {
                    speedText = speedNoRequestedText;
                    speedText = speedText.Inject(format.Inject(currentSpeed));
                }
                _list.Add(new ColoredText(speedText));
            }
        }

        #endregion

    }
}


