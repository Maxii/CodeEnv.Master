// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Planetoids.
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
    /// Factory that makes instances of text containing info about Planetoids.
    /// </summary>
    public class PlanetoidDisplayInfoFactory : AMortalItemDisplayInfoFactory<PlanetoidReport, PlanetoidDisplayInfoFactory> {

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,
            AccessControlInfoID.ParentName,
            AccessControlInfoID.Category,
            AccessControlInfoID.Owner,
            AccessControlInfoID.Capacity,
            AccessControlInfoID.Resources,
            AccessControlInfoID.Health,
            AccessControlInfoID.Defense,
            AccessControlInfoID.Mass,
            AccessControlInfoID.OrbitalSpeed,

            AccessControlInfoID.IntelState,
            AccessControlInfoID.CameraDistance
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private PlanetoidDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(AccessControlInfoID infoID, PlanetoidReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case AccessControlInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case AccessControlInfoID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != PlanetoidCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
                    case AccessControlInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : _unknown);
                        break;
                    case AccessControlInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : _unknown);
                        break;
                    case AccessControlInfoID.OrbitalSpeed:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.OrbitalSpeed.HasValue ? GetFormat(infoID).Inject(report.OrbitalSpeed.Value) : _unknown);
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

