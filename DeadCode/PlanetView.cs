// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetView.cs
// A class for managing the UI of a planet. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a planet. 
/// </summary>
public class PlanetView : APlanetoidView {

    public new PlanetPresenter Presenter {
        get { return base.Presenter as PlanetPresenter; }
        protected set { base.Presenter = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Start() {
        base.Start();
        InitializeContextMenu();
    }

    protected override void InitializePresenter() {
        Presenter = new PlanetPresenter(this);
    }

    #region ContextMenu

    private CtxObject _ctxObject;

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (!isDown) {
            // right press release
            FleetCmdView selectedFleetView = SelectionManager.Instance.CurrentSelection as FleetCmdView;
            if (selectedFleetView != null) {
                _ctxObject.ShowMenu();
            }
        }
    }

    private void InitializeContextMenu() {    // IMPROVE string use
        _ctxObject = UnityUtility.ValidateMonoBehaviourPresence<CtxObject>(gameObject);
        CtxMenu planetoidMenu = GuiManager.Instance.gameObject.GetSafeMonoBehavioursInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "PlanetoidMenu");
        _ctxObject.contextMenu = planetoidMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);

        EventDelegate.Add(_ctxObject.onShow, OnContextMenuShow);
        EventDelegate.Add(_ctxObject.onSelection, OnContextMenuSelection);
        EventDelegate.Add(_ctxObject.onHide, OnContextMenuHide);
    }

    private void OnContextMenuShow() {
        //TODO
    }

    private void OnContextMenuSelection() {
        int menuId = CtxObject.current.selectedItem;
        FleetCmdView_Player selectedFleetView = SelectionManager.Instance.CurrentSelection as FleetCmdView_Player;
        IFleetCmdModel selectedFleet = selectedFleetView.Presenter.Model;
        var thisPlanetoidTarget = Presenter.Model as INavigableTarget;
        if (menuId == 0) {  // UNDONE
            // MoveTo
            selectedFleet.CurrentOrder = new FleetOrder(FleetDirective.Move, thisPlanetoidTarget, Speed.FleetStandard);
        }
        else if (menuId == 1) {
            // Attack
            selectedFleet.CurrentOrder = new FleetOrder(FleetDirective.Attack, thisPlanetoidTarget, Speed.FleetStandard);
        }
    }

    private void OnContextMenuHide() {
        //TODO
    }


    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}

