// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResourceInfo.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    [Obsolete]
    public class ResourceInfo {

        public string ImageFilename { get; private set; }

        public AtlasID AtlasID { get; private set; }

        public string Description { get; private set; }

        public ResourceCategory Category { get; private set; }

        //TODO need ResourceID as a common enum across all resources
        public RareResourceID ResourceID { get; private set; }


        public ResourceInfo(RareResourceID resourceID, ResourceCategory category, string imageFilename, AtlasID atlasID, string description) {
            ResourceID = resourceID;
            Category = category;
            ImageFilename = imageFilename;
            AtlasID = atlasID;
            Description = description;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

