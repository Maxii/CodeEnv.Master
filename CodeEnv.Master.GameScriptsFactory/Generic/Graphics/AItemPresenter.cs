// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemPresenter.cs
// MVPresenter base class associated with an AView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// MVPresenter base class associated with an AView.
/// </summary>
public abstract class AItemPresenter : APropertyChangeTracking, IDisposable {

    static AItemPresenter() {
        InitializeHudPublishers();
    }

    private static void InitializeHudPublishers() {
        AGuiHudPublisher.SetGuiCursorHud(GuiCursorHud.Instance);
        GuiHudPublisher<ItemData>.SetFactory(GuiHudTextFactory.Instance);
        GuiHudPublisher<SectorData>.SetFactory(SectorGuiHudTextFactory.Instance);
        GuiHudPublisher<ShipData>.SetFactory(ShipGuiHudTextFactory.Instance);
        GuiHudPublisher<FleetData>.SetFactory(FleetGuiHudTextFactory.Instance);
        GuiHudPublisher<SystemData>.SetFactory(SystemGuiHudTextFactory.Instance);
        GuiHudPublisher<StarData>.SetFactory(StarGuiHudTextFactory.Instance);
        GuiHudPublisher<PlanetoidData>.SetFactory(PlanetoidGuiHudTextFactory.Instance);
        GuiHudPublisher<SettlementData>.SetFactory(SettlementGuiHudTextFactory.Instance);
        GuiHudPublisher<FacilityData>.SetFactory(FacilityGuiHudTextFactory.Instance);
        GuiHudPublisher<StarbaseData>.SetFactory(StarbaseGuiHudTextFactory.Instance);
    }

    protected IViewable View { get; private set; }

    private AItemModel _model;
    public AItemModel Model {
        get { return _model; }
        protected set {
            _model = value;
            _model.Radius = View.Radius;
        }
    }

    protected IList<IDisposable> _subscribers;

    protected GameObject _viewGameObject;

    public AItemPresenter(IViewable view) {
        View = view;
        _viewGameObject = (view as Component).gameObject;
        Model = AcquireModelReference();
        // the following use ItemData so Views should only be enabled to create this Presenter after ItemData is set
        View.HudPublisher = InitializeHudPublisher();
        // derived Presenters should call Subscribe() if they have any subscriptions to make
    }

    protected abstract AItemModel AcquireModelReference();

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    protected virtual void Subscribe() {    // TODO eventually move this Subscription mechanism up the chain only where it is needed
        _subscribers = new List<IDisposable>();
    }

    protected virtual void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    protected virtual void Unsubscribe() {
        if (_subscribers != null) { // needed for now so Presenters that don't need to subscribe don't have too
            _subscribers.ForAll(s => s.Dispose());
            _subscribers.Clear();
        }
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}


