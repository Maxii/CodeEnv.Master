// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItem.cs
// Abstract base class for any item in the universe that is ICameraTargetable and
//supports viewing data via the GuiCursorHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for any item in the universe that is ICameraTargetable and
/// supports viewing data via the GuiCursorHud.
/// </summary>
public abstract class AItem : AMonoBehaviourBase, ICameraTargetable, IHasData, IDisposable {

    private Data _data;
    /// <summary>
    /// Gets or sets the data for this item. Clients are responsible for setting in the right sequence as 
    /// one data can be dependant on another data.
    /// </summary>
    public Data Data {
        get { return _data; }
        set { SetProperty<Data>(ref _data, value, "Data", OnDataChanged); }
    }

    private IntelLevel _playerIntelLevel = IntelLevel.Unknown;
    public virtual IntelLevel PlayerIntelLevel {
        get {
            return _playerIntelLevel;
        }
        set {
            SetProperty<IntelLevel>(ref _playerIntelLevel, value, "PlayerIntelLevel", OnPlayerIntelLevelChanged);
        }
    }

    private float _size;
    /// <summary>
    /// The [float] size of this object's collider measured as the distance from the 
    /// min extent to the max extent. As bounds is a bounding box it is the longest 
    /// diagonal between corners of the box.
    /// </summary>
    public float Size {
        get {
            if (_size == Constants.ZeroF) {
                _size = collider.bounds.extents.magnitude * 2F; // Avoid cached _collider as Awake may not have run yet
            }
            return _size;
        }
    }

    /// <summary>
    /// Provides the ability to update the text for the GuiCursorHud. Can be null if there
    /// is no data for the GuiCursorHud to show for this item.
    /// </summary>
    protected IGuiHudPublisher HudPublisher { get; private set; }

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected virtual void OnDataChanged() {
        HudPublisher = InitializeHudPublisher();
    }

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    protected virtual void OnHover(bool isOver) {
        DisplayHud(isOver);
    }

    private void DisplayHud(bool toDisplay) {
        if (HudPublisher != null) {
            if (toDisplay) {
                StartCoroutine(HudPublisher.DisplayHudAtCursor(PlayerIntelLevel));
            }
            else {
                HudPublisher.ClearHud();
            }
        }
    }

    protected virtual void OnPlayerIntelLevelChanged() {
        if (HudPublisher != null && HudPublisher.IsHudShowing) {
            // it is currently showing so reinitialize it with new settings
            HudPublisher.ClearHud();
            DisplayHud(true);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (HudPublisher != null && HudPublisher.IsHudShowing) {
            HudPublisher.ClearHud();
        }
        Dispose();
    }

    #region ICameraTargetable Members

    public virtual bool IsTargetable {
        get { return true; }
    }

    [SerializeField]
    protected float minimumCameraViewingDistanceMultiplier = 2.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = CalcMinimumCameraViewingDistance();
            }
            return _minimumCameraViewingDistance;
        }
    }

    /// <summary>
    /// One time calculation of the minimum camera viewing distance.
    /// </summary>
    /// <returns></returns>
    protected virtual float CalcMinimumCameraViewingDistance() {
        return Size * minimumCameraViewingDistanceMultiplier;
    }

    #endregion

    // GuiHudPublisher has subscribers that need to be disposed
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
            (HudPublisher as IDisposable).Dispose();
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


    #region IHasData Members

    public Data GetData() {
        return Data;
    }

    #endregion
}

