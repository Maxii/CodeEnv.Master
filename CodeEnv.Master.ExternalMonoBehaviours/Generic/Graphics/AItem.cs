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
public abstract class AItem : AMonoBehaviourBase, ICameraTargetable {

    /// <summary>
    /// Gets or sets the data for this item. Clients are responsible for setting in the right sequence as 
    /// one data can be dependant on another data.
    /// </summary>
    public Data Data { get; set; }

    private IntelLevel _humanPlayerIntelLevel = IntelLevel.Unknown;
    public virtual IntelLevel HumanPlayerIntelLevel {
        get {
            return _humanPlayerIntelLevel;
        }
        set {
            SetProperty<IntelLevel>(ref _humanPlayerIntelLevel, value, "HumanPlayerIntelLevel");
        }
    }

    /// <summary>
    /// Provides the ability to update the text for the GuiCursorHud. Can be null if there
    /// is no data for the GuiCursorHud to show for this item.
    /// </summary>
    protected GuiHudPublisher HudPublisher { get; set; }

    protected Collider _collider;
    protected Transform _transform;

    void Awake() {
        InitializeOnAwake();
    }

    protected virtual void InitializeOnAwake() {
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        _transform = transform;
        _collider = gameObject.GetComponent<Collider>();
    }

    void Start() {
        InitializeOnStart();
    }

    protected virtual void InitializeOnStart() {
        InitializeHudPublisher();
    }

    protected abstract void InitializeHudPublisher();

    protected virtual void OnHover(bool isOver) {
        if (HudPublisher != null) {
            if (isOver) {
                HudPublisher.DisplayHudAtCursor(HumanPlayerIntelLevel);
                //StartCoroutine<float>(HudPublisher.KeepHudCurrent, 2F);     // NO. Won't start. MethodName = "KeepHudCurrent", same as using separately declared Func<>
                //StartCoroutine("HudPublisher.KeepHudCurrent", 2F);  // NO. Won't start. MethodName = "HudPublisher.KeepHudCurrent"
                //StartCoroutine<float>(HudPublisher.KeepHudCurrent(), 2F);   //NO. Won't start. Declares and gets a HudPublisher delegate pointing to KeepHudCurrent(float) from HudPublisher. 
                StartCoroutine(HudPublisher.KeepHudCurrent(2F));    // THIS WORKS!
            }
            else {
                StopAllCoroutines();        // THIS WORKS
                HudPublisher.ClearHud();
            }
        }
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

}

