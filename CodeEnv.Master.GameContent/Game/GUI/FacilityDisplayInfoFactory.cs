﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Facilities.
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
    /// Factory that makes instances of text containing info about Facilities.
    /// </summary>
    public class FacilityDisplayInfoFactory : AElementItemDisplayInfoFactory<FacilityReport, FacilityDisplayInfoFactory> {

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            ItemInfoID.Name,
            ItemInfoID.ParentName,
            ItemInfoID.Owner,
            ItemInfoID.Category,
            ItemInfoID.Health,
            ItemInfoID.Defense,
            ItemInfoID.Offense,
            ItemInfoID.WeaponsRange,
            //ItemInfoID.SensorRange,  // makes no sense
            ItemInfoID.Science,
            ItemInfoID.Culture,
            ItemInfoID.NetIncome,
            ItemInfoID.Mass,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private FacilityDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, FacilityReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != FacilityHullCategory.None ? report.Category.GetValueName() : Unknown);
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

