// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInput.cs
// Singleton Game Input class that receives and records Mouse and SpecialKey events not intended for the Gui.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;
    using System.Linq;
    using CodeEnv.Master.GameContent;
    using System;

    /// <summary>
    /// Singleton Game Input class that receives and records Mouse and special Key events not intended for the Gui.
    /// The mouse events come from the Ngui event system and the key events come from Unity's Input class.
    /// </summary>
    public class GameInput : AGenericSingleton<GameInput> {

        // WARNING: This class is referenced in CameraControl's Configuration classes which are initialized outside of Awake or Start. 
        // As a result, GameInput is instantiated at EDITOR TIME, before runtime even starts. Therefore, this call to References to provide
        // a GameInputHelper reference will return null, as References will only be initialized at the beginning of Runtime. I work around
        // this by using the reference held by References directly in the code, code that will only be executed during runtime.
        //public IGameInputHelper _inputHelper = References.InputHelper

        private KeyCode[] _viewModeKeyCodesToSearch;

        private GameInput() {
            Initialize();
        }

        protected override void Initialize() {
            ViewModeKeys[] _viewModeKeysExcludingDefault = Enums<ViewModeKeys>.GetValues().Except(default(ViewModeKeys)).ToArray();
            _viewModeKeyCodesToSearch = _viewModeKeysExcludingDefault.Select(sk => (KeyCode)sk).ToArray<KeyCode>();
        }

        #region ScrollWheel

        public bool isScrollValueWaiting;
        private float _scrollWheelDelta;
        public void RecordScrollWheelMovement(float delta) {
            _scrollWheelDelta = delta;
            isScrollValueWaiting = true;
        }

        public float GetScrollWheelMovement() {
            if (!isScrollValueWaiting) {
                D.Warn("Mouse ScrollWheel inquiry made with no scroll value waiting.");
            }
            isScrollValueWaiting = false;
            float delta = _scrollWheelDelta;
            _scrollWheelDelta = Constants.ZeroF;
            return delta;
        }

        // Unlike ClearDrag, ClearScrollWheel not needed as all scroll wheel movement
        // events recorded here will always be used. 

        #endregion

        #region Dragging

        public bool IsDragging { get; set; }

        public bool isDragValueWaiting;
        private Vector2 _dragDelta;
        public void RecordDrag(Vector2 delta) {
            _dragDelta = delta;
            isDragValueWaiting = true;
        }

        public Vector2 GetDragDelta() {
            if (!isDragValueWaiting) {
                D.Warn("Drag inquiry made with no delta value waiting.");
            }
            Vector2 delta = _dragDelta;
            ClearDragValue();
            return delta;
        }

        /// <summary>
        /// Tells GameInput that the drag that was occuring has ended.
        /// </summary>
        public void NotifyDragEnded() {
            // the drag has ended so clear any residual drag values that
            // may not have qualified for use by Camera Control due to wrong button, etc.
            ClearDragValue();
            IsDragging = false;
        }

        private void ClearDragValue() {
            _dragDelta = Vector2.zero;
            isDragValueWaiting = false;
        }

        #endregion

        #region Clicked

        // used by SelectionManager to clear the Selection when an unconsumed click occurs
        public event Action<NguiMouseButton> onUnconsumedClick;

        /// <summary>
        /// Called when a mouse button click event occurs but no collider consumes it.
        /// </summary>
        public void OnUnconsumedClick() {
            var temp = onUnconsumedClick;
            if (temp != null) {
                temp(References.InputHelper.GetMouseButton());
            }
        }

        #endregion

        #region Pressed

        // Previously used by SectorExaminer to open a contextMenu without using a collider.
        // This works but I replaced the Wireframe Mouse hot spot with a small collider instead
        // as the same approach using onUnconsumedHover was unreliable due to the way Ngui
        // spams onHover events

        //public event Action<NguiMouseButton, bool> onUnconsumedPress;

        //public void RecordUnconsumedPress(bool isDown) {
        //    if (!IsDragging) {  // if dragging, the press shouldn't have any meaning except related to terminating a drag
        //        var d = onUnconsumedPress;
        //        if (d != null) {
        //            d(GameInputHelper.GetMouseButton(), isDown);
        //        }
        //    }
        //}

        #endregion


        #region SpecialKeys

        // Ngui KeyEvents didn't work as only one event is sent when keys are held down
        // and even then, only selected keys were included

        // activates ViewMode in PlayerViews
        public event Action<ViewModeKeys> onViewModeKeyPressed;

        public void CheckForKeyActivity() {
            IGameInputHelper inputHelper = References.InputHelper;
            if (inputHelper.IsAnyKeyOrMouseButtonDown()) {
                KeyCode keyPressed;
                if (inputHelper.TryIsKeyDown(out keyPressed, _viewModeKeyCodesToSearch)) {
                    var temp = onViewModeKeyPressed;
                    if (temp != null) {
                        temp((ViewModeKeys)keyPressed);
                    }
                }
            }
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}


