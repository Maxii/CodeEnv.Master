// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACmdPublisherBase.cs
// Abstract base class for Report and LabelText CmdPublishers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Report and LabelText CmdPublishers.
    /// </summary>
    public abstract class ACmdPublisherBase {

        protected IDictionary<LabelID, LabelText> _labelTextCache = new Dictionary<LabelID, LabelText>();
        protected IGameManager _gameMgr;

        public ACmdPublisherBase() {
            _gameMgr = References.GameManager;
        }

        public abstract LabelText GetLabelText(LabelID labelID, AElementItemReport[] elementReports);

        public abstract IColoredTextList UpdateContent(LabelID labelID, LabelContentID contentID);

    }
}

