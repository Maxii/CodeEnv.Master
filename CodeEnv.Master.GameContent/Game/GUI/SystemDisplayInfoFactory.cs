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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes instances of text containing info about Systems.
    /// </summary>
    public class SystemDisplayInfoFactory : AItemDisplayInfoFactory<SystemReport, SystemDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] { 
            ContentID.Name,
            ContentID.Owner,
            ContentID.SectorIndex,
            ContentID.Capacity,
            ContentID.Resources,

            ContentID.CameraDistance
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private SystemDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, SystemReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.SectorIndex:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.SectorIndex.ToString());
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

