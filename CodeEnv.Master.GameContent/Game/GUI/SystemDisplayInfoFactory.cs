// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Systems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes instances of text containing info about Systems.
    /// </summary>
    public class SystemDisplayInfoFactory : AIntelItemDisplayInfoFactory<SystemReport, SystemDisplayInfoFactory> {

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            ItemInfoID.Name,
            ItemInfoID.Owner,
            ItemInfoID.SectorID,
            //ItemInfoID.Capacity,
            ItemInfoID.Resources,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private SystemDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, SystemReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.SectorID:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.SectorID.ToString());
                        break;
                    case ItemInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : Unknown);
                        break;
                    case ItemInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Resources != default(ResourcesYield) ? report.Resources.ToString() : Unknown);
                        ////colorizedText = _lineTemplate.Inject(report.Resources.HasValue ? report.Resources.Value.ToString() : Unknown);
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }


    }
}

