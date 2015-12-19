// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterDisplayInfoFactory.cs
// Factory that makes instances of text containing info about the Universe Center.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Factory that makes instances of text containing info about the Universe Center.
    /// </summary>
    public class UniverseCenterDisplayInfoFactory : AIntelItemDisplayInfoFactory<UniverseCenterReport, UniverseCenterDisplayInfoFactory> {

        private static ContentID[] _contentIDsToDisplay = new ContentID[] { 
            ContentID.Name,

            ContentID.IntelState,
            ContentID.CameraDistance
        };

        protected override ContentID[] ContentIDsToDisplay { get { return _contentIDsToDisplay; } }

        private UniverseCenterDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

