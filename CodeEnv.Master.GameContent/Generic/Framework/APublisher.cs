// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APublisher.cs
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
    public abstract class APublisher {

        protected IGameManager _gameMgr;
        private IDictionary<LabelID, ALabelText> _labelTextCache = new Dictionary<LabelID, ALabelText>();

        public APublisher() {
            _gameMgr = References.GameManager;
        }

        public abstract ALabelText GetLabelText(LabelID labelID);

        public abstract bool TryUpdateLabelTextContent(LabelID labelID, LabelContentID contentID, out IColoredTextList content);

        protected void CacheLabelText(LabelID labelID, ALabelText labelText) {
            _labelTextCache[labelID] = labelText;
        }

        protected bool TryGetCachedLabelText(LabelID labelID, out ALabelText cachedLabelText) {
            return _labelTextCache.TryGetValue(labelID, out cachedLabelText);
        }

    }
}

