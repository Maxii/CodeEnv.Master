// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyCustomCursor.cs
// Singleton. My implementation of Ngui's UICursor to allow custom sprites to replace the cursor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. My implementation of Ngui's UICursor to allow custom sprites to replace the cursor.
/// </summary>
public class MyCustomCursor : AMonoSingleton<MyCustomCursor> {

    public string DebugName { get { return GetType().Name; } }

    // Camera used to draw this cursor
    public Camera uiCamera;

    private UISprite _cursorSprite;
    private AtlasID _originalCursorSpriteAtlasID;
    private string _originalCursorSpriteName;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        // TODO  
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
    }

    #endregion

    private void InitializeValuesAndReferences() {
        _cursorSprite = gameObject.GetSingleComponentInChildren<UISprite>();

        if (uiCamera == null) {
            uiCamera = NGUITools.FindCameraForLayer(gameObject.layer);
        }

        D.AssertNull(_cursorSprite.atlas);
        _originalCursorSpriteAtlasID = AtlasID.None;

        _originalCursorSpriteName = _cursorSprite.spriteName;
        if (_cursorSprite.depth < 100) {
            _cursorSprite.depth = 100;
        }
        enabled = false;
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Reposition the widget.
    /// </summary>
    void Update() {
        Vector3 mousePos = Input.mousePosition;

        if (uiCamera != null) {
            // Since the screen can be of different than expected size, we want to convert
            // mouse coordinates to view space, then convert that to world position.
            mousePos.x = Mathf.Clamp01(mousePos.x / Screen.width);
            mousePos.y = Mathf.Clamp01(mousePos.y / Screen.height);
            transform.position = uiCamera.ViewportToWorldPoint(mousePos);

            // For pixel-perfect results
            if (uiCamera.orthographic) {
                Vector3 lp = transform.localPosition;
                lp.x = Mathf.Round(lp.x);
                lp.y = Mathf.Round(lp.y);
                transform.localPosition = lp;
            }
        }
        else {
            // Simple calculation that assumes that the camera is of fixed size
            mousePos.x -= Screen.width * 0.5F;
            mousePos.y -= Screen.height * 0.5F;
            mousePos.x = Mathf.Round(mousePos.x);
            mousePos.y = Mathf.Round(mousePos.y);
            transform.localPosition = mousePos;
        }
    }

    #endregion

    /// <summary>
    /// Override the cursor with the specified sprite.
    /// </summary>
    public static void Set(AtlasID atlasID, string spriteName) {
        if (_instance != null) {
            _instance._cursorSprite.atlas = atlasID.GetAtlas();
            _instance._cursorSprite.spriteName = spriteName;
            _instance._cursorSprite.MakePixelPerfect();
            _instance.Update();
            _instance.enabled = true;
        }
    }

    /// <summary>
    /// Override the cursor with the specified sprite.
    /// <remarks>6.21.17 Needed until all my textures are the correct size for the game.</remarks>
    /// </summary>
    public static void Set(AtlasID atlasID, string spriteName, int width, int height) {
        if (_instance != null) {
            _instance._cursorSprite.atlas = atlasID.GetAtlas();
            _instance._cursorSprite.spriteName = spriteName;
            _instance._cursorSprite.width = width;
            _instance._cursorSprite.height = height;
            ////_instance._cursorSprite.MakePixelPerfect();
            _instance.Update();
            _instance.enabled = true;
        }
    }

    /// <summary>
    /// Clear the cursor back to its original value.
    /// </summary>
    public static void Clear() {
        if (_instance != null) {
            Set(_instance._originalCursorSpriteAtlasID, _instance._originalCursorSpriteName);
            _instance.enabled = false;
        }
    }

    #region Cleanup

    protected override void Cleanup() {
        // TODO
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }

}

