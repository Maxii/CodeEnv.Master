// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APresenter.cs
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
public abstract class APresenter : APropertyChangeTracking, IDisposable {

    static APresenter() {
        InitializeHudPublishers();
    }

    private static void InitializeHudPublishers() {
        AGuiHudPublisher.SetGuiCursorHud(GuiCursorHud.Instance);
        GuiHudPublisher<Data>.SetFactory(GuiHudTextFactory.Instance);
        GuiHudPublisher<SectorData>.SetFactory(SectorGuiHudTextFactory.Instance);
        GuiHudPublisher<ShipData>.SetFactory(ShipGuiHudTextFactory.Instance);
        GuiHudPublisher<FleetData>.SetFactory(FleetGuiHudTextFactory.Instance);
        GuiHudPublisher<SystemData>.SetFactory(SystemGuiHudTextFactory.Instance);
        GuiHudPublisher<StarData>.SetFactory(StarGuiHudTextFactory.Instance);
        GuiHudPublisher<PlanetoidData>.SetFactory(PlanetoidGuiHudTextFactory.Instance);
        GuiHudPublisher<SettlementData>.SetFactory(SettlementGuiHudTextFactory.Instance);
        //GuiHudPublisher<StarBaseData>.SetFactory(StarBaseGuiHudTextFactory.Instance);
        GuiHudPublisher<FacilityData>.SetFactory(FacilityGuiHudTextFactory.Instance);
        GuiHudPublisher<StarbaseData>.SetFactory(StarbaseGuiHudTextFactory.Instance);
    }

    protected IViewable View { get; private set; }

    private AItem _item;
    public AItem Item {
        get { return _item; }
        protected set { SetProperty<AItem>(ref _item, value, "Item", OnItemChanged); }
    }

    protected IList<IDisposable> _subscribers;

    protected GameObject _viewGameObject;

    public APresenter(IViewable view) {
        View = view;
        _viewGameObject = (view as Component).gameObject;
        Item = AcquireItemReference();
        // the following use ItemData so Views should only be enabled to create this Presenter after ItemData is set
        View.HudPublisher = InitializeHudPublisher();
    }

    protected abstract AItem AcquireItemReference();

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        //_subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
    }

    private void OnItemChanged() {
        Item.Radius = View.Radius;
    }

    protected virtual void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    protected virtual void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
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


