// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes instances of text containing info about Starbases.
    /// </summary>
    public class StarbaseDisplayInfoFactory : AUnitCmdDisplayInfoFactory<StarbaseCmdReport, StarbaseDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Category,
            AccessControlInfoID.Composition,
            AccessControlInfoID.Owner,

            AccessControlInfoID.CurrentCmdEffectiveness,
            AccessControlInfoID.Formation,
            AccessControlInfoID.UnitOffense,
            AccessControlInfoID.UnitDefense,
            AccessControlInfoID.UnitHealth,
            AccessControlInfoID.UnitWeaponsRange,
            AccessControlInfoID.UnitSensorRange,
            AccessControlInfoID.UnitScience,
            AccessControlInfoID.UnitCulture,
            AccessControlInfoID.UnitNetIncome,

            AccessControlInfoID.Capacity,
            AccessControlInfoID.Resources,

            AccessControlInfoID.CameraDistance,
            AccessControlInfoID.IntelState
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private StarbaseDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, StarbaseCmdReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != StarbaseCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    case AccessControlInfoID.Composition:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

