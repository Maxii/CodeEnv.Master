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
    public class StarbaseDisplayInfoFactory : AUnitCmdDisplayInfoFactory<StarbaseReport, StarbaseDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] {                         
            ContentID.Name,
            ContentID.ParentName,
            ContentID.Category,
            ContentID.Composition,
            ContentID.Owner,

            ContentID.CurrentCmdEffectiveness,
            ContentID.Formation,
            ContentID.UnitOffense,
            ContentID.UnitDefense,
            ContentID.UnitHealth,
            ContentID.UnitMaxWeaponsRange,
            ContentID.UnitMaxSensorRange,
            ContentID.UnitScience,
            ContentID.UnitCulture,
            ContentID.UnitNetIncome,

            ContentID.Capacity,
            ContentID.Resources,

            ContentID.CameraDistance,
            ContentID.IntelState
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private StarbaseDisplayInfoFactory() {
            Initialize();
        }

        protected override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, StarbaseReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != StarbaseCategory.None ? report.Category.GetName() : _unknown);
                        break;
                    case ContentID.Composition:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : _unknown);
                        break;
                    case ContentID.Capacity:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Capacity.HasValue ? GetFormat(contentID).Inject(report.Capacity.Value) : _unknown);
                        break;
                    case ContentID.Resources:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : _unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(contentID));
                }
            }
            return isSuccess;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

