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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// DisplayManager for Facilities.
    /// </summary>
    public class FacilityDisplayManager : AElementDisplayManager {

        private IRevolver _revolver;

        public FacilityDisplayManager(IWidgetTrackable trackedFacility, Layers meshLayer)
            : base(trackedFacility, meshLayer) { }

        protected override void InitializeOther(GameObject itemGo) {
            base.InitializeOther(itemGo);
            _revolver = itemGo.GetSingleInterfaceInChildren<IRevolver>();
            //_revolver.IsActivated = false;    // enabled = false in Awake
            if (_revolver.RotateDuringPause) {
                D.Warn("FYI. {0} revolver set to rotate during a pause.", DebugName);
            }
            //TODO Revolver settings
        }

        protected override void AssessComponentsToShowOrOperate() {
            base.AssessComponentsToShowOrOperate();
            _revolver.IsActivated = IsDisplayEnabled && IsPrimaryMeshInMainCameraLOS;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }

}

