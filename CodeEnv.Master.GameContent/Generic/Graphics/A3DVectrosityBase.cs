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
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;
    using Vectrosity;

    /// <summary>
    /// Base class for 3D Vectrosity Classes that generate VectorObjects .
    /// </summary>
    public abstract class A3DVectrosityBase : AVectrosityBase {

        #region LineManager Relocation Archive

        /******************************************************************************************
        * IDEA: I can relocate the Vectrosity LineManager to wherever I want to cleanup my root
        * hierarchy. The code below works fine. However, when the scene is about to change I would 
        * then have to move it back to the root so it's DontDestroyOnLoad would work. Otherwise,
        * LineManager is destroyed and will not be recreated when the next scene appears causing
        * a Unity error when the new Vectrosity objects created in the new scene try to access it.
        * This will involve a fair amount of work (like I did in SystemCreator) to make sure the
        * correct static states are handled, along with a lot of event subscriptions from all these
        * Vectrosity3D objects. The benefit is small so I've commented all this out for now.
        *******************************************************************************************/

        //private static bool _isLineManagerRelocated = false;

        //private static void RelocateLineManager() {
        //    if (!_isLineManagerRelocated) {
        //        LineManager vectrosityLineMgr = GameObject.FindObjectOfType<LineManager>();
        //        if (vectrosityLineMgr != null) {
        //            vectrosityLineMgr.transform.parent = References.DynamicObjectsFolder.Folder;
        //            _isLineManagerRelocated = true;
        //        }
        //    }
        //}

        #endregion

        private GameColor _color;
        public GameColor Color {
            get { return _color; }
            set { SetProperty<GameColor>(ref _color, value, "Color", ColorPropChangedHandler); }
        }

        private float _lineWidth;
        public float LineWidth {
            get { return _lineWidth; }
            set { SetProperty<float>(ref _lineWidth, value, "LineWidth", LineWidthPropChangedHandler); }
        }

        private List<Vector3> _points;  // must be a List<> (not IList<>) to allow access to the list's capacity during InitializeLine
        public List<Vector3> Points {
            get { return _points; }
            set { SetProperty<List<Vector3>>(ref _points, value, "Points", PointsPropChangedHandler); }
        }

        protected Transform _target;    // can be null as GridWireframe doesn't use a target Transform

        private LineType _lineType;
        private Transform _lineParent;

        /// <summary>
        /// Initializes a new instance of the <see cref="A3DVectrosityBase" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="points">The points.</param>
        /// <param name="target">The transform that this line follows in the scene.</param>
        /// <param name="lineParent">The line parent.</param>
        /// <param name="lineType">Type of the line.</param>
        /// <param name="width">The width.</param>
        /// <param name="color">The color.</param>
        public A3DVectrosityBase(string name, List<Vector3> points, Transform target, Transform lineParent, LineType lineType = LineType.Discrete, float width = 1F, GameColor color = GameColor.White)
            : base(name) {
            _points = points;
            _target = target;
            _lineParent = lineParent;
            _lineType = lineType;
            _lineWidth = width;
            _color = color;
            VectorLine.SetCamera3D(References.MainCameraControl.MainCamera_Far);    // eliminates most jitter using MainCamera_Far
        }

        protected virtual void Initialize() {

            /*********************************************************************************************************************************************
             * GOTCHA! The new VectorLine 5.0 constructor relies on the capacity of the list when Points.Count is 0. 
             * If Points.Count is 0, new List(Points) creates an empty list with ZERO capacity, it DOES NOT copy the capacity of the Points list!
             *********************************************************************************************************************************************/

            var points = new List<Vector3>(Points.Capacity);
            points.AddRange(Points);
            D.Log("List being used for Line creation: Capacity = {0}, Count = {1}.", points.Capacity, points.Count);
            _line = new VectorLine(LineName, points, texture, LineWidth, _lineType);
            _line.color = Color.ToUnityColor(); // color removed from constructor in Vectrosity 4.0
            _line.layer = (int)Layers.TransparentFX;    // make the line visible to the mainCamera. line.layer added in Vectrosity 5.0

            if (_target != null) { _line.drawTransform = _target; } // added as Vectrosity 3.0 removed Draw3D(Transform)
            _line.active = false;
            //RelocateLineManager();    // see Relocate LineManager Archive
        }

        /// <summary>
        /// Shows or hides a VectorLine that moves with the provided <c>target</c> if not null.
        /// </summary>
        /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
        public void Show(bool toShow) {
            if (IsShowing == toShow) {
                //D.Log("{0}.Show({1}) is a duplicate.", GetType().Name, toShow);
                return;
            }

            if (toShow) {
                if (_line == null) {
                    Initialize();
                }
                D.Assert(!IsLineActive);
                _line.active = true;
                HandleLineActivated();
                AssignParent(_lineParent);
                if (!_line.isAutoDrawing) {
                    _line.Draw3DAuto();
                }
            }
            else {
                D.Assert(IsLineActive);
                _line.active = false;
                HandleLineDeactivated();
            }
        }

        /// <summary>
        /// Assigns the provided parent to this VectorLine object. Derived classes
        /// that add other VectorLine objects (like points) to this line should override
        /// this method and assign the parent to their object using
        /// VectorLine.rectTransform.SetParent(lineParent, worldPositionStays: true);
        /// </summary>
        /// <param name="lineParent">The line parent.</param>
        protected virtual void AssignParent(Transform lineParent) {
            D.Assert(IsLineActive);
            _line.Draw3D();   // Eric5h5: Active line must be drawn once before the parent can be set
            _line.rectTransform.SetParent(lineParent, worldPositionStays: true);
        }

        /// <summary>
        /// Hook that is called immediately after the VectorLine _line is activated.
        /// Default does nothing.
        /// </summary>
        protected virtual void HandleLineActivated() { }

        /// <summary>
        /// Hook that is called immediately after the VectorLine _line is deactivated.
        /// Default does nothing.
        /// </summary>
        protected virtual void HandleLineDeactivated() { }

        #region Event and Property Change Handlers

        private void ColorPropChangedHandler() {
            if (_line != null) {
                _line.SetColor(Color.ToUnityColor());
            }
        }

        private void LineWidthPropChangedHandler() {
            if (_line != null) {
                _line.lineWidth = LineWidth;
            }
        }

        private void PointsPropChangedHandler() {
            if (_line != null) {
                _line.points3.Clear();  //_line.Resize(Points); removed by Vectrosity 4.0
                D.Log("{0}.PointsPropChangedHandler called. Adding {1} points.", LineName, Points.Count);
                _line.points3.AddRange(Points);
            }
        }

        #endregion

        protected override void Cleanup() {
            if (_line != null && _line.isAutoDrawing) {
                _line.StopDrawing3DAuto();  // stop auto drawing before _line destroyed
            }
            base.Cleanup();
        }

    }
}

