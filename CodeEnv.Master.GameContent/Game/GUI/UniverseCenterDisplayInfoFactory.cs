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

        private static AccessControlInfoID[] _infoIDsToDisplay = new AccessControlInfoID[] {
            AccessControlInfoID.Name,

            AccessControlInfoID.IntelState,
            AccessControlInfoID.CameraDistance
        };

        protected override AccessControlInfoID[] InfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private UniverseCenterDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

