// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Stars.
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
    /// Factory that makes instances of text containing info about Stars.
    /// </summary>
    public class StarDisplayInfoFactory : AIntelItemDisplayInfoFactory<StarReport, StarDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] { 
            ContentID.Name,
            ContentID.ParentName,
            ContentID.Category,
            ContentID.Owner,
            ContentID.Capacity,
            ContentID.Resources,
            ContentID.SectorIndex,

            ContentID.IntelState,
            ContentID.CameraDistance
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private StarDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ContentID contentID, StarReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(contentID, report, out colorizedText);
            if (!isSuccess) {
                switch (contentID) {
                    case ContentID.ParentName:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.ParentName != null ? report.ParentName : _unknown);
                        break;
                    case ContentID.Category:
                        isSuccess = true;
                        colorizedText = _phrase.Inject(report.Category != StarCategory.None ? report.Category.GetValueName() : _unknown);
                        break;
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

