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

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Report and LabelText CmdPublishers.
    /// </summary>
    [Obsolete]
    public abstract class ACmdPublisherBase {

        protected IDictionary<DisplayTargetID, IntelLabelText> _labelTextCache = new Dictionary<DisplayTargetID, IntelLabelText>();
        protected IGameManager _gameMgr;

        public ACmdPublisherBase() {
            _gameMgr = References.GameManager;
        }

        public abstract IntelLabelText GetLabelText(DisplayTargetID displayTgtID, AElementItemReport[] elementReports);

        public abstract bool TryUpdateLabelTextContent(DisplayTargetID displayTgtID, LabelContentID contentID, AElementItemReport[] elementReports, out IColoredTextList content);

    }
}

