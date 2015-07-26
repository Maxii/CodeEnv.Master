// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceHudFormContent.cs
// Content for the custom ResourceHudForm.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Content for the custom ResourceHudForm.
    /// </summary>
    [System.Obsolete]
    public class ResourceHudFormContent : AHudFormContent {

        public ResourceID ResourceID { get; private set; }

        public ResourceHudFormContent(ResourceID resourceID)
            : base(HudFormID.Resource) {
            ResourceID = resourceID;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

