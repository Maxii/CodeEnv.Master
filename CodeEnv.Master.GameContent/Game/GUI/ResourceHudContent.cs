// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceHudContent.cs
// Content containing Resource info for the custom ResourceHudElement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Content containing Resource info for the custom ResourceHudElement.
    /// </summary>
    public class ResourceHudContent : AHudElementContent {

        public ResourceID ResourceID { get; private set; }

        public ResourceHudContent(ResourceID resourceID)
            : base(HudElementID.Resource) {
            ResourceID = resourceID;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

