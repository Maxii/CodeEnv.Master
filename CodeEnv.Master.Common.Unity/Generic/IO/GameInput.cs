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

namespace CodeEnv.Master.Common.Unity {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Game Input class that receives and holds Ngui events intended for the camera.
    /// </summary>
    public class GameInput {

        private float _scrollWheelDelta;
        public bool isScrolling;
        public void RecordScrollWheelMovement(float delta) {
            _scrollWheelDelta = delta;
            isScrolling = true;
        }

        public float GetScrollWheelMovement() {
            if (!isScrolling) {
                D.Warn("Mouse ScrollWheel inquiry made with no scroll value waiting.");
            }
            isScrolling = false;
            float delta = _scrollWheelDelta;
            _scrollWheelDelta = Constants.ZeroF;
            return delta;
        }

        // Unlike ClearDrag, ClearScrollWheel not needed as all scroll wheel movement
        // events recorded here will always be used. 

        private Vector2 _dragDelta;
        public bool isDragging;
        public void RecordDrag(Vector2 delta) {
            _dragDelta = delta;
            isDragging = true;
        }

        public Vector2 GetDragDelta() {
            if (!isDragging) {
                D.Warn("Drag inquiry made with no delta value waiting.");
            }
            Vector2 delta = _dragDelta;
            ClearDrag();
            return delta;
        }

        public void OnDragEnd() {
            // the drag has ended so clear any residual drag values that
            // may not have qualified for use by Camera Control due to wrong button, etc.
            ClearDrag();
        }

        private void ClearDrag() {
            _dragDelta = Vector2.zero;
            isDragging = false;
        }

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

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}


