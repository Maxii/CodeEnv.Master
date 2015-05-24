// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetoidView.cs
// A class for managing the UI of a planetoid.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a planetoid.
/// </summary>
public class PlanetoidView : AMortalItemView, ICameraFollowable {

    public new PlanetoidPresenter Presenter {
        get { return base.Presenter as PlanetoidPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override void Awake() {
        base.Awake();
        _selectionMgr = SelectionManager.Instance;
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        InitializeContextMenu();
    }

    protected override IIntel InitializePlayerIntel() {
        return new ImprovingIntel();
    }

    protected override void InitializePresenter() {
        Presenter = new PlanetoidPresenter(this);
    }

    protected override void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as ImprovingIntel).SubscribeToPropertyChanged<ImprovingIntel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
    }

    #region ContextMenu

    private SelectionManager _selectionMgr;
    private CtxObject _ctxObject;

    void OnPress(bool isDown) {
        if (GameInputHelper.Instance.IsRightMouseButton() && !isDown) {
            OnRightPressRelease();
        }
    }

    private void OnRightPressRelease() {
        FleetCmdView selectedFleetView = _selectionMgr.CurrentSelection as FleetCmdView;
        if (selectedFleetView != null) {
            _ctxObject.ShowMenu();
        }
    }

    private void InitializeContextMenu() {    // IMPROVE string use
        _ctxObject = UnityUtility.ValidateMonoBehaviourPresence<CtxObject>(gameObject);
        CtxMenu planetMenu = GuiManager.Instance.gameObject.GetSafeMonoBehavioursInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "PlanetMenu");
        _ctxObject.contextMenu = planetMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        // TODO
    }

    private void OnContextMenuSelection() {
        int menuId = CtxObject.current.selectedItem;
        FleetCmdView_Player selectedFleetView = _selectionMgr.CurrentSelection as FleetCmdView_Player;
        IFleetCmdModel selectedFleet = selectedFleetView.Presenter.Model;
        var planetTarget = Presenter.Model as INavigableTarget;
        if (menuId == 0) {  // UNDONE
            // MoveTo
            selectedFleet.CurrentOrder = new FleetOrder(FleetDirective.Move, planetTarget, Speed.FleetStandard);
        }
        else if (menuId == 1) {
            // Attack
            selectedFleet.CurrentOrder = new FleetOrder(FleetDirective.Attack, planetTarget, Speed.FleetStandard);
        }
    }

    private void OnContextMenuHide() {
        // TODO
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 3.0F;
    public virtual float FollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public virtual float FollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

}

