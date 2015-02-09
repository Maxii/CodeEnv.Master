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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The manager of a Fleet whos primary purpose is to handle fleet-wide administrative business. FleetCommand
/// handles fleet order execution. FleetGraphics handles most graphics operations, 
/// and each individual fleet object (command or ship) handles camera interaction and showing Hud info.
/// </summary>
[System.Obsolete]
public class FleetManager : AMonoBase, ISelectable, IHasData, IDisposable {

    private static AIconFactory _iconFactory;

    /// <summary>
    /// Used for convenience only. Actual FleetData repository is held by FleetCommand.
    /// </summary>
    public FleetCmdItemData Data {
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
    private Transform _fleetCmdTransform;
    private Transform _leadShipTransform;

    private Transform _fleetIconTransform;
    private Vector3 _fleetIconPivotOffset;
    private BoxCollider _fleetCmdCollider;
    private UISprite _fleetIconSprite;
    private ScaleRelativeToCamera _fleetIconScaler;
    private IIcon _fleetIcon;
    private Vector3 _iconSize;

    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        enabled = false;    // disabled behaviours aren't updated
        _fleetCmd = gameObject.GetSafeMonoBehaviourComponentInChildren<FleetCommand>();
        _fleetCmdTransform = _fleetCmd.transform;
        _fleetGraphics = gameObject.GetSafeMonoBehaviourComponent<FleetGraphics>();
        ShipCaptains = gameObject.GetSafeMonoBehaviourComponentsInChildren<ShipCaptain>().ToList();
        _gameMgr = GameManager.Instance;
        _eventMgr = GameEventManager.Instance;
        _selectionMgr = SelectionManager.Instance;
        InitializeFleetIcon();
        Subscribe();
    }

    private void InitializeFleetIcon() {
        _fleetIconSprite = gameObject.GetSafeMonoBehaviourComponentInChildren<UISprite>();
        _fleetIconTransform = _fleetIconSprite.transform;
        _fleetIconScaler = _fleetIconTransform.gameObject.GetSafeMonoBehaviourComponent<ScaleRelativeToCamera>();
        _fleetCmdCollider = _fleetCmd.collider as BoxCollider;
        // I need the CmdCollider sitting over the fleet icon to be 3D as it's rotation tracks the Cmd object, not the billboarded icon
        Vector2 iconSize = _fleetIconSprite.localSize;
        _iconSize = new Vector3(iconSize.x, iconSize.y, iconSize.x);

        _iconFactory = AIconFactory.Instance;
        UpdateRate = FrameUpdateFrequency.Normal;
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameMgr.SubscribeToPropertyChanged<GameManager, GameState>(gm => gm.GameState, OnGameStateChanged));
        _subscribers.Add(_fleetCmd.SubscribeToPropertyChanged<FleetCommand, FleetCmdItemData>(fc => fc.Data, OnFleetDataChanged));
    }

    private void OnGameStateChanged() {
        if (_gameMgr.GameState == GameState.Running) {
            // IMPROVE select LeadShipCaptain here for now as Data must be initialized first
            LeadShipCaptain = RandomExtended<ShipCaptain>.Choice(ShipCaptains);
            enabled = true; // OK to update now
            _fleetCmd.__GetFleetUnderway();
        }
    }

    private void OnFleetDataChanged() {
        _subscribers.Add(_fleetCmd.Data.SubscribeToPropertyChanged<FleetCmdItemData, FleetComposition>(fd => fd.Composition, OnFleetCompositionChanged));
    }

    private void OnFleetCompositionChanged() {
        AssessFleetIcon();
    }

    void Update() {
        KeepCommandOverLeadShip();
        if (ToUpdate()) {
            KeepCommandColliderCurrent();
        }
    }

    private void KeepCommandOverLeadShip() {
        _fleetCmdTransform.position = _leadShipTransform.position;
        _fleetCmdTransform.rotation = _leadShipTransform.rotation;

        // Notes: _fleetIconPivotOffset is a worldspace offset to the top of the leadship collider and doesn't change with scale, position or rotation
        // The approach below will also work if we want a viewport offset that is a constant percentage of the viewport
        //Vector3 viewportOffsetLocation = Camera.main.WorldToViewportPoint(_leadShipTransform.position + _fleetIconPivotOffset);
        //Vector3 worldOffsetLocation = Camera.main.ViewportToWorldPoint(viewportOffsetLocation + _fleetIconViewportOffset);
        //_fleetIconTransform.localPosition = worldOffsetLocation - _leadShipTransform.position;
        _fleetIconTransform.localPosition = _fleetIconPivotOffset;
    }

    private void KeepCommandColliderCurrent() {
        _fleetCmdCollider.size = Vector3.Scale(_iconSize, _fleetIconScaler.Scale);

        Vector3[] iconWorldCorners = _fleetIconSprite.worldCorners;
        Vector3 iconWorldCenter = iconWorldCorners[0] + (iconWorldCorners[2] - iconWorldCorners[0]) * 0.5F;
        // convert icon's world position to the equivalent local position on the fleetCmd transform
        _fleetCmdCollider.center = _fleetCmdTransform.InverseTransformPoint(iconWorldCenter);
    }

    private void OnLeadShipChanged() {
        //AssignFleetCmdToLeadShip();
        _leadShipTransform = LeadShipCaptain.transform;
        _fleetIconPivotOffset = new Vector3(Constants.ZeroF, _leadShipTransform.collider.bounds.extents.y, Constants.ZeroF);
        Data.FlagshipData = LeadShipCaptain.Data;
    }

    [Obsolete]  // Ship rigidbody consumes FleetCmd collider ray hits from camera
    private void AssignFleetCmdToLeadShip() {
        UnityUtility.AttachChildToParent(_fleetCmd.gameObject, _leadShipCaptain.gameObject);
        Data.FlagshipData = LeadShipCaptain.Data;
    }

    private void OnIntelLevelChanged() {
        _fleetCmd.PlayerIntelLevel = PlayerIntelLevel;
        ShipCaptains.ForAll<ShipCaptain>(cap => cap.PlayerIntelLevel = PlayerIntelLevel);
        AssessFleetIcon();
    }

    private void AssessFleetIcon() {
        // TODO evaluate Composition
        switch (PlayerIntelLevel) {
            case IntelLevel.Nil:
                _fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.None);
                _fleetIconSprite.color = GameColor.Clear.ToUnityColor();    // TODO None should be a completely transparent icon
                break;
            case IntelLevel.Unknown:
                _fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Unknown);
                _fleetIconSprite.color = GameColor.White.ToUnityColor();    // may be clear from prior setting
                break;
            case IntelLevel.OutOfDate:
                _fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Unknown);
                _fleetIconSprite.color = Data.Owner.Color.ToUnityColor();
                break;
            case IntelLevel.LongRangeSensors:
                _fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, IconSelectionCriteria.Level5);
                _fleetIconSprite.color = Data.Owner.Color.ToUnityColor();
                break;
            case IntelLevel.ShortRangeSensors:
            case IntelLevel.Complete:
                var selectionCriteria = new IconSelectionCriteria[] { IconSelectionCriteria.Level5, IconSelectionCriteria.Science, IconSelectionCriteria.Colony, IconSelectionCriteria.Troop };
                _fleetIcon = _iconFactory.MakeInstance<FleetIcon>(IconSection.Base, selectionCriteria);
                _fleetIconSprite.color = Data.Owner.Color.ToUnityColor();
                break;
            case IntelLevel.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(PlayerIntelLevel));
        }
        _fleetIconSprite.spriteName = _fleetIcon.Filename;
        D.Log("IntelLevel is {2}, changing {0} to {1}.", typeof(FleetIcon).Name, _fleetIcon.Filename, PlayerIntelLevel.GetName());
    }

    private void OnIsSelectedChanged() {
        _fleetGraphics.AssessHighlighting();
        ShipCaptains.ForAll<ShipCaptain>(sc => sc.gameObject.GetSafeMonoBehaviourComponent<ShipGraphics>().AssessHighlighting());
        //D.Log("ShipCaptains count is {0}.", ShipCaptains.Count);
        if (IsSelected) {
            _selectionMgr.CurrentSelection = this;
        }
    }

    public void ProcessShipRemoval(ShipCaptain shipCaptain) {
        RemoveShip(shipCaptain);
        if (ShipCaptains.Count > Constants.Zero) {
            if (shipCaptain == LeadShipCaptain) {
                // LeadShip has died
                LeadShipCaptain = SelectBestShip();
            }
            if (_fleetGraphics.IsDetectable) {
                _fleetCmd.IsFocus = true;
            }
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
        if (_fleetGraphics.IsDetectable) {
            KeyCode notUsed;
            if (GameInputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                ShipCaptains.ForAll<ShipCaptain>(s => s.__SimulateAttacked());
                return;
            }
            IsSelected = true;
        }
    }

    #endregion

    #region IHasData Members

    public AMortalItemData GetData() {
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

