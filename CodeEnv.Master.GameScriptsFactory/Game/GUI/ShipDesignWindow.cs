// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesignWindow.cs
// GuiWindow for the Ship Design Screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiWindow for the Ship Design Screen.
/// </summary>
public class ShipDesignWindow : AGuiWindow {

    private const string DesignerUITitleRoot = "Designer";

    private const string DesignIconExtension = " DesignIcon";

    private const string EquipmentIconExtension = " EquipmentIcon";

    /// <summary>
    /// The position in 3DStageCamera's Viewport space where the hull should be located.
    /// </summary>
    private static Vector3 _3DCameraViewportPosition_TinyHull = new Vector3(0.75f, 0.30F, 0.3F);
    private static Vector3 _3DCameraViewportPosition_SmallHull = new Vector3(0.75f, 0.30F, 0.4F);
    private static Vector3 _3DCameraViewportPosition_MediumHull = new Vector3(0.75f, 0.30F, 0.5F);
    private static Vector3 _3DCameraViewportPosition_LargeHull = new Vector3(0.75f, 0.30F, 0.7F);

    /// <summary>
    /// The Euler angle rotation of the hull for display.
    /// </summary>
    private static Vector3 _hullRotationAngle = new Vector3(20F, 90F, 0F);

    [SerializeField]
    private ShipDesignImageIcon _designIconPrefab = null;

    [SerializeField]
    private EquipmentIcon _equipmentIconPrefab = null;

    [SerializeField]
    private GameObject _hull3DStagePrefab = null;

    [SerializeField]
    private bool _includeObsoleteDesigns = false;    // TODO Debug location for testing. Move to UI

    private Transform _contentHolder;
    protected override Transform ContentHolder { get { return _contentHolder; } }

    /// <summary>
    /// The DesignIcon that is currently 'selected'.
    /// <remarks>Warning: do not change this value directly. Use ChangeSelectedDesignIcon() to change it.</remarks>
    /// </summary>
    private ShipDesignImageIcon _selectedDesignIcon;

    /// <summary>
    /// The 'stage' that shows the 3D model of the 'selected' design's hull.
    /// </summary>
    private GameObject _hull3DStage;

    /// <summary>
    /// List of icons associated with registered designs.
    /// </summary>
    private IList<ShipDesignImageIcon> _registeredDesignIcons;

    private UIPopupList _createDesignHullCatPopupList;
    private UIInput _createDesignNameInput;

    private GuiWindow _renameObsoleteDesignPopupWindow;
    private UIInput _renameObsoleteDesignNameInput;

    /// <summary>
    /// GameObject used to temporarily act as the parent of a DesignIcon when it is not to be shown as a registered design.
    /// <remarks>The design icon placed under this GameObject will be moved to the _registeredDesignIconsGrid if/when
    /// the design associated with the icon is 'registered' by the user pressing the ApplyDesign button. If the
    /// user takes some other action that results in the design not becoming registered, the icon will be destroyed.</remarks>
    /// </summary>
    private GameObject _transitoryDesignIconHolder;

    /// <summary>
    /// The UIGrid that allows scrolling through the icons that represent registered designs.
    /// </summary>
    private UIGrid _registeredDesignIconsGrid;

    /// <summary>
    /// Widget that contains the UI where equipment can be added or removed from a design.
    /// </summary>
    private UIWidget _designerUIContainerWidget;
    private UILabel _designerUITitleLabel;
    private UIWidget _windowControlUIContainerWidget;
    private UIWidget _designsUIContainerWidget;
    private DesignEquipmentStorage _designerEquipmentStorage;

    /// <summary>
    /// Widget that contains the UI that frames the 3D model of the 'selected' design's hull.
    /// </summary>
    private UIWidget _3DStageUIContainerWidget;

    /// <summary>
    /// The UIGrid that allows scrolling through the icons of available equipment for use in a design.
    /// </summary>
    private UIGrid _designerEquipInventoryGrid;

    protected override void Awake() {
        base.Awake();
        InitializeOnAwake();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        D.AssertNotNull(_designIconPrefab);
        D.AssertNotNull(_hull3DStagePrefab);
        InitializeContentHolder();
        InitializeCreatedDesignIconHolder();
        Subscribe();
        HideDesignerUI();
        Hide3DStage();
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _designsUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.DesignsUIContainer).GetComponent<UIWidget>();
        _designerUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.DesignerUIContainer).GetComponent<UIWidget>();
        _designerUITitleLabel = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.DesignerUITitleLabel).GetComponent<UILabel>();
        _3DStageUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.ThreeDStageUIContainer).GetComponent<UIWidget>();
        _designerEquipInventoryGrid = _designerUIContainerWidget.GetComponentInChildren<UIGrid>();
        _designerEquipInventoryGrid.sorting = UIGrid.Sorting.Alphabetic;

        _registeredDesignIconsGrid = gameObject.GetComponentsInChildren<UIGrid>().Single(grid => grid != _designerEquipInventoryGrid);
        _registeredDesignIconsGrid.sorting = UIGrid.Sorting.Alphabetic;
        _registeredDesignIcons = new List<ShipDesignImageIcon>();
        _panel.widgetsAreStatic = true; // OPTIMIZE see http://www.tasharen.com/forum/index.php?topic=261.0

        GuiWindow createDesignPopupWindow = GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.CreateDesignPopupWindow).GetComponent<GuiWindow>();
        _createDesignNameInput = createDesignPopupWindow.gameObject.GetSingleComponentInChildren<UIInput>();
        _createDesignNameInput.validation = UIInput.Validation.Alphanumeric;
        _createDesignHullCatPopupList = createDesignPopupWindow.gameObject.GetSingleComponentInChildren<UIPopupList>();
        _createDesignHullCatPopupList.keepValue = true;

        _renameObsoleteDesignPopupWindow = GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.RenameObsoleteDesignPopupWindow).GetComponent<GuiWindow>();
        _renameObsoleteDesignNameInput = _renameObsoleteDesignPopupWindow.gameObject.GetSingleComponentInChildren<UIInput>();
        _renameObsoleteDesignNameInput.validation = UIInput.Validation.Alphanumeric;

        _designerEquipmentStorage = _designerUIContainerWidget.gameObject.GetSingleComponentInChildren<DesignEquipmentStorage>();
        _windowControlUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.MenuControlsUIContainer).GetComponent<UIWidget>();
    }

    private void InitializeContentHolder() {
        _contentHolder = gameObject.GetSingleComponentInImmediateChildren<UISprite>().transform;    // background sprite
    }

    private void InitializeCreatedDesignIconHolder() {
        _transitoryDesignIconHolder = gameObject.GetComponentsInImmediateChildren<Transform>().Single(t => t != _contentHolder).gameObject;
    }

    private void Subscribe() {
        EventDelegate.Add(onShowBegin, ShowBeginEventHandler);
        EventDelegate.Add(onHideComplete, HideCompleteEventHandler);
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Event handler called when the GuiWindow begins showing.
    /// </summary>
    private void ShowBeginEventHandler() {
        BuildRegisteredDesignIcons();
        ShowDesignsUI();
    }

    /// <summary>
    /// Event handler called when the GuiWindow completes hiding.
    /// </summary>
    private void HideCompleteEventHandler() {
        Reset();
    }

    private void DesignIconDoubleClickedEventHandler(GameObject go) {
        var doubleClickedIcon = go.GetComponent<ShipDesignImageIcon>();
        ChangeSelectedDesignIcon(doubleClickedIcon);
        EditSelectedDesign();
    }

    private void DesignIconClickedEventHandler(GameObject go) {
        ShipDesignImageIcon designIcon = go.GetComponent<ShipDesignImageIcon>();
        D.AssertNotNull(designIcon);
        HandleDesignIconClicked(designIcon);
    }

    private void EquipmentIconDoubleClickedEventHandler(GameObject iconGo) {
        HandleEquipmentIconDoubleClicked(iconGo);
    }

    #endregion

    /// <summary>
    /// Handles when a design icon showing in available designs is clicked. Clicking 'selects' a designIcon.
    /// If the same designIcon is clicked twice in sequence, the icon is selected then unselected.
    /// </summary>
    /// <param name="designIcon">The designIcon that was clicked.</param>
    private void HandleDesignIconClicked(ShipDesignImageIcon designIcon) {
        if (designIcon == _selectedDesignIcon) {
            // current icon unselected by user without another selection
            ChangeSelectedDesignIcon(null);
        }
        else {
            // a new designIcon has been selected
            ChangeSelectedDesignIcon(designIcon);
        }
    }

    private void HandleEquipmentIconDoubleClicked(GameObject iconGo) {
        var eStat = iconGo.GetComponent<EquipmentIcon>().EquipmentStat;
        _designerEquipmentStorage.PlaceInEmptySlot(eStat);
    }

    /// <summary>
    /// Initializes the rename obsolete design popup window. 
    /// <remarks>Called when an obsolete design is used as the basis for editing a design.</remarks>
    /// </summary>
    private void InitializeRenameObsoleteDesignPopup() {
        _renameObsoleteDesignNameInput.value = null;
        HideDesignsUI();
        HideWindowControlUI();
        RefreshSelectedItemHudWindow(null);
    }

    #region Public API

    // Public to allow drag and drop to buttons in the GUI

    /// <summary>
    /// Show the Window.
    /// </summary>
    public void Show() {
        ShowWindow();
    }

    /// <summary>
    /// Hide the Window.
    /// </summary>
    public void Hide() {
        RefreshSelectedItemHudWindow(null);
        HideWindow();
    }

    /// <summary>
    /// Initializes the create design popup window. 
    /// <remarks>Called by Design's CreateDesign Button.</remarks>
    /// </summary>
    public void InitializeCreateDesignPopup() {
        if (_createDesignHullCatPopupList.items.IsNullOrEmpty()) {
            List<string> hullCatNames = TempGameValues.ShipHullCategoriesInUse.Select(cat => cat.GetValueName()).ToList();
            _createDesignHullCatPopupList.items = hullCatNames;
        }
        _createDesignNameInput.value = null;
        HideDesignsUI();
        HideWindowControlUI();
        RefreshSelectedItemHudWindow(null);
    }

    /// <summary>
    /// Handles showing parts of the UI when the CreateDesignPopupWindow Cancel button is clicked.
    /// </summary>
    public void HandleCreateDesignPopupCancelled() {
        ShowDesignsUI();
        ShowWindowControlUI();
    }

    /// <summary>
    /// Creates a design from the chosen Hull and DesignName.
    /// <remarks>Called by CreateDesignPopupWindow's Accept Button.</remarks>
    /// </summary>
    public void CreateDesign() {
        if (_createDesignHullCatPopupList.value.IsNullOrEmpty()) {
            D.Warn("{0}: User did not make a Hull choice when attempting to create a design.", DebugName);
            ShowDesignsUI();
            ShowWindowControlUI();
            return;
        }
        if (_createDesignNameInput.value.IsNullOrEmpty()) {
            D.Warn("{0}: User did not pick a Design name when attempting to create a design.", DebugName);
            ShowDesignsUI();
            ShowWindowControlUI();
            return;
        }
        // Acquire hull and design name from CreateDesignWindow
        string rootDesignName = _createDesignNameInput.value;
        if (_gameMgr.PlayersDesigns.IsDesignNameInUseByUser(rootDesignName)) {
            D.Warn("{0}: User picked DesignName {1} that is already in use when attempting to create a design.", DebugName, rootDesignName);
            ShowDesignsUI();
            ShowWindowControlUI();
            return;
        }
        ShipHullCategory hullCategory = Enums<ShipHullCategory>.Parse(_createDesignHullCatPopupList.value);

        // Instantiate a 'control' design with the info along with a new icon and assign it to _selectedDesignIcon.
        // This design will become the 'previousDesign' in UpdateDesigns to see if newDesign (aka WorkingDesign) has any changes.
        // The status of this 'previousDesign' needs to be system created so UpdateDesigns won't attempt to obsolete it.
        ShipDesign emptyTemplateDesign = _gameMgr.PlayersDesigns.GetUserShipDesign(hullCategory.GetEmptyTemplateDesignName());
        ShipDesign controlDesign = new ShipDesign(emptyTemplateDesign) {
            Status = AUnitDesign.SourceAndStatus.System_CreationTemplate,
            RootDesignName = rootDesignName
        };

        var controlDesignIcon = CreateIcon(controlDesign, _transitoryDesignIconHolder);
        ChangeSelectedDesignIcon(controlDesignIcon);

        // IMPROVE CreateDesignPopup is currently a full screen so don't let the HudWindow interfere with it
        RefreshSelectedItemHudWindow(null);
        HideDesignsUI();
        ShowDesignerUI();   // copies selectedIconDesign to WorkingDesign
        ShowWindowControlUI();
    }

    /// <summary>
    /// Edits the selected design.
    /// <remarks>Called by Design's EditDesign Button.</remarks>
    /// </summary>
    public void EditSelectedDesign() {
        if (_selectedDesignIcon != null) {
            if (_selectedDesignIcon.Design.Status == AUnitDesign.SourceAndStatus.Player_Obsolete) {
                InitializeRenameObsoleteDesignPopup();
                _renameObsoleteDesignPopupWindow.Show();
                return;
            }
            ShowDesignerUI();
            HideDesignsUI();
        }
        else {
            D.Warn("{0}: User attempted to edit a design without a design selected.", DebugName);
        }
    }
    ////public void EditSelectedDesign() {
    ////    if (_selectedDesignIcon != null) {
    ////        ShowDesignerUI();
    ////        HideDesignsUI();
    ////    }
    ////    else {
    ////        D.Warn("{0}: User attempted to edit a design without a design selected.", DebugName);
    ////    }
    ////}

    /// <summary>
    /// Creates a design from the chosen Hull and DesignName.
    /// <remarks>Called by RenameObsoleteDesignPopupWindow's Accept Button.</remarks>
    /// </summary>
    public void EditObsoleteDesign() {
        D.AssertNotNull(_selectedDesignIcon);
        if (_renameObsoleteDesignNameInput.value.IsNullOrEmpty()) {
            D.Warn("{0}: User did not pick a Design name when attempting to edit an obsolete design.", DebugName);
            ShowDesignsUI();
            ShowWindowControlUI();
            return;
        }
        // Acquire design name from RenameObsoleteDesignPopupWindow
        string rootDesignName = _renameObsoleteDesignNameInput.value;
        if (_gameMgr.PlayersDesigns.IsDesignNameInUseByUser(rootDesignName)) {
            D.Warn("{0}: User picked DesignName {1} that is already in use when attempting to edit an obsolete design.", DebugName, rootDesignName);
            ShowDesignsUI();
            ShowWindowControlUI();
            return;
        }

        // Instantiate a 'control' design with the info along with a new icon and assign it to _selectedDesignIcon.
        // This design will become the 'previousDesign' in UpdateDesigns to see if newDesign (aka WorkingDesign) has any changes.
        // The status of this 'previousDesign' needs to be obsolete so UpdateDesigns won't attempt to obsolete it.
        ShipDesign controlDesign = new ShipDesign(_selectedDesignIcon.Design) {
            Status = AUnitDesign.SourceAndStatus.Player_Obsolete,
            RootDesignName = rootDesignName
        };

        var controlDesignIcon = CreateIcon(controlDesign, _transitoryDesignIconHolder);
        ChangeSelectedDesignIcon(controlDesignIcon);

        // IMPROVE Popup is currently a full screen so don't let the HudWindow interfere with it
        RefreshSelectedItemHudWindow(null);
        HideDesignsUI();
        ShowDesignerUI();   // copies selectedIconDesign to WorkingDesign
        ShowWindowControlUI();
    }

    /// <summary>
    /// Handles showing parts of the UI when the RenameObsoleteDesignPopupWindow Cancel button is clicked.
    /// </summary>
    public void HandleRenameObsoleteDesignPopupCancelled() {
        ShowDesignsUI();
        ShowWindowControlUI();
    }

    /// <summary>
    /// Registers the finished design and shows its new icon in the Design's ScrollView.
    /// <remarks>Called by Designer's Apply Button.</remarks>
    /// </summary>
    public void ApplyDesign() {
        var previousDesign = _selectedDesignIcon.Design;
        ShipDesign newDesign = _designerEquipmentStorage.WorkingDesign as ShipDesign;
        if (!GameUtility.IsDesignContentEqual(previousDesign, newDesign)) {
            // The user modified the design
            D.Log("{0}.ApplyDesign: {1} has changed and is being registered as a new design.", DebugName, newDesign.DebugName);
            UpdateDesign(_selectedDesignIcon, newDesign);
        }
        else if (previousDesign.Status == AUnitDesign.SourceAndStatus.System_CreationTemplate) {
            // The user has chosen to create an empty design that has the same content as the empty CreationTemplateDesign
            D.Log("{0}.ApplyDesign: {1} has not changed but is being registered as a new design.", DebugName, newDesign.DebugName);
            UpdateDesign(_selectedDesignIcon, newDesign);
        }
        else {
            D.Log("{0}.ApplyDesign: {1} will not be registered as a new design.", DebugName, newDesign.DebugName);
        }

        ShowDesignsUI();
        HideDesignerUI();
        // UpdateDesign hides the SelectedItemHudWindow
    }

    /// <summary>
    /// Resets the work in process design to its original state.
    /// <remarks>Called by Designer's Reset Button.</remarks>
    /// </summary>
    public void ResetDesigner() {
        RemoveEquipmentStorageIcons();
        InstallEquipmentStorageIconsFor(_selectedDesignIcon.Design);
        MyCustomCursor.Clear();
    }

    public void AutoDesigner() {
        D.Warn("{0}: AutoDesigner not yet implemented.", DebugName);
        // UNDONE
    }

    /// <summary>
    /// Hides the Designer UI portion of the window without applying any design changes.
    /// <remarks>Called by Designer's Close Button.</remarks>
    /// </summary>
    public void CloseDesigner() {
        HideDesignerUI();
        ShowDesignsUI();
    }

    /// <summary>
    /// Obsoletes the selected design, if any.
    /// <remarks>Called by Design's Obsolete Button.</remarks>
    /// </summary>
    public void ObsoleteSelectedDesign() {
        if (_selectedDesignIcon != null) {
            ShipDesign selectedDesign = _selectedDesignIcon.Design;
            _gameMgr.PlayersDesigns.ObsoleteUserShipDesign(selectedDesign.DesignName);

            if (!_includeObsoleteDesigns) {
                RemoveIcon(_selectedDesignIcon);
            }
            ChangeSelectedDesignIcon(null);
            HideDesignerUI();
            _registeredDesignIconsGrid.repositionNow = true;
        }
        else {
            D.Warn("{0}: User attempted to obsolete a design without a design selected.", DebugName);
        }
    }

    #endregion

    /// <summary>
    /// Changes _selectedDesignIcon to newSelectedDesignIcon, destroying any icon already assigned
    /// to _selectedDesignIcon if that icon is not present in the list of registered design icons.
    /// Also shows or clears the SelectedItemHudWindow depending on the value of newSelectedDesignIcon
    /// and manages the IsSelected status of the icons.
    /// <remarks>Handled this way to make sure any partially created icon and design that has not
    /// yet been applied is properly destroyed.</remarks>
    /// </summary>
    /// <param name="newSelectedDesignIcon">The new selected design icon.</param>
    private void ChangeSelectedDesignIcon(ShipDesignImageIcon newSelectedDesignIcon) {
        if (_selectedDesignIcon != null) {
            if (_selectedDesignIcon.gameObject != null) {    // could already be destroyed
                if (!_registeredDesignIcons.Contains(_selectedDesignIcon)) {
                    // design and icon were created but never accepted and added to list of icons and registered designs
                    Destroy(_selectedDesignIcon.gameObject);
                }
            }
            _selectedDesignIcon.IsSelected = false;
        }
        _selectedDesignIcon = newSelectedDesignIcon;
        if (_selectedDesignIcon != null) {
            _selectedDesignIcon.IsSelected = true;
            RefreshSelectedItemHudWindow(_selectedDesignIcon.Design);
        }
        else {
            RefreshSelectedItemHudWindow(null);
        }
    }

    /// <summary>
    /// Shows the designer UI and populates it with the Design embedded in _selectedDesignIcon, prepared to be edited.
    /// </summary>
    private void ShowDesignerUI() {
        D.AssertNotNull(_selectedDesignIcon);
        BuildAvailableEquipmentIcons();
        ShipDesign design = _selectedDesignIcon.Design;

        InstallEquipmentStorageIconsFor(design);
        _designerUITitleLabel.text = DesignerUITitleRoot + Constants.Colon + Constants.Space + design.DesignName;
        _designerUIContainerWidget.alpha = Constants.OneF;
        Show3DStage(design.HullCategory);
    }

    private void ShowDesignsUI() {
        _designsUIContainerWidget.alpha = Constants.OneF;
    }

    private void Show3DStage(ShipHullCategory hullCat) {
        D.AssertNull(_hull3DStage);
        _hull3DStage = NGUITools.AddChild(Gui3DStageFolder.Instance.Folder.gameObject, _hull3DStagePrefab);
        D.AssertEqual(Layers.Default, (Layers)_hull3DStage.layer);

        Camera stageCamera = _hull3DStage.GetSingleComponentInImmediateChildren<Camera>();

        Vector3 viewportPosition = default(Vector3);
        switch (hullCat) {
            case ShipHullCategory.Frigate:
                viewportPosition = _3DCameraViewportPosition_TinyHull;
                break;
            case ShipHullCategory.Destroyer:
            case ShipHullCategory.Support:
                viewportPosition = _3DCameraViewportPosition_SmallHull;
                break;
            case ShipHullCategory.Cruiser:
            case ShipHullCategory.Colonizer:
            case ShipHullCategory.Investigator:
                viewportPosition = _3DCameraViewportPosition_MediumHull;
                break;
            case ShipHullCategory.Dreadnought:
            case ShipHullCategory.Carrier:
            case ShipHullCategory.Troop:
                viewportPosition = _3DCameraViewportPosition_LargeHull;
                break;
            case ShipHullCategory.Scout:
            case ShipHullCategory.Fighter:
            case ShipHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        Vector3 desiredHullPosition = stageCamera.ViewportToWorldPoint(viewportPosition);

        Revolver hullRevolver = _hull3DStage.GetSingleComponentInImmediateChildren<Revolver>();
        hullRevolver.transform.position = desiredHullPosition;
        hullRevolver.enabled = true;
        GameObject hullParentGo = hullRevolver.gameObject;

        var hullPrefabGo = RequiredPrefabs.Instance.shipHulls.Single(hull => hull.HullCategory == hullCat).gameObject;
        GameObject hullInstanceGo = NGUITools.AddChild(hullParentGo, hullPrefabGo);

        hullInstanceGo.transform.localEulerAngles = _hullRotationAngle;
        var hullCollider = hullInstanceGo.AddComponent<SphereCollider>();
        hullCollider.radius = hullCat.Dimensions().magnitude / 2F;
        hullInstanceGo.AddComponent<SpinWithMouse>();   // IMPROVE currently an NGUI example script

        var hullMeshGo = hullInstanceGo.GetComponent<ShipHull>().HullMesh.gameObject;
        var hullMeshCameraLosChgdComponent = hullMeshGo.GetComponent<CameraLosChangedListener>();
        Destroy(hullMeshCameraLosChgdComponent);    // not needed for this display application
        hullMeshGo.layer = (int)Layers.Default;

        var hullMeshRenderer = hullMeshGo.GetComponent<MeshRenderer>();
        D.Assert(!hullMeshRenderer.enabled);
        hullMeshRenderer.enabled = true;

        _3DStageUIContainerWidget.alpha = Constants.OneF;
    }

    private void ShowWindowControlUI() {
        _windowControlUIContainerWidget.alpha = Constants.OneF;
    }

    // OPTIMIZE With control over showing these elements, I can probably lose the collider that blocks 
    // access to anything underneath the CreateDesignPopupWindow

    private void HideDesignerUI() {
        _designerUIContainerWidget.alpha = Constants.ZeroF;
        _designerUITitleLabel.text = DesignerUITitleRoot;
        RemoveAvailableEquipmentIcons();
        RemoveEquipmentStorageIcons();
        MyCustomCursor.Clear();
        Hide3DStage();
    }

    private void HideDesignsUI() {
        _designsUIContainerWidget.alpha = Constants.ZeroF;
    }

    private void Hide3DStage() {
        if (_hull3DStage != null) {
            Destroy(_hull3DStage);
            _hull3DStage = null;
        }
        _3DStageUIContainerWidget.alpha = Constants.ZeroF;
    }

    private void HideWindowControlUI() {
        _windowControlUIContainerWidget.alpha = Constants.ZeroF;
    }

    private void UpdateDesign(ShipDesignImageIcon previousDesignIcon, ShipDesign newDesign) {
        D.AssertEqual(_selectedDesignIcon, previousDesignIcon);
        D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, newDesign.Status);

        // handle the previous design and its icon
        ShipDesign previousDesign = previousDesignIcon.Design;
        // capture previousDesignStatus before it is potentially changed to obsolete
        AUnitDesign.SourceAndStatus previousDesignStatus = previousDesign.Status;
        if (previousDesignStatus == AUnitDesign.SourceAndStatus.Player_Current) {
            // current design that has just been updated to newDesign so obsolete previousDesign
            _gameMgr.PlayersDesigns.ObsoleteUserShipDesign(previousDesign.DesignName);
            // Only remove the previousDesignIcon from displayed and registered designs when the source of the design is current.
            // If previous source is system then it isn't displayed, and if it is obsolete, the user has chosen to display obsolete.
            // Even if current the icon should only be removed when not showing obsolete.
            if (!_includeObsoleteDesigns) {
                RemoveIcon(previousDesignIcon);
            }
        }

        if (previousDesignStatus == AUnitDesign.SourceAndStatus.Player_Current) {
            // Only increment the newDesign name when the source of the design is current 
            // as newDesigns from both system and obsolete sources get new names created by the user
            newDesign.IncrementDesignName();
        }

        // add the new design and its icon
        _gameMgr.PlayersDesigns.Add(newDesign);
        var newDesignIcon = CreateIcon(newDesign, _registeredDesignIconsGrid.gameObject);
        AddIcon(newDesignIcon);

        // clear any prior selection
        ChangeSelectedDesignIcon(null);
        _registeredDesignIconsGrid.repositionNow = true;
    }

    /// <summary>
    /// Build the collection of icons that represent User registered designs.
    /// </summary>
    private void BuildRegisteredDesignIcons() {
        D.AssertEqual(Constants.Zero, _registeredDesignIcons.Count);
        RemoveRegisteredDesignIcons();   // OPTIMIZE Reqd to destroy the icon already present. Can be removed once reuse of icons is implemented
        IEnumerable<ShipDesign> designs = GetRegisteredUserShipDesigns();
        designs.ForAll(design => {
            var designIcon = CreateIcon(design, _registeredDesignIconsGrid.gameObject);
            AddIcon(designIcon);
        });
        _registeredDesignIconsGrid.repositionNow = true;
    }

    /// <summary>
    /// Build the collection of icons that represent equipment available for use in creating or editing a design.
    /// </summary>
    private void BuildAvailableEquipmentIcons() {
        RemoveAvailableEquipmentIcons();   // OPTIMIZE Reqd to destroy the icon already present. Can be removed once reuse of icons is implemented
        IEnumerable<AEquipmentStat> availableEquipStats = GetAvailableUserEquipmentStats();

        // make grid 2 rows deep
        int maxStatsPerRow = Mathf.CeilToInt(availableEquipStats.Count() / (float)2);
        //D.Log("{0}: Equipment count = {1}, allowedEquipmentPerRow = {2}.", DebugName, equipStats.Count(), maxStatsPerRow);
        _designerEquipInventoryGrid.maxPerLine = maxStatsPerRow;

        availableEquipStats.ForAll(eStat => AddIcon(eStat));
        _designerEquipInventoryGrid.repositionNow = true;
    }

    /// <summary>
    /// Creates the equipment storage representation of the provided design using EquipmentStorageIcons.
    /// </summary>
    /// <param name="design">The design.</param>
    private void InstallEquipmentStorageIconsFor(ShipDesign design) {
        ShipDesign workingDesign = new ShipDesign(design);
        _designerEquipmentStorage.InstallEquipmentStorageIconsFor(workingDesign);
    }

    private void RemoveRegisteredDesignIcons() {
        if (!_registeredDesignIcons.Any()) {
            // Could be newly Awakened in which case it will have an icon present for debug inspection
            IList<Transform> iconTransforms = _registeredDesignIconsGrid.GetChildList();
            if (iconTransforms.Any()) {
                D.AssertEqual(Constants.One, iconTransforms.Count);
                foreach (var it in iconTransforms) {
                    var icon = it.GetComponent<ShipDesignImageIcon>();
                    RemoveIcon(icon);
                }
            }
        }
        else {
            var iconsCopy = new List<ShipDesignImageIcon>(_registeredDesignIcons);
            iconsCopy.ForAll(i => {
                RemoveIcon(i);
            });
        }
    }

    private void RemoveAvailableEquipmentIcons() {
        IList<Transform> iconTransforms = _designerEquipInventoryGrid.GetChildList();
        if (iconTransforms.Any()) {
            foreach (var it in iconTransforms) {
                var icon = it.GetComponent<EquipmentIcon>();
                RemoveIcon(icon);
            }
        }
    }

    private void RemoveEquipmentStorageIcons() {
        _designerEquipmentStorage.RemoveEquipmentStorageIcons();
    }

    /// <summary>
    /// Creates a ShipDesignImageIcon from the provided design and parents it to parent. 
    /// <remarks>Handled this way to avoid adding the icon generated by CreateDesign to the list of
    /// available designs. This icon is not an available design until the design is accepted by pressing
    /// the ApplyDesign button in the DesignerUI.</remarks>
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    private ShipDesignImageIcon CreateIcon(ShipDesign design, GameObject parent) {
        GameObject designIconGo = NGUITools.AddChild(parent, _designIconPrefab.gameObject);
        designIconGo.name = design.DesignName + DesignIconExtension;
        ShipDesignImageIcon designIcon = designIconGo.GetSafeComponent<ShipDesignImageIcon>(); ;
        designIcon.Design = design;
        return designIcon;
    }

    /// <summary>
    /// Adds the provided ShipDesignImageIcon to the list of available design icons displayed in
    /// a scroll view. If the icon is not already parented to _availableDesignsGrid, its parent will be changed.
    /// <remarks>Handled this way to avoid adding the icon generated by CreateDesign to the list of 
    /// available designs. This icon is not an available design until the design is accepted by pressing
    /// the ApplyDesign button in the DesignerUI.</remarks>
    /// </summary>
    /// <param name="designIcon">The design icon.</param>
    private void AddIcon(ShipDesignImageIcon designIcon) {
        if (designIcon.transform.parent != _registeredDesignIconsGrid.transform) {
            UnityUtility.AttachChildToParent(designIcon.gameObject, _registeredDesignIconsGrid.gameObject);
        }
        var eventListener = UIEventListener.Get(designIcon.gameObject);
        eventListener.onDoubleClick += DesignIconDoubleClickedEventHandler;
        eventListener.onClick += DesignIconClickedEventHandler;

        _registeredDesignIcons.Add(designIcon);
    }

    private void AddIcon(AEquipmentStat equipStat) {
        GameObject equipIconGo = NGUITools.AddChild(_designerEquipInventoryGrid.gameObject, _equipmentIconPrefab.gameObject);
        equipIconGo.name = equipStat.Name + EquipmentIconExtension;
        EquipmentIcon equipIcon = equipIconGo.GetSafeComponent<EquipmentIcon>(); ;
        equipIcon.EquipmentStat = equipStat;

        UIEventListener.Get(equipIconGo).onDoubleClick += EquipmentIconDoubleClickedEventHandler;
    }

    private void RemoveIcon(ShipDesignImageIcon designIcon) {
        var eventListener = UIEventListener.Get(designIcon.gameObject);
        eventListener.onDoubleClick -= DesignIconDoubleClickedEventHandler;
        eventListener.onClick -= DesignIconClickedEventHandler;

        _registeredDesignIcons.Remove(designIcon);
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(designIcon.gameObject);
    }

    private void RemoveIcon(EquipmentIcon equipIcon) {
        UIEventListener.Get(equipIcon.gameObject).onDoubleClick -= EquipmentIconDoubleClickedEventHandler;

        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(equipIcon.gameObject);
    }

    private void RefreshSelectedItemHudWindow(ShipDesign design) {
        if (design == null) {
            SelectedItemHudWindow.Instance.Hide();
        }
        else {
            SelectedItemHudWindow.Instance.Show(FormID.SelectedShipDesign, design);
        }
    }

    private IEnumerable<ShipDesign> GetRegisteredUserShipDesigns() {
        return _gameMgr.PlayersDesigns.GetAllUserShipDesigns(_includeObsoleteDesigns);
    }

    private IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.__GetAvailableUserElementEquipmentStats();
    }

    private void Reset() {
        RemoveRegisteredDesignIcons();
        ChangeSelectedDesignIcon(null);
        HideDesignerUI();
        D.AssertEqual(Constants.Zero, _registeredDesignIcons.Count);
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        EventDelegate.Remove(onShowBegin, ShowBeginEventHandler);
        EventDelegate.Remove(onHideComplete, HideCompleteEventHandler);
    }

    #region Debug


    #endregion


}

