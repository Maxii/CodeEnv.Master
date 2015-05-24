// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SectorDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Sectors.
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
    /// Factory that makes instances of text containing info about Sectors.
    /// </summary>
    public class SectorDisplayInfoFactory : AItemDisplayInfoFactory<SectorReport, SectorDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] { 
            ContentID.Name,
            ContentID.Owner,
            ContentID.SectorIndex,
            ContentID.Density,

            ContentID.CameraDistance
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private SectorDisplayInfoFactory() {
            Initialize();
        }

        protected override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, SectorReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.Density:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(GetFormat(contentID).Inject(report.Density));
                        break;
                    case ContentID.SectorIndex:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.SectorIndex.ToString());
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

