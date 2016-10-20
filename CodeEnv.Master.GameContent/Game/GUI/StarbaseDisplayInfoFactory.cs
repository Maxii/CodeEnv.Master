﻿// --------------------------------------------------------------------------------------------------------------------
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

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            ItemInfoID.Name,
            ItemInfoID.ParentName,
            ItemInfoID.Category,
            ItemInfoID.Composition,
            ItemInfoID.Owner,
            ItemInfoID.SectorID,

            ItemInfoID.CurrentCmdEffectiveness,
            ItemInfoID.Formation,
            ItemInfoID.UnitOffense,
            ItemInfoID.UnitDefense,
            ItemInfoID.UnitHealth,
            ItemInfoID.UnitWeaponsRange,
            ItemInfoID.UnitSensorRange,
            ItemInfoID.UnitScience,
            ItemInfoID.UnitCulture,
            ItemInfoID.UnitNetIncome,

            ItemInfoID.Capacity,
            ItemInfoID.Resources,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private StarbaseDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, StarbaseCmdReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != StarbaseCategory.None ? report.Category.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.Composition:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : Unknown);
                        break;
                    case ItemInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : Unknown);
                        break;
                    case ItemInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : Unknown);
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

