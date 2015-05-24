// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdView_AI.cs
// A class for managing the UI of a StarbaseCmd owned by the AI.    
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// A class for managing the UI of a StarbaseCmd owned by the AI.    
/// </summary>
public class StarbaseCmdView_AI : StarbaseCmdView {

    public new StarbaseCmdPresenter_AI Presenter {
        get { return base.Presenter as StarbaseCmdPresenter_AI; }
        protected set { base.Presenter = value; }
    }

    protected override void Start() {
        base.Start();
        __InitializeContextMenu();
    }

    protected override void InitializePresenter() {
        Presenter = new StarbaseCmdPresenter_AI(this);
    }

    #region ContextMenu

    private CtxObject _ctxObject;

    private void __InitializeContextMenu() {      // IMPROVE use of string
        _ctxObject = gameObject.GetSafeMonoBehaviour<CtxObject>();
        _ctxObject.offsetMenu = true;

        CtxMenu generalMenu = GuiManager.Instance.gameObject.GetSafeMonoBehavioursInChildren<CtxMenu>().Single(menu => menu.gameObject.name == "GeneralMenu");
        _ctxObject.contextMenu = generalMenu;
        D.Assert(_ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, Presenter.FullName));

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

    #region Mouse Events

    protected override void OnRightPress(bool isDown) {
        base.OnRightPress(isDown);
        if (IsSelected) {
            Presenter.RequestContextMenu(isDown);
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

