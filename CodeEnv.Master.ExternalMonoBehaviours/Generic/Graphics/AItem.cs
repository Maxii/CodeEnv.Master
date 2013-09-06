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
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Abstract base class for any item in the universe that is ICameraTargetable and
/// supports viewing data via the GuiCursorHud.
/// </summary>
public abstract class AItem : AMonoBehaviourBase, ICameraTargetable, IDisposable {

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

    /// <summary>
    /// Provides the ability to update the text for the GuiCursorHud. Can be null if there
    /// is no data for the GuiCursorHud to show for this item.
    /// </summary>
    protected IGuiHudPublisher HudPublisher { get; private set; }

    protected Collider _collider;
    protected Transform _transform;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        _transform = transform;
        _collider = gameObject.GetComponent<Collider>();
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
                HudPublisher.DisplayHudAtCursor(PlayerIntelLevel);
                //StartCoroutine<float>(HudPublisher.KeepHudCurrent, 2F);     // NO. Won't start. MethodName = "KeepHudCurrent", same as using separately declared Func<>
                //StartCoroutine("HudPublisher.KeepHudCurrent", 2F);  // NO. Won't start. MethodName = "HudPublisher.KeepHudCurrent"
                //StartCoroutine<float>(HudPublisher.KeepHudCurrent(), 2F);   //NO. Won't start. Declares and gets a HudPublisher delegate pointing to KeepHudCurrent(float) from HudPublisher. 
                //StartCoroutine(HudPublisher.KeepHudCurrent(2F));    // THIS WORKS!
                StartCoroutine(HudPublisher.KeepHudCurrent());  // THIS WORKS!
            }
            else {
                StopAllCoroutines();
                HudPublisher.ClearHud();
            }
        }
    }

    protected virtual void OnPlayerIntelLevelChanged() {
        if (HudPublisher != null && HudPublisher.IsHudShowing) {
            // it is currently showing so reinitialize it with new settings
            DisplayHud(false);
            DisplayHud(true);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (HudPublisher.IsHudShowing) {
            DisplayHud(false);
        }
        Dispose();
    }

    #region ICameraTargetable Members

    public virtual bool IsTargetable {
        get { return true; }
    }

    [SerializeField]
    private float minimumCameraViewingDistanceMultiplier = 4.0F;

    private float _minimumCameraViewingDistance;
    public virtual float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = _collider.bounds.extents.magnitude * minimumCameraViewingDistanceMultiplier;
            }
            return _minimumCameraViewingDistance;
        }
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

}

