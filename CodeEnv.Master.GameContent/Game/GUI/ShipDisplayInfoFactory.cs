// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Factory that makes instances of text containing info about Ships.
    /// </summary>
    public class ShipDisplayInfoFactory : AElementItemDisplayInfoFactory<ShipReport, ShipDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] {                         
            ContentID.Name,
            ContentID.ParentName,
            ContentID.Owner,
            ContentID.Category,
            ContentID.Health,
            ContentID.Defense,
            ContentID.Offense,
            ContentID.MaxWeaponsRange,
            ContentID.MaxSensorRange,
            ContentID.Science,
            ContentID.Culture,
            ContentID.NetIncome,
            ContentID.Mass,

            ContentID.Target,
            ContentID.TargetDistance,
            ContentID.CombatStance,
            ContentID.CurrentSpeed,
            ContentID.FullSpeed,
            ContentID.MaxTurnRate,

            ContentID.CameraDistance,
            ContentID.IntelState
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private ShipDisplayInfoFactory() {
            Initialize();
        }

        protected override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, ShipReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != ShipCategory.None ? report.Category.GetName() : _unknown);
                        break;
                    case ContentID.Target:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Target != null ? report.Target.DisplayName : _unknown);
                        break;
                    case ContentID.TargetDistance:
                        isSuccess = true;
                        float? targetDistance = CalcTargetDistance(report.Target, report.Position);
                        colorizedText = _phrase.Inject(targetDistance.HasValue ? GetFormat(contentID).Inject(targetDistance.Value) : _unknown);
                        break;
                    case ContentID.CurrentSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CurrentSpeed.HasValue ? GetFormat(contentID).Inject(report.CurrentSpeed.Value) : _unknown);
                        break;
                    case ContentID.FullSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.FullSpeed.HasValue ? GetFormat(contentID).Inject(report.FullSpeed.Value) : _unknown);
                        break;
                    case ContentID.MaxTurnRate:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.MaxTurnRate.HasValue ? GetFormat(contentID).Inject(report.MaxTurnRate.Value) : _unknown);
                        break;
                    case ContentID.CombatStance:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.CombatStance != ShipCombatStance.None ? report.CombatStance.GetName() : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
                }
            }
            return isSuccess;
        }

        private float? CalcTargetDistance(INavigableTarget target, Vector3? fleetPosition) {
            if (target != null && fleetPosition.HasValue) {
                return Vector3.Distance(target.Position, fleetPosition.Value);
            }
            return null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

