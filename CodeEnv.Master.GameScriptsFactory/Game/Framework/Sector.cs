// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sector.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class Sector : AMonoBase, IItem, INavigableTarget {

    public event Action<IItem> onOwnerChanged;

    private SectorData _data;
    public SectorData Data {
        get { return _data; }
        set {
            if (_data != null) { throw new MethodAccessException("{0}.{1}.Data can only be set once.".Inject(FullName, GetType().Name)); }
            _data = value;
            _data.Transform = _transform;
            SubscribeToDataValueChanges();
        }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    /// <summary>
    /// The name to use for display in the UI.
    /// </summary>
    public string DisplayName { get { return Name; } }

    public string FullName {
        get {
            if (Data != null) {
                return Data.FullName;
            }
            return _transform.name + "(from transform)";
        }
    }

    /// <summary>
    /// The name of this individual Item.
    /// </summary>
    public string Name { get { return Data.Name; } }

    public Vector3 Position { get { return Data.Position; } }

    private float _radius;
    /// <summary>
    /// The radius of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public float Radius {
        get {
            D.Assert(_radius != Constants.ZeroF, "{0}.Radius has not yet been set.".Inject(FullName));
            return _radius;
        }
        protected set { _radius = value; }
    }

    public Player Owner { get { return Data.Owner; } }

    public Transform Transform { get { return _transform; } }

    private SectorPublisher _publisher;
    public SectorPublisher Publisher {
        get { return _publisher = _publisher ?? new SectorPublisher(Data); }
    }

    public bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    private SectorHudManager _hudManager;
    private IGameManager _gameMgr;
    private List<IDisposable> _subscribers;

    #region Initialization

    protected override void Awake() {
        base.Awake();
        InitializeLocalReferencesAndValues();
        Subscribe();
        enabled = false;
    }

    /// <summary>
    /// Called from Awake, initializes local references and values including Radius-related components.
    /// </summary>
    private void InitializeLocalReferencesAndValues() {
        _gameMgr = References.GameManager;
        Radius = TempGameValues.SectorSideLength / 2F;  // the radius of the sphere inscribed inside a sector box
        // there is no collider associated with a SectorItem. The collider used for context menu activation is part of the SectorExaminer
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    protected override void Start() {
        base.Start();
        InitializeModelMembers();
        InitializeViewMembers();
    }

    /// <summary>
    /// Called from Start, initializes Model-related members of this Item.
    /// </summary>
    private void InitializeModelMembers() { }

    /// <summary>
    /// Called from Start, initializes View-related members of this item 
    /// that can't wait until the Item first becomes discernible. Default
    /// implementation does nothing.
    /// </summary>
    private void InitializeViewMembers() {
        InitializeHudManager();
    }

    private void InitializeHudManager() {
        _hudManager = new SectorHudManager(Publisher);
    }

    /// <summary>
    /// Subscribes to changes to values contained in Data. 
    /// </summary>
    private void SubscribeToDataValueChanges() {
        D.Assert(_subscribers != null);
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.Name, OnNamingChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.ParentName, OnNamingChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OnOwnerChanging));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OnOwnerChanged));
    }

    #endregion


    #region Model Methods

    public SectorReport GetReport(Player player) { return Publisher.GetReport(player); }

    private void OnOwnerChanging(Player newOwner) { }

    private void OnOwnerChanged() {
        if (onOwnerChanged != null) {
            onOwnerChanged(this);
        }
    }

    /// <summary>
    /// Called when either the Item name or parentName is changed.
    /// </summary>
    private void OnNamingChanged() { }

    #endregion

    #region View Methods

    public void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                _hudManager.Show(Position);
            }
            else {
                _hudManager.Hide();
            }
        }
    }

    #endregion

    #region Cleanup

    protected sealed override void OnDestroy() {
        base.OnDestroy();
    }

    /// <summary>
    /// Cleans up this instance.
    /// Note: all members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    protected override void Cleanup() {
        Unsubscribe();
        if (_hudManager != null) {
            _hudManager.Dispose();
        }
    }

    private void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public Topography Topography { get { return Data.Topography; } }

    public bool IsMobile { get { return false; } }

    #endregion

}

