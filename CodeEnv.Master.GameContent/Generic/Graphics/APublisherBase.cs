// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APublisherBase.cs
// Abstract base class for Report and LabelText Publishers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Report and LabelText Publishers.
    /// </summary>
    public abstract class APublisherBase {

        protected IGameManager _gameMgr;
        private IDictionary<LabelID, LabelText> _labelTextCache = new Dictionary<LabelID, LabelText>();

        public APublisherBase() {
            _gameMgr = References.GameManager;
        }

        public abstract LabelText GetLabelText(LabelID labelID);

        public abstract IColoredTextList UpdateContent(LabelID labelID, LabelContentID contentID);

        protected bool IsCachedLabelTextCurrent(LabelID labelID, IntelCoverage intelCoverage, out LabelText labelText) {
            return TryGetCachedLabelText(labelID, out labelText) && labelText.IntelCoverage == intelCoverage;
        }

        private void CacheLabelText(LabelID labelID, LabelText labelText) {
            _labelTextCache[labelID] = labelText;
        }

        private bool TryGetCachedLabelText(LabelID labelID, out LabelText cachedLabelText) {
            return _labelTextCache.TryGetValue(labelID, out cachedLabelText);
        }

    }
}

