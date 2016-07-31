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

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            ItemInfoID.Name,
            ItemInfoID.ParentName,
            ItemInfoID.Category,
            ItemInfoID.Owner,
            ItemInfoID.Capacity,
            ItemInfoID.Resources,
            ItemInfoID.Health,
            ItemInfoID.Defense,
            ItemInfoID.Mass,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.OrbitalSpeed,
            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private PlanetoidDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, PlanetoidReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.ParentName:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.ParentName != null ? report.ParentName : Unknown);
                        break;
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != PlanetoidCategory.None ? report.Category.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : Unknown);
                        break;
                    case ItemInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : Unknown);
                        break;
                    case ItemInfoID.OrbitalSpeed:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.OrbitalSpeed.HasValue ? GetFormat(infoID).Inject(report.OrbitalSpeed.Value) : Unknown);
                        break;
                    case ItemInfoID.Mass:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Mass.HasValue ? GetFormat(infoID).Inject(report.Mass.Value) : Unknown);
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

