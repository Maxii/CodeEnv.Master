// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GlobeManager.cs
// Manages the spherical globes that are a part of Cellestial Bodies.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the spherical globes that are a part of Cellestial Bodies. 
/// </summary>
[Serializable, RequireComponent(typeof(SphereCollider), typeof(MeshRenderer))]
public class GlobeManager : AMonoBehaviourBase, ICameraFocusable {

    [SerializeField]
    private GlobeMaterialAnimator primaryMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = 0.015F, yScrollSpeed = 0.015F };
    [SerializeField]
    private GlobeMaterialAnimator optionalSecondMaterialAnimator = new GlobeMaterialAnimator { xScrollSpeed = -0.015F, yScrollSpeed = -0.015F };

    // Cached references
    private GameEventManager _eventMgr;
    private Transform _transform;
    private SphereCollider _collider;
    private Material _primaryMaterial;
    private Material _optionalSecondMaterial;
    //private Color _startingColor;

    private StringBuilder hudMsg;
    private IGuiCursorHud _cursorHud;

    void Awake() {
        _eventMgr = GameEventManager.Instance;
        _transform = transform;
        _collider = collider as SphereCollider;
        UpdateRate = UpdateFrequency.Continuous;
    }

    void Start() {
        Renderer globeRenderer = renderer;
        _primaryMaterial = globeRenderer.material;
        //_startingColor = _primaryMaterial.color;
        if (globeRenderer.materials.Length > 1) {
            _optionalSecondMaterial = globeRenderer.materials[1];
        }
        _cursorHud = GuiCursorHud.Instance;
        hudMsg = ConstructMsgForHud();
    }

    private StringBuilder ConstructMsgForHud() {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("GlobeManager to Cursor HUD.");
        sb.AppendFormat("My name is {0}.", _transform.parent.parent.name);
        return sb;
    }

    void Update() {
        if (ToUpdate()) {
            AnimateGlobeRotation();
        }
    }

    private void AnimateGlobeRotation() {
        float time = GameTime.TimeInCurrentSession; // TODO convert animation to __GameTime.DeltaTime * (int)UpdateRate
        if (_primaryMaterial != null) {  // OPTIMIZE can remove. Only needed for testing
            primaryMaterialAnimator.Animate(_primaryMaterial, time);
        }
        // Added for IOS compatibility? IMPROVE
        if (_optionalSecondMaterial != null) {
            optionalSecondMaterialAnimator.Animate(_optionalSecondMaterial, time);
        }
    }

    #region Unity Events
    // Unity Event System is interfered with when NGUI's UICamera is attached to the camera, even if disabled

    //void OnMouseEnter() {
    //    Debug.Log("GlobeManager.OnMouseEnter() called.");
    //    _primaryMaterial.color = Color.black;    // IMPROVE need better highlighting
    //}

    //void OnMouseExit() {
    //    Debug.Log("GlobeManager.OnMouseExit() called.");
    //    _primaryMaterial.color = _startingColor;
    //}

    //void OnMouseOver() {
    //    Debug.Log("GlobeManager.OnMouseOver() called.");
    //    if (GameInput.IsMiddleMouseButtonClick()) {
    //        _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
    //    }
    //}
    #endregion

    #region NGUI Events
    public void OnClick() {
        //Debug.Log("GlobeManager.OnClick() called.");
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    void OnHover(bool isOver) {
        if (isOver) {
            //Debug.Log("GlobeManager.OnHover(true) called.");
            //_primaryMaterial.color = Color.black;    // IMPROVE need better highlighting
            //_eventMgr.Raise<CursorHudTextEvent>(new CursorHudTextEvent(this, hudMsg));
            _cursorHud.Set(hudMsg.ToString());
        }
        else {
            //Debug.Log("GlobeManager.OnHover(false) called.");
            //_primaryMaterial.color = _startingColor;    // IMPROVE need better highlighting
            //_eventMgr.Raise<CursorHudTextEvent>(new CursorHudTextEvent(this, emptyGlobeInfo));
            _cursorHud.Clear();
        }
    }
    #endregion

    void OnBecameVisible() {
        //Debug.Log("A GlobeManager has become isTargetVisibleThisFrame.");
        EnableAllScripts(true);
    }

    void OnBecameInvisible() {
        //Debug.Log("A GlobeManager has become invisible.");
        EnableAllScripts(false);
    }

    // IMPROVE disabling all the scripts in this Sun is not logically part of GlobeMgmt 
    // mission but this script needs a renderer to receive OnBecameInvisible()...
    private void EnableAllScripts(bool toEnable) {
        // acquiring all scripts in advance at startup results in some of them already being destroyed when called during exit
        Transform globeParent = _transform.parent;
        if (globeParent != null) {
            MonoBehaviour[] allSunScripts = globeParent.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in allSunScripts) {
                s.enabled = toEnable;
            }
        }
    }

    [Serializable]
    public class GlobeMaterialAnimator {
        public float xScrollSpeed = 0.015F;
        public float yScrollSpeed = 0.015F;

        internal void Animate(Material material, float time) {
            Vector2 textureOffset = new Vector2(xScrollSpeed * time % 1, yScrollSpeed * time % 1);
            material.SetTextureOffset(UnityConstants.MainDiffuseTexture, textureOffset);
            material.SetTextureOffset(UnityConstants.NormalMapTexture, textureOffset);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 10.0F;

    private float _optimalCameraViewingDistance;
    public float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.radius * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraTargetable Members

    [SerializeField]
    private float minimumCameraViewingDistanceMultiplier = 4.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = _collider.radius * minimumCameraViewingDistanceMultiplier;
            }
            return _minimumCameraViewingDistance;
        }
    }

    #endregion


}

