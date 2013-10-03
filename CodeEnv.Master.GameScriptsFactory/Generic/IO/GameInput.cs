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

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using System.Linq;

/// <summary>
/// Singleton Game Input class that receives and records Mouse and SpecialKey events not intended for the Gui.
/// The mouse events come from the Ngui event system and the SpecialKey events come from Unity's Input class.
/// WARNING: Must remain a MonoBehaviour as Update must run every frame to catch single-shot key strokes
/// </summary>
public class GameInput : AMonoBehaviourBaseSingleton<GameInput> {

    private SpecialKeys[] _specialKeysExcludingDefault;
    private KeyCode[] _specialKeyCodesToSearch;

    protected override void Awake() {
        base.Awake();
        Initialize();
        // Must check every frame as Input.isKeyDown is only true during the frame in which it occurs
        UpdateRate = FrameUpdateFrequency.Continuous;
    }

    protected void Initialize() {
        _specialKeysExcludingDefault = Enums<SpecialKeys>.GetValues().Except(default(SpecialKeys)).ToArray();
        _specialKeyCodesToSearch = _specialKeysExcludingDefault.Select(sk => (KeyCode)sk).ToArray<KeyCode>();
    }

    #region ScrollWheel

    [HideInInspector]
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

    public bool IsDragging { get; private set; }

    [HideInInspector]
    public bool isDragValueWaiting;
    private Vector2 _dragDelta;
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

    #region Clicking

    public void OnClickOnNothing() {
        if (GameInputHelper.IsLeftMouseButton()) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    #endregion

    #region SpecialKeys

    // Ngui KeyEvents didn't work as only one event is sent when keys are held down
    // Also the approach below was flawed as the key was consumed and reset even if it
    // wasn't the right key, so it precludes separate algorithms testing for separate keys

    private KeyCode _specialKeyPressed;
    public KeyCode SpecialKeyPressed {
        get { return _specialKeyPressed; }
        set { SetProperty<KeyCode>(ref _specialKeyPressed, value, "SpecialKeyPressed"); }
    }

    void Update() {
        if (ToUpdate()) {
            if (GameInputHelper.IsAnyKeyOrMouseButtonDown()) {
                KeyCode keyPressed;
                if (GameInputHelper.TryIsKeyDown(out keyPressed, _specialKeyCodesToSearch)) {
                    SpecialKeyPressed = keyPressed;
                }
            }
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


