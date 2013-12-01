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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections;
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

        private Vector3[] _points;
        public Vector3[] Points {
            get { return _points; }
            set { SetProperty<Vector3[]>(ref _points, value, "Points", OnPointsChanged); }
        }

        protected Transform _target;

        /// <summary>
        /// Initializes a new instance of the <see cref="A3DVectrosityBase" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="points">The points.</param>
        /// <param name="target">The transform that this line follows in the scene.</param>
        /// <param name="parent">The parent to attach the VectorObject too.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public A3DVectrosityBase(string name, Vector3[] points, Transform target, Transform parent = null, float width = 1F, GameColor color = GameColor.White)
            : base(name, parent) {
            _points = points;
            _target = target;
            _lineWidth = width;
            _color = color;
        }

        protected override void Initialize() {
            _line = new VectorLine(LineName, _points, Color.ToUnityColor(), material, LineWidth);
            if (Parent != null) { OnParentChanged(); }
        }

        /// <summary>
        /// Shows or hides a line emanating from the target.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
        public virtual void Show(bool toShow) {
            if (_line == null) {
                Initialize();
            }
            if (toShow) {
                if (_job == null) {
                    _job = new Job(DrawLine(), toStart: true, onJobComplete: delegate {
                        // TODO
                    });
                }
                else if (!_job.IsRunning) {
                    _job.Start();
                }
            }
            else if (_job != null && _job.IsRunning) {
                _job.Kill();
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
            if (_target != null) {
                _line.Draw3D(_target);
            }
            else {
                _line.Draw3D();
            }
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
                _line.Resize(Points);
            }
        }
    }
}

