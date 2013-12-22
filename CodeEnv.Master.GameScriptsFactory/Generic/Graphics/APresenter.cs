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
        GuiHudPublisher<StarBaseData>.SetFactory(StarBaseGuiHudTextFactory.Instance);
    }

    protected IViewable View { get; private set; }

    private AItem _item;
    public AItem Item {
        get { return _item; }
        protected set { SetProperty<AItem>(ref _item, value, "Item", OnItemChanged); }
    }

    private float _3dAnimationsTo3dDisplayModeTransitionDistanceInSectors;
    private float _3dTo2dDisplayModeTransitionDistanceInSectors;
    private float _2dToNoneDisplayModeTransitionDistanceInSectors;
    private ViewDisplayMode _cameraDistanceGeneratedDisplayMode;

    protected IList<IDisposable> _subscribers;

    protected GameObject _viewGameObject;

    public APresenter(IViewable view) {
        View = view;
        _viewGameObject = (view as Component).gameObject;
        Item = InitilizeItemLinkage();
        // the following use ItemData so Views should only be enabled to create this Presenter after ItemData is set
        View.HudPublisher = InitializeHudPublisher();
        InitializeDisplayModeTransitionDistances();
    }

    protected abstract AItem InitilizeItemLinkage();

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    private void InitializeDisplayModeTransitionDistances() {
        _2dToNoneDisplayModeTransitionDistanceInSectors = View.Radius * AnimationSettings.Instance.CameraDistanceThresholdFactor_2dToNoneDisplayMode;
        _3dTo2dDisplayModeTransitionDistanceInSectors = View.Radius * AnimationSettings.Instance.CameraDistanceThresholdFactor_3dTo2dDisplayMode;
        _3dAnimationsTo3dDisplayModeTransitionDistanceInSectors = View.Radius * AnimationSettings.Instance.CameraDistanceThresholdFactor_3dAnimationsTo3dDisplayMode;
    }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(CameraControl.Instance.SubscribeToPropertyChanged<CameraControl, Index3D>(cc => cc.SectorIndex, OnCameraSectorIndexChanged));
    }

    private void OnCameraSectorIndexChanged() {
        Index3D cameraSector = CameraControl.Instance.SectorIndex;
        Index3D viewSector = SectorGrid.GetSectorIndex(Item.Data.Position);
        float distanceInSectorsToCamera = SectorGrid.GetDistanceInSectors(viewSector, cameraSector);
        ViewDisplayMode desiredDisplayMode;
        //D.Log("CameraDistances {0}, {1}, {2}, {3}.", distanceToCamera, _show2DIconDistance, _show3DMeshDistance, _showAnimationsDistance);
        if (distanceInSectorsToCamera > _2dToNoneDisplayModeTransitionDistanceInSectors) {
            desiredDisplayMode = ViewDisplayMode.Hide;
        }
        else if (distanceInSectorsToCamera > _3dTo2dDisplayModeTransitionDistanceInSectors) {
            desiredDisplayMode = ViewDisplayMode.TwoD;
        }
        else if (distanceInSectorsToCamera > _3dAnimationsTo3dDisplayModeTransitionDistanceInSectors) {
            desiredDisplayMode = ViewDisplayMode.ThreeD;
        }
        else {
            desiredDisplayMode = ViewDisplayMode.ThreeDAnimation;
        }

        if (desiredDisplayMode != _cameraDistanceGeneratedDisplayMode) {
            View.RecordDesiredDisplayModeDerivedFromCameraDistance(desiredDisplayMode);
            _cameraDistanceGeneratedDisplayMode = desiredDisplayMode;
        }
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


