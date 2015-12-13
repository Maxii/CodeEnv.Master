// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDisplayManager.cs
// DisplayManager for Facilities.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Facilities.
    /// </summary>
    public class FacilityDisplayManager : AElementDisplayManager {

        protected override Layers CullingLayer { get { return Layers.FacilityCull; } }

        private IRevolver _revolver;

        public FacilityDisplayManager(IWidgetTrackable trackedFacility, GameColor color)
            : base(trackedFacility, color) { }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _revolver = itemGo.GetSingleInterfaceInChildren<IRevolver>();
            _revolver.enabled = false;
            //TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.enabled = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

