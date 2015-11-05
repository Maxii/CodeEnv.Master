// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GridWireframe_Alt.cs
// Generates an entire Grid of Sectors as a Wireframe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Generates an entire Grid of Sectors as a Wireframe. This alternative implementation
    /// uses VectorManager.ObjectSetup which enables Brightness (fog) control and visibility controls using
    /// OnBecameVisible/Invisible.
    /// </summary>
    public class GridWireframe_Alt : APropertyChangeTracking {

        private Transform _target;

        //private Transform _parent;
        ///// <summary>
        ///// The parent transform where you want the wireframe line object to reside in the scene.
        ///// </summary>
        //public Transform Parent {
        //    get { return _parent; }
        //    set { SetProperty<Transform>(ref _parent, value, "Parent", OnParentChanged); }
        //}

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

        private string _lineName;
        public string LineName {
            get { return _lineName; }
            set { SetProperty<string>(ref _lineName, value, "LineName", OnLineNameChanged); }
        }

        private VectorLine _line;
        private IList<Vector3> _points;
        private Visibility _visibility;

        public GridWireframe_Alt(string name, Transform target, IList<Vector3> points, Visibility visibility = Visibility.Always, float width = 1F, GameColor color = GameColor.Gray) {
            _lineName = name;
            _target = target;
            _points = points;
            _visibility = visibility;
            _lineWidth = width;
            _color = color;
        }

        public void Show(bool toShow) {
            if (!toShow && _line == null) {
                return;
            }
            if (_line == null) {
                _line = new VectorLine(LineName, new List<Vector3>(_points), null, LineWidth);
                _line.color = Color.ToUnityColor(); // color removed from constructor by Vectrosity 4.0

                VectorManager.useDraw3D = true;
                VectorManager.ObjectSetup(_target.gameObject, _line, _visibility, Brightness.Fog);
                VectorManager.SetBrightnessParameters(5000F, 250F, 32, .2F, GameColor.Clear.ToUnityColor());
                // NOTE: Using makeBounds: false means this CubeWireframe object does not automatically gain a renderer and invisible mesh. This renderer 
                // and mesh enables OnBecameVisible/Invisible which overrides the line.active on/off commands I use to show/not show the line. There is another
                // alternative to retain control: destroy this SectorWireframe gameobject each time I turn the line off (and then rebuild it of course). Destroying 
                // the VectorLine itself when using VectorManager.ObjectSetup will result in an error.
            }
            _line.active = toShow;
            D.Log("{0} line.active = {1}.", this.GetType().Name, toShow);
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

        private void OnLineNameChanged() {
            if (_line != null) {
                _line.name = LineName;
            }
        }

        // to Cleanup the VectorLine, just destroy the target GameObject ObjectSetup
        // is using. This is the proper way to destroy this type of VectorLine.

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

