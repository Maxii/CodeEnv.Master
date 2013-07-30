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

// default namespace

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
    protected HudPublisher HudPublisher { get; private set; }

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

    private void InitializeHudPublisher() {
        if (Data != null) {
            HudPublisher = new HudPublisher(Data);
        }
    }

    protected virtual void OnHover(bool isOver) {
        if (HudPublisher != null) {
            if (isOver) {
                DisplayCursorHud();
            }
            else {
                ClearCursorHud();
            }
        }
    }

    public abstract void DisplayCursorHud();

    public abstract void ClearCursorHud();

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

