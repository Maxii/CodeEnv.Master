// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameInput.cs
// Game Input class that receives and holds Ngui events intended for the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Game Input class that receives and holds Ngui events intended for the camera.
    /// </summary>
    public class GameInput {

        #region ScrollWheel

        private float _scrollWheelDelta;
        public bool isScrollValueWaiting;
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
        public bool IsDragging { get; private set; }

        private Vector2 _dragDelta;
        public bool isDragValueWaiting;
        public void RecordDrag(Vector2 delta) {
            _dragDelta = delta;
            isDragValueWaiting = true;
            IsDragging = true;
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

        #region ArrowKeys

        private KeyCode _arrowKeyPressed;
        public bool isArrowKeyPressed;
        public void RecordKey(KeyCode key) {
            if (key == KeyCode.LeftArrow || key == KeyCode.RightArrow ||
                key == KeyCode.UpArrow || key == KeyCode.DownArrow) {
                _arrowKeyPressed = key;
                isArrowKeyPressed = true;
            }
        }

        public KeyCode GetArrowKey() {
            if (!isArrowKeyPressed) {
                D.Warn("Key inquiry made with no arrow key waiting.");
            }
            KeyCode key = _arrowKeyPressed;
            _arrowKeyPressed = KeyCode.None;
            isArrowKeyPressed = false;
            return key;
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}


