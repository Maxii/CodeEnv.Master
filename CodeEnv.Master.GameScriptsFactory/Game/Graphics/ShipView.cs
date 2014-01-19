// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipView.cs
//  A class for managing the UI of a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a ship.
/// </summary>
public class ShipView : AElementView, ISelectable {
    //public class ShipView : AFollowableView, IShipViewable, ISelectable {

    public new ShipPresenter Presenter {
        get { return base.Presenter as ShipPresenter; }
        protected set { base.Presenter = value; }
    }

    //public AudioClip dying;
    //private AudioSource _audioSource;

    //private Color _originalMeshColor_Main;
    //private Color _originalMeshColor_Specular;
    //private Color _hiddenMeshColor;
    //private Renderer _renderer;

    private CtxObject _ctxObject;

    //private Job _showingJob;
    private VelocityRay _velocityRay;

    //protected override void Awake() {
    //    base.Awake();
    //    _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
    //    circleScaleFactor = 1.0F;
    //    InitializeMesh();
    //}

    protected override void InitializePresenter() {
        Presenter = new ShipPresenter(this);
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
    }

    #region ContextMenu

    private void __InitializeContextMenu() {    // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        CtxMenu shipMenu = GuiManager.Instance.gameObject.GetSafeMonoBehaviourComponentsInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "ShipMenu");
        _ctxObject.contextMenu = shipMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        // UNDONE
    }

    private void OnContextMenuSelection() {
        // int itemId = CtxObject.current.selectedItem;
        // D.Log("{0} selected context menu item {1}.", _transform.name, itemId);
        // UNDONE
    }

    private void OnContextMenuHide() {
        // UNDONE
    }

    #endregion

    //protected override void OnClick() {
    //    base.OnClick();
    //    if (IsDiscernible) {
    //        if (GameInputHelper.IsLeftMouseButton()) {
    //            KeyCode notUsed;
    //            if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
    //                OnAltLeftClick();
    //            }
    //            else { 
    //                OnLeftClick(); 
    //            }
    //        }
    //    }
    //}

    //private void OnLeftClick() {
    //    IsSelected = true;
    //}

    protected override void OnLeftClick() {
        base.OnLeftClick();
        IsSelected = true;
    }

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        Presenter.__SimulateAttacked();
    }

    //private void OnAltLeftClick() {
    //    Presenter.__SimulateAttacked();
    //}

    //protected override void OnIsDiscernibleChanged() {
    //    base.OnIsDiscernibleChanged();
    //    ShowMesh(IsDiscernible);
    //    ShowVelocityRay(IsDiscernible);
    //}

    protected override void OnIsDiscernibleChanged() {
        base.OnIsDiscernibleChanged();
        ShowVelocityRay(IsDiscernible);
    }


    private void OnIsSelectedChanged() {
        if (IsSelected) {
            Presenter.OnIsSelected();
        }
        AssessHighlighting();
    }

    void OnPress(bool isDown) {
        if (GameInputHelper.IsRightMouseButton()) {
            OnRightPress(isDown);
        }
    }

    private void OnRightPress(bool isDown) {
        if (IsDiscernible) {
            if (IsSelected) {
                Presenter.RequestContextMenu(isDown);
            }
        }
    }

    void OnDoubleClick() {
        if (GameInputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    private void OnLeftDoubleClick() {
        if (IsDiscernible) {
            SelectFleet();
        }
    }

    private void SelectFleet() {
        Presenter.IsFleetSelected = true;
    }


    public override void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            if (IsSelected) {
                Highlight(Highlights.SelectedAndFocus);
                return;
            }
            if (Presenter.IsFleetSelected) {
                Highlight(Highlights.FocusAndGeneral);
                return;
            }
            Highlight(Highlights.Focused);
            return;
        }
        if (IsSelected) {
            Highlight(Highlights.Selected);
            return;
        }
        if (Presenter.IsFleetSelected) {
            Highlight(Highlights.General);
            return;
        }
        Highlight(Highlights.None);
    }

    protected override void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    //private void ShowMesh(bool toShow) {
    //    if (toShow) {
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMeshColor_Main);
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalMeshColor_Specular);
    //        // TODO audio on goes here
    //    }
    //    else {
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenMeshColor);
    //        _renderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenMeshColor);
    //        // TODO audio off goes here
    //    }
    //    //ShowVelocityRay(toShow);
    //}

    /// <summary>
    /// Shows a Ray indicating the course and speed of the ship.
    /// </summary>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                Reference<float> shipSpeed = Presenter.GetShipSpeed();
                _velocityRay = new VelocityRay("ShipVelocity", _transform, shipSpeed, parent: DynamicObjects.Folder,
                    width: 1F, color: GameColor.Gray);
            }
            _velocityRay.Show(toShow);
        }
    }

    //private void InitializeMesh() {
    //    _renderer = gameObject.GetComponentInChildren<Renderer>();
    //    _originalMeshColor_Main = _renderer.material.GetColor(UnityConstants.MaterialColor_Main);
    //    _originalMeshColor_Specular = _renderer.material.GetColor(UnityConstants.MaterialColor_Specular);
    //    _hiddenMeshColor = GameColor.Clear.ToUnityColor();
    //}

    protected override void Cleanup() {
        base.Cleanup();
        if (_velocityRay != null) {
            _velocityRay.Dispose();
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region ICameraFocusable Members

    //protected override float CalcOptimalCameraViewingDistance() {
    //    return Radius * 2.4F;
    //}

    //#endregion

    //#region ICameraTargetable Members

    //public override bool IsEligible {
    //    get {
    //        return PlayerIntel.Source != IntelSource.None;
    //    }
    //}

    //protected override float CalcMinimumCameraViewingDistance() {
    //    return Radius * 2.0F;
    //}

    //#endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    #endregion

    //#region IShipViewable Members

    //public event Action onShowCompletion;

    //// these 3 must return onShowCompletion when finished to inform 
    //// ShipItem when it is OK to progress to the next state
    //public void ShowAttacking() {
    //    throw new NotImplementedException();
    //}

    //public void ShowHit() {
    //    throw new NotImplementedException();
    //}

    //public void ShowDying() {
    //    _showingJob = new Job(ShowingDying(), toStart: true);
    //}

    //private IEnumerator ShowingDying() {
    //    if (dying != null) {
    //        _audioSource.PlayOneShot(dying);
    //    }
    //    _collider.enabled = false;
    //    //animation.Stop();
    //    //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
    //    yield return null;

    //    var sc = onShowCompletion;
    //    if (sc != null) {
    //        sc();
    //    }
    //}

    //// these 3 run continuously until they are stopped via StopShowing() when
    //// ShipItem state changes from the state that started them
    //public void ShowEntrenching() {
    //    throw new NotImplementedException();
    //}

    //public void ShowRepairing() {
    //    throw new NotImplementedException();
    //}

    //public void ShowRefitting() {
    //    throw new NotImplementedException();
    //}

    //public void StopShowing() {
    //    if (_showingJob != null && _showingJob.IsRunning) {
    //        _showingJob.Kill();
    //    }
    //}

    //#endregion

}

