// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CubeWireframe.cs
// Generates a rectangular box wireframe around a gameobject.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Generates a rectangular box wireframe around a gameobject. 
    /// </summary>
    public class CubeWireframe : A3DVectrosityBase {

        private Vector3 _size;
        public Vector3 Size {
            get { return _size; }
            set { SetProperty<Vector3>(ref _size, value, "Size", OnSizeChanged); }
        }

        /// <summary>
        /// <c>true</c> if [is mouse over hot spot]; otherwise, <c>false</c>.
        /// </summary>
        public bool IsMouseOverHotSpot {
            get {
                if (_pointLine == null) { return false; }
                int unused;
                return _pointLine.Selected(Input.mousePosition, 40, out unused);
            }
        }

        private VectorPoints _pointLine;

        /// <summary>
        /// Initializes a new instance of the <see cref="CubeWireframe"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target.</param>
        /// <param name="boxSize">Size of the box.</param>
        /// <param name="parent">The parent to attach the VectorObject too.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public CubeWireframe(string name, Transform target, Vector3 boxSize, Transform parent = null, float width = 1F, GameColor color = GameColor.White)
            : base(name, new Vector3[24], target, parent, width, color) {
            _size = boxSize;
        }

        protected override void Initialize() {
            base.Initialize();
            _line.MakeCube(Vector3.zero, Size.x, Size.y, Size.z);
            Vector3[] centerPoint = new Vector3[] { new Vector3(0F, 0F, 0F) };
            _pointLine = new VectorPoints("CenterPoint", centerPoint, Color.ToUnityColor(), material, 2F);
        }

        public override void Show(bool toShow) {
            base.Show(toShow);
            _pointLine.active = toShow;
        }

        protected override void Draw3D() {
            base.Draw3D();
            if (_target != null) {
                _pointLine.Draw3D(_target);
            }
            else {
                _pointLine.Draw3D();
            }
        }

        private void OnSizeChanged() {
            if (_line != null) {
                _line.MakeCube(Vector3.zero, Size.x, Size.y, Size.z);
            }
        }

        protected override void Cleanup() {
            base.Cleanup();
            VectorLine.Destroy(ref _pointLine);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

