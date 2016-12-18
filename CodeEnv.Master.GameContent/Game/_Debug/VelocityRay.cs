// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VelocityRay.cs
// Produces a Ray emanating from Target that indicates the Target's forward direction and speed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Produces a Ray emanating from Target that indicates the Target's forward direction and speed.
    /// </summary>
    public class VelocityRay : A3DVectrosityBase {

        private Reference<float> _speed;

        /// <summary>
        /// Job that refreshes _point3[1] with the current speed value. This refresh must occur 
        /// before LateUpdate so Draw3DAuto which uses LateUpdate will always have a current value.
        /// </summary>
        private Job _refreshSpeedValueJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="VelocityRay"/> class with the DynamicObjectsFolder
        /// as the line parent, a line width of 1 and color White.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The transform that this VelocityRay emanates from in the scene.</param>
        /// <param name="speed">The potentially changing speed as a reference.</param>
        public VelocityRay(string name, Transform target, Reference<float> speed)
            : this(name, target, speed, References.DynamicObjectsFolder.Folder) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VelocityRay" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The transform that this VelocityRay emanates from in the scene.</param>
        /// <param name="speed">The potentially changing speed as a reference.</param>
        /// <param name="lineParent">The line parent.</param>
        /// <param name="width">The width. Default is 1 pixel.</param>
        /// <param name="color">The color. Default is Gray.</param>
        public VelocityRay(string name, Transform target, Reference<float> speed, Transform lineParent, float width = 1F, GameColor color = GameColor.White)
            : base(name, new List<Vector3>(2), target, lineParent, LineType.Discrete, width, color) {
            _speed = speed;
        }

        protected override void HandleLineActivated() {
            base.HandleLineActivated();
            D.Assert(IsLineActive);
            D.AssertNull(_refreshSpeedValueJob);
            string jobName = "{0}.RefreshSpeedJob".Inject(LineName);
            _refreshSpeedValueJob = _jobMgr.StartGameplayJob(UpdateSpeed(), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                D.Assert(jobWasKilled);
            });
        }

        private IEnumerator UpdateSpeed() {
            while (true) {
                _line.points3[1] = Vector3.forward * _speed.Value;
                yield return null;  // Updates speed value just before every Draw3DAuto.LateUpdate draw call    
            }   //yield return new WaitForFixedUpdate();  // also tried this, but no difference I can tell
        }

        protected override void HandleLineDeactivated() {
            base.HandleLineDeactivated();
            D.Assert(!IsLineActive);
            D.AssertNotNull(_refreshSpeedValueJob);
            KillRefreshSpeedValueJob();
        }

        private void KillRefreshSpeedValueJob() {
            if (_refreshSpeedValueJob != null) {
                _refreshSpeedValueJob.Kill();
                _refreshSpeedValueJob = null;
            }
        }

        #region Event and Property Change Handlers

        #endregion

        // 8.12.16 Job pausing moved to JobManager to consolidate handling

        protected override void Cleanup() {
            base.Cleanup();
            // 12.8.16 Job Disposal centralized in JobManager
            KillRefreshSpeedValueJob();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

