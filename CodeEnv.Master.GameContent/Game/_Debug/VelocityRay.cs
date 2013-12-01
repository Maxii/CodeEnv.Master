// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: VelocityRay.cs
// Produces a Ray eminating from Target that indicates the Target's forward direction and speed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Produces a Ray eminating from Target that indicates the Target's forward direction and speed.
    /// </summary>
    public class VelocityRay : A3DVectrosityBase {

        private Reference<float> _speed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VelocityRay" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The transform that this VelocityRay is eminates from in the scene.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="parent">The parent to attach the VectorObject too.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public VelocityRay(string name, Transform target, Reference<float> speed, Transform parent = null, float width = 1F, GameColor color = GameColor.White)
            : base(name, new Vector3[2], target, parent, width, color) {
            _speed = speed;
        }

        protected override void Draw3D() {
            _line.points3[1] = Vector3.forward * _speed.Value;
            base.Draw3D();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

