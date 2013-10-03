// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetManager.cs
// The manager of a Fleet whos primary purpose is to handle fleet-wide administrative business. FleetCommand
// handles fleet order execution.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The manager of a Fleet whos primary purpose is to handle fleet-wide administrative business. FleetCommand
/// handles fleet order execution. FleetGraphics handles most graphics operations, 
/// and each individual fleet object (command or ship) handles camera interaction and showing Hud info.
/// </summary>
public class FleetManager : AMonoBehaviourBase, ISelectable, IDisposable {

    private static IconFactory _iconFactory;

    /// <summary>
    /// The separation between the pivot point on the lead ship and the fleet icon,
    ///as a Viewport vector. Viewport vector values vary from 0.0F to 1.0F.
    /// </summary>
    public Vector3 _fleetIconOffsetFromPivot = new Vector3(Constants.ZeroF, 0.03F, Constants.ZeroF);

    /// <summary>
    /// Used for convenience only. Actual FleetData repository is held by FleetCommand.
    /// </summary>
    public FleetData Data {
        get { return _fleetCmd.Data; }
        set { _fleetCmd.Data = value; }
    }

    private IntelLevel _playerIntelLevel;
    public IntelLevel PlayerIntelLevel {
        get { return _playerIntelLevel; }
        set { SetProperty<IntelLevel>(ref _playerIntelLevel, value, "PlayerIntelLevel", OnIntelLevelChanged); }
    }

    public IList<ShipCaptain> ShipCaptains { get; private set; }

    private ShipCaptain _leadShipCaptain;
    public ShipCaptain LeadShipCaptain {
        get { return _leadShipCaptain; }
        set { SetProperty<ShipCaptain>(ref _leadShipCaptain, value, "LeadShipCaptain", OnLeadShipChanged); }
    }

    private SelectionManager _selectionMgr;
    private GameManager _gameMgr;
    private GameEventManager _eventMgr;
    private FleetGraphics _fleetGraphics;
    private FleetCommand _fleetCmd;
    private UISprite _fleetIconSprite;

    private IList<IDisposable> _subscribers;

    // cached transforms
    private Transform _fleetCmdTransform;
    public Transform _leadShipTransform;

    /// <summary>
    /// The offset that determines the point on the lead ship from which
    ///  the Fleet Icon pivots, as a Worldspace vector.
    /// </summary>
    private Vector3 _fleetIconPivotOffset;
    private IList<IIcon> _fleetIcons;

    protected override void Awake() {
        base.Awake();
        _fleetCmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>();
        _fleetCmdTransform = _fleetCmd.transform;
        _fleetGraphics = gameObject.GetSafeMonoBehaviourComponent<FleetGraphics>();
        _fleetIconSprite = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        ShipCaptains = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>().ToList();
        _gameMgr = GameManager.Instance;
        _eventMgr = GameEventManager.Instance;
        _selectionMgr = SelectionManager.Instance;
        InitializeFleetIcon();
        Subscribe();
    }

    private void InitializeFleetIcon() {
        _fleetIcons = new List<IIcon>(1);
        _iconFactory = IconFactory.Instance;    // TODO How to handle which of these icons go to which sprites?
        _fleetIcons.Add(_iconFactory.MakeInstance<FleetIcon>(IconSection.Bottom, IconSelectionCriteria.None));
        //_fleetIcons.Add(_iconFactory.MakeInstance<FleetIcon>(IconSection.Top, IconSelectionCriteria.None));
        _fleetIconSprite.spriteName = _fleetIcons[0].Filename;  // UNDONE
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.GameState, OnGameStateChanged));
        _subscribers.Add(_fleetCmd.SubscribeToPropertyChanged<FleetCommand, FleetData>(fc => fc.Data, OnFleetDataChanged));
    }

    private void OnGameStateChanged() {
        if (_gameMgr.GameState == GameState.Running) {
            // IMPROVE select LeadShipCaptain here for now as Data must be initialized first
            LeadShipCaptain = RandomExtended<ShipCaptain>.Choice(ShipCaptains);
            _fleetCmd.__GetFleetUnderway();
        }
    }

    private void OnFleetDataChanged() {
        _subscribers.Add(_fleetCmd.Data.SubscribeToPropertyChanged<FleetData, FleetComposition>(fd => fd.Composition, OnFleetCompositionChanged));
    }

    private void OnFleetCompositionChanged() {
        TryChangeFleetIcon();
    }

    void Update() {
        if (ToUpdate()) {
            KeepCommandOverLeadShip();
        }
    }

    private void KeepCommandOverLeadShip() {  // OPTIMIZE?
        Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_leadShipTransform.position + _fleetIconPivotOffset);
        _fleetCmdTransform.position = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconOffsetFromPivot);
        _fleetCmdTransform.rotation = _leadShipTransform.rotation;
    }

    private void OnLeadShipChanged() {
        _leadShipTransform = LeadShipCaptain.transform;
        _fleetCmd.Data.LeadShipData = LeadShipCaptain.Data;
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, _leadShipTransform.collider.bounds.extents.y, Constants.ZeroF);
    }

    private void OnIntelLevelChanged() {
        _fleetCmd.PlayerIntelLevel = PlayerIntelLevel;
        ShipCaptains.ForAll<ShipCaptain>(cap => cap.PlayerIntelLevel = PlayerIntelLevel);
        TryChangeFleetIcon();
    }

    private void TryChangeFleetIcon() {
        // TODO evaluate IntelLevel and Composition
        // __ChangeFleetIcon();
    }

    private void __ChangeFleetIcon() {
        IIcon newIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Top, IconSelectionCriteria.Science);
        if (_fleetIcons.Contains(newIcon)) {
            return;
        }
        D.Log("Changing {0} {1}.", typeof(FleetIcon).Name, IconSection.Top.GetName());
        _fleetIcons[0] = newIcon;
        _fleetIconSprite.spriteName = newIcon.Filename;
        // TODO swap out new icon for the old icon with the same Section value. Will order matter?
    }

    private void OnIsSelectedChanged() {
        _fleetGraphics.AssessHighlighting();
        ShipCaptains.ForAll<ShipCaptain>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<ShipGraphics>().AssessHighlighting());
        if (IsSelected) {
            _selectionMgr.CurrentSelection = this;
        }
    }

    public void ProcessShipRemoval(ShipCaptain shipCaptain) {
        RemoveShip(shipCaptain);
        if (ShipCaptains.Count > Constants.Zero) {
            if (shipCaptain == LeadShipCaptain) {
                // LeadShip was destroyed
                LeadShipCaptain = SelectBestShip();
            }
            _fleetCmd.DeclareAsFocus();
            return;
        }
        // FleetCommand knows when to die
    }

    private void RemoveShip(ShipCaptain shipCaptain) {
        bool isRemoved = ShipCaptains.Remove(shipCaptain);
        isRemoved = isRemoved && _fleetCmd.Data.RemoveShip(shipCaptain.Data);
        D.Assert(isRemoved, "{0} not found.".Inject(shipCaptain.Data.Name));
    }

    public void Die() {
        if (IsSelected) {
            _selectionMgr.CurrentSelection = null;
        }
        Destroy(gameObject);
    }

    private ShipCaptain SelectBestShip() {
        return ShipCaptains.MaxBy(sc => sc.Data.Health);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", OnIsSelectedChanged); }
    }

    public void OnLeftClick() {
        IsSelected = true;
    }

    #endregion

    #region IHasData Members

    public Data GetData() {
        return Data;
    }

    #endregion

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

