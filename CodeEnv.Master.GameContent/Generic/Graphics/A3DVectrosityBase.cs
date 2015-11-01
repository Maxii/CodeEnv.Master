// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: A3DVectrosityBase.cs
// Base class for 3D Vectrosity Classes that generate VectorObjects .
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Base class for 3D Vectrosity Classes that generate VectorObjects .
    /// </summary>
    public abstract class A3DVectrosityBase : AVectrosityBase {

        private GameColor _color;
        public GameColor Color {
            get { return _color; }
            set { SetProperty<GameColor>(ref _color, value, "Color", OnColorChanged); }
        }

        private float _lineWidth;
        public float LineWidth {
            get { return _lineWidth; }
            set { SetProperty<float>(ref _lineWidth, value, "LineWidth", OnLineWidthChanged); }
        }

        //private Vector3[] _points;
        //public Vector3[] Points {
        //    get { return _points; }
        //    set { SetProperty<Vector3[]>(ref _points, value, "Points", OnPointsChanged); }
        //}
        private IList<Vector3> _points;
        public IList<Vector3> Points {
            get { return _points; }
            set { SetProperty<IList<Vector3>>(ref _points, value, "Points", OnPointsChanged); }
        }


        protected Transform _target;    // can be null as GridWireframe doesn t use a target Transform
        private LineType _lineType;

        /// <summary>
        /// Initializes a new instance of the <see cref="A3DVectrosityBase" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="points">The points.</param>
        /// <param name="target">The transform that this line follows in the scene.</param>
        /// <param name="lineType">Type of the line.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public A3DVectrosityBase(string name, IList<Vector3> points, Transform target, LineType lineType = LineType.Discrete, float width = 1F, GameColor color = GameColor.White)
            : base(name) {
            _points = points;
            _target = target;
            _lineType = lineType;
            _lineWidth = width;
            _color = color;
        }
        //public A3DVectrosityBase(string name, Vector3[] points, Transform target, LineType lineType = LineType.Discrete, float width = 1F, GameColor color = GameColor.White)
        //    : base(name) {
        //    _points = points;
        //    _target = target;
        //    _lineType = lineType;
        //    _lineWidth = width;
        //    _color = color;
        //}

        protected override void Initialize() {
            //VectorLine.canvas3D.gameObject.layer = (int)Layers.TransparentFX;   // make the 3D canvas visible to the default 3D camera, aka mainCamera

            _line = new VectorLine(LineName, new List<Vector3>(Points), texture, LineWidth, _lineType);
            _line.color = Color.ToUnityColor(); // color removed from constructor in Vectrosity 4.0
            _line.layer = (int)Layers.TransparentFX;

            if (_target != null) { _line.drawTransform = _target; } // added as Vectrosity 3.0 removed Draw3D(Transform)
        }
        //protected override void Initialize() {
        //    VectorLine.canvas3D.gameObject.layer = (int)Layers.TransparentFX;   // make the 3D canvas visible to the default 3D camera, aka mainCamera

        //    _line = new VectorLine(LineName, _points, material, LineWidth, _lineType);
        //    _line.color = Color.ToUnityColor(); // color removed from constructor in Vectrosity 4.0

        //    if (_target != null) { _line.drawTransform = _target; } // added as Vectrosity 3.0 removed Draw3D(Transform)
        //}

        /// <summary>
        /// Shows or hides a VectorLine that moves with the provided <c>target</c> if not null.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
        public virtual void Show(bool toShow) {
            if (_line == null) {
                Initialize();
            }
            if (_job != null && _job.IsRunning) {
                _job.Kill();
                _job = null;
            }

            if (toShow) {
                _job = new Job(DrawLine(), toStart: true, onJobComplete: delegate {
                    // TODO
                });
            }
            _line.active = toShow;
        }

        private IEnumerator DrawLine() {
            while (true) {
                Draw3D();
                yield return null;
            }
        }

        protected virtual void Draw3D() {
            _line.Draw3D();  // _line.Draw3D(_target);  removed by Vectrosity 3.0
        }

        private void OnColorChanged() {
            if (_line != null) {
                _line.SetColor(Color.ToUnityColor());
            }
        }

        private void OnLineWidthChanged() {
            if (_line != null) {
                _line.lineWidth = LineWidth;
            }
        }

        private void OnPointsChanged() {
            if (_line != null) {
                _line.points3.Clear();  //_line.Resize(Points); removed by Vectrosity 4.0
                //D.Log("{0}.OnPointsChanged called. Adding {1} points.", GetType().Name, Points.Length);
                _line.points3.AddRange(Points);
            }
        }
    }
}

