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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
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
            set { SetProperty<Vector3>(ref _size, value, "Size", SizePropChangedHandler); }
        }

        /// <summary>
        /// The visual center point of the wireframe.
        /// </summary>
        private VectorLine _centerPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="CubeWireframe"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target.</param>
        /// <param name="cubeSize">Size of the cube.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public CubeWireframe(string name, Transform target, Vector3 cubeSize, float width = 1F, GameColor color = GameColor.White)
            : base(name, new List<Vector3>(24), target, GameReferences.DynamicObjectsFolder.Folder, LineType.Discrete, width, color) {
            Utility.ValidateNotNull(target);
            _size = cubeSize;
        }

        protected override void Initialize() {
            base.Initialize();
            _line.MakeCube(Vector3.zero, Size.x, Size.y, Size.z);
            List<Vector3> centerPoint = new List<Vector3>() { Vector3.zero };

            _centerPoint = new VectorLine("CenterPoint", centerPoint, texture, 2F, LineType.Points);
            _centerPoint.color = Color.ToUnityColor();    // color removed from constructor in Vectrosity 4.0

            _centerPoint.drawTransform = _target; // _line.Draw3D(_target);  removed by Vectrosity 3.0
            _centerPoint.active = false;
        }

        protected override void HandleLineActivated() {
            base.HandleLineActivated();
            D.Assert(!_centerPoint.active);
            _centerPoint.active = true;
            if (!_centerPoint.isAutoDrawing) {
                _centerPoint.Draw3DAuto();
            }
        }

        protected override void AssignParent(Transform lineParent) {
            base.AssignParent(lineParent);
            D.Assert(_centerPoint.active);
            _centerPoint.rectTransform.SetParent(lineParent, worldPositionStays: true);
        }

        protected override void HandleLineDeactivated() {
            base.HandleLineDeactivated();
            D.Assert(_centerPoint.active);
            _centerPoint.active = false;
        }

        #region Event and Property Change Handlers

        private void SizePropChangedHandler() {
            if (_line != null) {
                _line.MakeCube(Vector3.zero, Size.x, Size.y, Size.z);
            }
        }

        #endregion

        protected override void Cleanup() {
            base.Cleanup();
            if (_centerPoint != null && _centerPoint.isAutoDrawing) {
                _centerPoint.StopDrawing3DAuto();
            }
            VectorLine.Destroy(ref _centerPoint);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region OverHotSpot Archive

        ///// <summary>
        ///// <c>true</c> if [is mouse over hot spot]; otherwise, <c>false</c>.
        ///// </summary>
        //public bool IsMouseOverHotSpot {
        //    get {
        //        if (_pointLine == null) { return false; }
        //        int unused;
        //        return _pointLine.Selected(Input.mousePosition, 40, out unused);
        //    }
        //}

        #endregion

    }
}

