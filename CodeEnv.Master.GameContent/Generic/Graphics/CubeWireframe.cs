// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CubeWireframe.cs
// Class that controls the display of a rectangular box wireframe around a gameobject.
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
    /// Class that controls the display of a rectangular box wireframe around a gameobject. 
    /// 
    /// </summary>
    public class CubeWireframe : APropertyChangeTracking, IDisposable {

        private Transform _target;
        /// <summary>
        /// The transform that this cube wireframe is drawn around.
        /// </summary>
        public Transform Target {
            get { return _target; }
            set { SetProperty<Transform>(ref _target, value, "Target", OnTargetChanged); }
        }

        private Transform _parent;
        /// <summary>
        /// The parent transform where you want the wireframe line object to reside in the scene.
        /// </summary>
        public Transform Parent {
            get { return _parent; }
            set { SetProperty<Transform>(ref _parent, value, "Parent", OnParentChanged); }
        }

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

        private Vector3 _size;
        public Vector3 Size {
            get { return _size; }
            set { SetProperty<Vector3>(ref _size, value, "Size", OnSizeChanged); }
        }

        private string _lineName;
        public string LineName {
            get { return _lineName; }
            set { SetProperty<string>(ref _lineName, value, "LineName", OnLineNameChanged); }
        }

        private VectorLine _line;
        private Visibility _visibility;

        public CubeWireframe(string name, Transform target, Vector3 cubeSize, Visibility visibility = Visibility.Static, float width = 1F, GameColor color = GameColor.White) {
            _lineName = name;
            _target = target;
            _size = cubeSize;
            _visibility = visibility;
            _lineWidth = width;
            _color = color;
        }

        public void Show(bool toShow) {
            if (!toShow && _line == null) {
                return;
            }
            if (_line == null) {
                _line = new VectorLine(LineName, new Vector3[24], Color.ToUnityColor(), null, LineWidth, LineType.Discrete);
                _line.MakeCube(Vector3.zero, Size.x, Size.y, Size.z);
                if (Parent != null) {
                    OnParentChanged();
                }
                VectorManager.ObjectSetup(Target.gameObject, _line, _visibility, Brightness.None);
            }
            _line.active = toShow;
        }

        private void OnTargetChanged() {
            VectorManager.ObjectSetup(Target.gameObject, _line, _visibility, Brightness.None);
        }

        private void OnParentChanged() {
            if (_line != null) {
                _line.vectorObject.transform.parent = Parent;
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

        private void OnSizeChanged() {
            if (_line != null) {
                _line.MakeCube(Vector3.zero, Size.x, Size.y, Size.z);
            }
        }

        private void OnLineNameChanged() {
            if (_line != null) {
                _line.name = LineName;
            }
        }

        private void Cleanup() {
            VectorLine.Destroy(ref _line);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

