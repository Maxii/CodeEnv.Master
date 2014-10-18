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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Produces a Ray emanating from Target that indicates the Target's forward direction and speed.
    /// </summary>
    public class VelocityRay : A3DVectrosityBase {

        private Reference<float> _speed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VelocityRay" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The transform that this VelocityRay emanates from in the scene.</param>
        /// <param name="speed">The potentially changing speed as a reference.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public VelocityRay(string name, Transform target, Reference<float> speed, float width = 1F, GameColor color = GameColor.White)
            : base(name, new Vector3[2], target, References.DynamicObjectsFolder.Folder, LineType.Discrete, width, color) {
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

