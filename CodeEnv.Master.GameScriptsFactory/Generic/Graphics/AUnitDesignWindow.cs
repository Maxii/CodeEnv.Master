﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitDesignWindow.cs
// Abstract base class for Unit Cmd and Element Design Windows.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for Unit Cmd and Element Design Windows.
/// </summary>
public abstract class AUnitDesignWindow : AGuiWindow {

    private const string DesignerUITitleRoot = "Designer";

    private const string DesignIconExtension = " DesignIcon";

    private const string EquipmentIconExtension = " EquipmentIcon";

    /// <summary>
    /// The position in 3DStageCamera's Viewport space where the 3D model should be located.
    /// </summary>
    private static Vector3 _3DCameraViewportPosition_Closest = new Vector3(0.75F, 0.28F, 0.2F);
    private static Vector3 _3DCameraViewportPosition_Close = new Vector3(0.75F, 0.28F, 0.35F);
    private static Vector3 _3DCameraViewportPosition_Average = new Vector3(0.75F, 0.28F, 0.6F);
    private static Vector3 _3DCameraViewportPosition_Far = new Vector3(0.75F, 0.28F, 1.0F);
    private static Vector3 _3DCameraViewportPosition_Farthest = new Vector3(0.75F, 0.28F, 2.0F);

    /// <summary>
    /// The Euler angle rotation of the hull for display.
    /// </summary>
    private static Vector3 _threeDModelRotationAngle = new Vector3(20F, 90F, 0F);

    [SerializeField]    // 7.29.17 Ability to place prefabs in Editor fields not needed while using DesignScreensManager
    private UnitDesignIcon _designIconPrefab = null;

    [SerializeField]    // 7.29.17 Ability to place prefabs in Editor fields not needed while using DesignScreensManager
    private EquipmentIcon _equipmentIconPrefab = null;

    [SerializeField]    // 7.29.17 Ability to place prefabs in Editor fields not needed while using DesignScreensManager
    private GameObject _threeDModelStagePrefab = null;

    [SerializeField]
    private bool _includeObsoleteDesigns = false;    // TODO Debug location for testing. Move to UI

    private Transform _contentHolder;
    protected override Transform ContentHolder { get { return _contentHolder; } }

    /// <summary>
    /// The DesignIcon that is currently chosen.
    /// <remarks>Warning: do not change this value directly. Use ChangeChosenDesignIcon() to change it.</remarks>
    /// </summary>
    private UnitDesignIcon _chosenDesignIcon;

    /// <summary>
    /// The 'stage' that shows the 3D model of the chosen design.
    /// </summary>
    private GameObject _threeDModelStage;

    /// <summary>
    /// List of icons associated with registered designs.
    /// </summary>
    private IList<UnitDesignIcon> _registeredDesignIcons;

    private GuiWindow _createDesignPopupWindow;
    private UIWidget _createDesignPopupListContainer;
    private UIPopupList _createDesignPopupList;
    private UILabel _createDesignPopupListTitle;
    private UIInput _createDesignNameInput;

    private GuiWindow _renameObsoleteDesignPopupWindow;
    private UIInput _renameObsoleteDesignNameInput;

    /// <summary>
    /// GameObject used to temporarily act as the parent of a DesignIcon when it is not to be shown as a registered design.
    /// <remarks>The design icon placed under this GameObject will be moved to the _registeredDesignIconsGrid if/when
    /// the design associated with the icon is 'registered' by the user pressing the ApplyDesign button. If the
    /// user takes some other action that results in the design not becoming registered, the icon will be destroyed.</remarks>
    /// </summary>
    private GameObject _transientDesignIconHolder;

    /// <summary>
    /// The UIGrid that allows scrolling through the icons that represent registered designs.
    /// </summary>
    private UIGrid _registeredDesignIconsGrid;

    /// <summary>
    /// The size of the design icon to display in the _registeredDesignIconsGrid.
    /// </summary>
    private AMultiSizeGuiIcon.IconSize _designIconSize;

    /// <summary>
    /// Widget that contains the UI where equipment can be added or removed from a design.
    /// </summary>
    private UIWidget _designerUIContainerWidget;
    private UILabel _designerUITitleLabel;
    private UIWidget _windowControlsUIContainerWidget;
    private UIWidget _designsUIContainerWidget;
    private DesignEquipmentStorage _designerEquipmentStorage;

    /// <summary>
    /// Widget that contains the UI that frames the 3D model of the 'selected' design.
    /// </summary>
    private UIWidget _3DModelStageUIContainerWidget;

    /// <summary>
    /// The UIGrid that allows scrolling through the icons of available equipment for use in a design.
    /// </summary>
    private UIGrid _designerEquipmentIconsGrid;

    protected sealed override void Awake() {
        base.Awake();
        InitializeOnAwake();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        //ValidatePrefabs();    // will fail while using DesignScreensManager as it populates the prefab fields
        InitializeContentHolder();
        InitializeTransientDesignIconHolder();
        Subscribe();
        HideDesignerUI();
        Hide3DModelStage();
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        //D.Log("{0}: GuiElementIDs = {1}.", DebugName, gameObject.GetComponentsInChildren<GuiElement>().Select(e => e.ElementID.GetValueName()).Concatenate());
        _designsUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.DesignsUIContainer).GetComponent<UIWidget>();
        _designerUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.DesignerUIContainer).GetComponent<UIWidget>();
        _designerUITitleLabel = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.DesignerUITitleLabel).GetComponent<UILabel>();
        _3DModelStageUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.ThreeDStageUIContainer).GetComponent<UIWidget>();
        _designerEquipmentIconsGrid = _designerUIContainerWidget.gameObject.GetComponentsInChildren<UIGrid>().Single(grid => grid.GetComponentInParent<DesignEquipmentStorage>() == null);
        _designerEquipmentIconsGrid.arrangement = UIGrid.Arrangement.Horizontal;
        _designerEquipmentIconsGrid.sorting = UIGrid.Sorting.Alphabetic;

        _registeredDesignIconsGrid = _designsUIContainerWidget.gameObject.GetSingleComponentInChildren<UIGrid>();
        _registeredDesignIconsGrid.sorting = UIGrid.Sorting.Alphabetic;
        _registeredDesignIcons = new List<UnitDesignIcon>();
        _panel.widgetsAreStatic = true; // OPTIMIZE see http://www.tasharen.com/forum/index.php?topic=261.0

        _createDesignPopupWindow = GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.CreateDesignPopupWindow).GetComponent<GuiWindow>();
        _createDesignPopupList = _createDesignPopupWindow.gameObject.GetSingleComponentInChildren<UIPopupList>();
        _createDesignPopupListContainer = _createDesignPopupList.gameObject.GetSafeFirstComponentInParents<UIWidget>(excludeSelf: true);
        _createDesignPopupListTitle = _createDesignPopupListContainer.gameObject.GetSingleComponentInImmediateChildren<UILabel>();
        _createDesignPopupList.keepValue = true;

        _createDesignNameInput = _createDesignPopupWindow.gameObject.GetSingleComponentInChildren<UIInput>();
        _createDesignNameInput.validation = UIInput.Validation.Alphanumeric;

        _renameObsoleteDesignPopupWindow = GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.RenameObsoleteDesignPopupWindow).GetComponent<GuiWindow>();
        _renameObsoleteDesignNameInput = _renameObsoleteDesignPopupWindow.gameObject.GetSingleComponentInChildren<UIInput>();
        _renameObsoleteDesignNameInput.validation = UIInput.Validation.Alphanumeric;

        _designerEquipmentStorage = _designerUIContainerWidget.gameObject.GetSingleComponentInChildren<DesignEquipmentStorage>();
        _windowControlsUIContainerWidget = gameObject.GetComponentsInChildren<GuiElement>().Single(e => e.ElementID == GuiElementID.MenuControlsUIContainer).GetComponent<UIWidget>();
    }

    private void ValidatePrefabs() {
        D.AssertNotNull(_designIconPrefab);
        D.AssertNotNull(_equipmentIconPrefab);
        D.AssertNotNull(_threeDModelStagePrefab);
    }

    private void InitializeContentHolder() {
        _contentHolder = gameObject.GetSingleComponentInImmediateChildren<UISprite>().transform;    // background sprite
    }

    private void InitializeTransientDesignIconHolder() {
        _transientDesignIconHolder = gameObject.GetComponentsInImmediateChildren<Transform>().Single(t => t != _contentHolder).gameObject;
    }

    protected override void Subscribe() {
        base.Subscribe();
        EventDelegate.Add(onShowBegin, ShowBeginEventHandler);
    }
    ////private void Subscribe() {
    ////    EventDelegate.Add(onShowBegin, ShowBeginEventHandler);
    ////    EventDelegate.Add(onHideComplete, HideCompleteEventHandler);
    ////}

    #region Event and Property Change Handlers

    /// <summary>
    /// Event handler called when the GuiWindow begins showing.
    /// </summary>
    private void ShowBeginEventHandler() {
        BuildRegisteredDesignIcons();
        ShowDesignsUI();
    }

    /////// <summary>
    /////// Event handler called when the GuiWindow completes hiding.
    /////// </summary>
    ////private void HideCompleteEventHandler() {
    ////    ResetforReuse();
    ////}

    private void DesignIconDoubleClickedEventHandler(GameObject go) {
        var doubleClickedIcon = go.GetComponent<UnitDesignIcon>();
        ChangeChosenDesignIcon(doubleClickedIcon);
        EditChosenDesign();
    }

    private void DesignIconClickedEventHandler(GameObject go) {
        UnitDesignIcon designIcon = go.GetComponent<UnitDesignIcon>();
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
    private void HandleDesignIconClicked(UnitDesignIcon designIcon) {
        if (designIcon == _chosenDesignIcon) {
            // current icon re-chosen by user without another choice
            ChangeChosenDesignIcon(null);
        }
        else {
            // a new designIcon has been chosen
            ChangeChosenDesignIcon(designIcon);
        }
    }

    private void HandleEquipmentIconDoubleClicked(GameObject iconGo) {
        var eStat = iconGo.GetComponent<EquipmentIcon>().EquipmentStat;
        bool isPlaced = _designerEquipmentStorage.PlaceInEmptySlot(eStat);
        if (isPlaced) {
            SFXManager.Instance.PlaySFX(SfxClipID.Tap);
        }
        else {
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
        }
    }

    /// <summary>
    /// Initializes and shows the rename obsolete design popup window, adjusting other UI
    /// components as needed.
    /// <remarks>Called when an obsolete design is used as the basis for editing a design.</remarks>
    /// </summary>
    private void ShowRenameObsoleteDesignPopupWindow() {
        _renameObsoleteDesignNameInput.value = null;
        HideDesignsUI();
        HideWindowControlsUI();
        _renameObsoleteDesignPopupWindow.Show();
    }

    #region Public API

    // Public to allow drag and drop to buttons in the GUI

    /// <summary>
    /// Initializes the prefab fields of this DesignWindow.
    /// <remarks>Called by DesignScreensManager when it installs a new AUnitDesignWindow.</remarks>
    /// </summary>
    public void __InitializePrefabs(UnitDesignIcon designIconPrefab, EquipmentIcon equipmentIconPrefab, GameObject threeDModelStagePrefab) {
        _designIconPrefab = designIconPrefab;
        _equipmentIconPrefab = equipmentIconPrefab;
        _threeDModelStagePrefab = threeDModelStagePrefab;
    }

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
        HideWindow();
    }

    /// <summary>
    /// Initializes the create design popup window. 
    /// <remarks>Called by Design's CreateDesign Button.</remarks>
    /// </summary>
    public void InitializeCreateDesignPopup() {
        string popupTitle;
        List<string> popupContent;
        if (TryGetCreateDesignPopupContent(out popupTitle, out popupContent)) {
            _createDesignPopupListTitle.text = popupTitle;
            _createDesignPopupList.items = popupContent;
        }
        else {
            _createDesignPopupListContainer.gameObject.SetActive(false);
        }
        _createDesignNameInput.value = null;
        HideDesignsUI();
        HideWindowControlsUI();
    }

    /// <summary>
    /// Handles showing parts of the UI when the CreateDesignPopupWindow Cancel button is clicked.
    /// </summary>
    public void HandleCreateDesignPopupCancelled() {
        ShowDesignsUI();
        ShowWindowControlsUI();
    }

    /// <summary>
    /// Creates a design from the chosen Hull and DesignName.
    /// <remarks>Called by CreateDesignPopupWindow's Accept Button.</remarks>
    /// </summary>
    public void CreateDesign() {
        if (_createDesignNameInput.value.IsNullOrEmpty()) {
            D.Warn("{0}: User did not include a Design name when attempting to create a design.", DebugName);
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
            ShowDesignsUI();
            ShowWindowControlsUI();
            return;
        }
        // Acquire hull and design name from CreateDesignWindow
        string rootDesignName = _createDesignNameInput.value;
        if (_gameMgr.PlayersDesigns.IsDesignNameInUseByUser(rootDesignName)) {
            D.Warn("{0}: User picked DesignName {1} that is already in use when attempting to create a design.", DebugName, rootDesignName);
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
            ShowDesignsUI();
            ShowWindowControlsUI();
            return;
        }

        string chosenPopupValue = null;
        if (_createDesignPopupList.gameObject.activeInHierarchy) {
            // Not all create design popup windows require the user to use a popupList to provide a emptyTemplateDesign name hint.
            // E.g. The design of a Command has no counterpart to the different hulls that elements have.
            if (_createDesignPopupList.value.IsNullOrEmpty()) {
                D.Warn("{0}: User did not make a Hull choice when attempting to create a design.", DebugName);
                SFXManager.Instance.PlaySFX(SfxClipID.Error);
                ShowDesignsUI();
                ShowWindowControlsUI();
                return;
            }
            chosenPopupValue = _createDesignPopupList.value;
        }
        AUnitDesign emptyTemplateDesign = GetEmptyTemplateDesign(chosenPopupValue);

        // Instantiate a 'control' design with the info along with a new icon and assign it to _selectedDesignIcon.
        // This design will become the 'previousDesign' in UpdateDesigns to see if newDesign (aka WorkingDesign) has any changes.
        // The status of this 'previousDesign' needs to be system created so UpdateDesigns won't attempt to obsolete it.
        AUnitDesign controlDesign = CopyDesignFrom(emptyTemplateDesign);
        controlDesign.Status = AUnitDesign.SourceAndStatus.System_CreationTemplate;
        controlDesign.RootDesignName = rootDesignName;

        var controlDesignIcon = CreateIcon(controlDesign, _designIconSize, _transientDesignIconHolder);
        ChangeChosenDesignIcon(controlDesignIcon);

        HideDesignsUI();
        ShowDesignerUI();   // copies selectedIconDesign to WorkingDesign
        ShowWindowControlsUI();
    }

    /// <summary>
    /// Edits the selected design.
    /// <remarks>Called by Design's EditDesign Button.</remarks>
    /// </summary>
    public void EditChosenDesign() {
        if (_chosenDesignIcon != null) {
            if (_chosenDesignIcon.Design.Status == AUnitDesign.SourceAndStatus.Player_Obsolete) {
                ShowRenameObsoleteDesignPopupWindow();
                return;
            }
            ShowDesignerUI();
            HideDesignsUI();
        }
        else {
            D.Warn("{0}: User attempted to edit a design without a design chosen.", DebugName);
        }
    }

    /// <summary>
    /// Allows editing of the chosen obsolete Design after its name has been changed.
    /// <remarks>Called by RenameObsoleteDesignPopupWindow's Accept Button.</remarks>
    /// </summary>
    public void EditObsoleteDesign() {
        D.AssertNotNull(_chosenDesignIcon);
        if (_renameObsoleteDesignNameInput.value.IsNullOrEmpty()) {
            D.Warn("{0}: User did not include a new Design name when attempting to edit an obsolete design.", DebugName);
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
            ShowDesignsUI();
            ShowWindowControlsUI();
            return;
        }
        // Acquire design name from RenameObsoleteDesignPopupWindow
        string rootDesignName = _renameObsoleteDesignNameInput.value;
        if (_gameMgr.PlayersDesigns.IsDesignNameInUseByUser(rootDesignName)) {
            D.Warn("{0}: User picked DesignName {1} that is already in use when attempting to edit an obsolete design.", DebugName, rootDesignName);
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
            ShowDesignsUI();
            ShowWindowControlsUI();
            return;
        }

        // Instantiate a 'control' design with the info along with a new icon and assign it to _chosenDesignIcon.
        // This design will become the 'previousDesign' in UpdateDesigns to see if newDesign (aka WorkingDesign) has any changes.
        // The status of this 'previousDesign' needs to be obsolete so UpdateDesigns won't attempt to obsolete it.
        AUnitDesign controlDesign = CopyDesignFrom(_chosenDesignIcon.Design);
        controlDesign.Status = AUnitDesign.SourceAndStatus.Player_Obsolete;
        controlDesign.RootDesignName = rootDesignName;

        var controlDesignIcon = CreateIcon(controlDesign, _designIconSize, _transientDesignIconHolder);
        ChangeChosenDesignIcon(controlDesignIcon);

        HideDesignsUI();
        ShowDesignerUI();   // copies chosenIconDesign to WorkingDesign
        ShowWindowControlsUI();
    }

    /// <summary>
    /// Handles showing parts of the UI when the RenameObsoleteDesignPopupWindow Cancel button is clicked.
    /// </summary>
    public void HandleRenameObsoleteDesignPopupCancelled() {
        ShowDesignsUI();
        ShowWindowControlsUI();
    }

    /// <summary>
    /// Registers the finished design and shows its new icon in the Design's ScrollView.
    /// <remarks>Called by Designer's Apply Button.</remarks>
    /// </summary>
    public void ApplyDesign() {
        var previousDesign = _chosenDesignIcon.Design;
        AUnitDesign newDesign = _designerEquipmentStorage.WorkingDesign;
        if (!IsDesignContentEqual(previousDesign, newDesign)) {
            // The user modified the design
            D.Log("{0}.ApplyDesign: {1} has changed and is being registered as a new design.", DebugName, newDesign.DebugName);
            UpdateDesign(_chosenDesignIcon, newDesign);
        }
        else if (previousDesign.Status == AUnitDesign.SourceAndStatus.System_CreationTemplate) {
            // The user has chosen to create an empty design that has the same content as the empty CreationTemplateDesign
            D.Log("{0}.ApplyDesign: {1} has not changed but is being registered as a new design.", DebugName, newDesign.DebugName);
            UpdateDesign(_chosenDesignIcon, newDesign);
        }
        else {
            D.Log("{0}.ApplyDesign: {1} will not be registered as a new design.", DebugName, newDesign.DebugName);
        }

        ShowDesignsUI();
        HideDesignerUI();
        // UpdateDesign hides the InteractableHudWindow
    }

    /// <summary>
    /// Resets the work in process design to its original state.
    /// <remarks>Called by Designer's Reset Button.</remarks>
    /// </summary>
    public void ResetDesigner() {
        RemoveEquipmentStorageIcons();
        InstallEquipmentStorageIconsFor(_chosenDesignIcon.Design);
        MyCustomCursor.Clear();
    }

    public void AutoDesigner() {
        D.Warn("{0}: AutoDesigner not yet implemented.", DebugName);
        SFXManager.Instance.PlaySFX(SfxClipID.Error);
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
    /// Obsoletes the chosen design, if any.
    /// <remarks>Called by Design's Obsolete Button.</remarks>
    /// </summary>
    public void ObsoleteChosenDesign() {
        if (_chosenDesignIcon != null) {
            AUnitDesign selectedDesign = _chosenDesignIcon.Design;
            ObsoleteDesign(selectedDesign.DesignName);
            SFXManager.Instance.PlaySFX(SfxClipID.OpenShut);

            if (!_includeObsoleteDesigns) {
                RemoveIcon(_chosenDesignIcon);
            }
            ChangeChosenDesignIcon(null);
            HideDesignerUI();
            _registeredDesignIconsGrid.repositionNow = true;
        }
        else {
            D.Warn("{0}: User attempted to obsolete a design without a design chosen.", DebugName);
            SFXManager.Instance.PlaySFX(SfxClipID.Error);
        }
    }

    #endregion

    protected abstract bool IsDesignContentEqual(AUnitDesign previousDesign, AUnitDesign newDesign);

    protected abstract void ObsoleteDesign(string designName);

    /// <summary>
    /// Changes _selectedDesignIcon to newSelectedDesignIcon, destroying any icon already assigned
    /// to _selectedDesignIcon if that icon is not present in the list of registered design icons.
    /// Also shows or clears the SelectedItemHudWindow depending on the value of newSelectedDesignIcon
    /// and manages the IsSelected status of the icons.
    /// <remarks>Handled this way to make sure any partially created icon and design that has not
    /// yet been applied is properly destroyed.</remarks>
    /// </summary>
    /// <param name="newChosenDesignIcon">The new chosen design icon.</param>
    private void ChangeChosenDesignIcon(UnitDesignIcon newChosenDesignIcon) {
        if (_chosenDesignIcon != null) {
            if (_chosenDesignIcon.gameObject != null) {    // could already be destroyed
                if (!_registeredDesignIcons.Contains(_chosenDesignIcon)) {
                    // design and icon were created but never accepted and added to list of icons and registered designs
                    Destroy(_chosenDesignIcon.gameObject);
                }
            }
            _chosenDesignIcon.IsPicked = false;
        }
        _chosenDesignIcon = newChosenDesignIcon;
        if (_chosenDesignIcon != null) {
            _chosenDesignIcon.IsPicked = true;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if this design window supports a popupList in the CreateDesign PopupWindow,
    /// false otherwise. If true, popupContent will be valid.
    /// </summary>
    /// <param name="popupTitle">The popup title.</param>
    /// <param name="popupContent">The resulting content to be placed in the popupList.</param>
    /// <returns></returns>
    protected abstract bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent);

    /// <summary>
    /// Shows the designer UI and populates it with the Design embedded in _selectedDesignIcon, prepared to be edited.
    /// </summary>
    private void ShowDesignerUI() {
        D.AssertNotNull(_chosenDesignIcon);
        BuildAvailableEquipmentIcons();
        AUnitDesign design = _chosenDesignIcon.Design;

        InstallEquipmentStorageIconsFor(design);
        _designerUITitleLabel.text = DesignerUITitleRoot + Constants.Colon + Constants.Space + design.DesignName;
        _designerUIContainerWidget.alpha = Constants.OneF;

        Vector3 modelDimensions;
        GameObject modelPrefab;
        if (TryGet3DModelFor(design, out modelDimensions, out modelPrefab)) {
            Show3DModelStage(modelDimensions, modelPrefab);
        }
    }

    /// <summary>
    /// Returns <c>true</c> if this design window supports displaying a 3DModel for the provided design in the designer, false otherwise.
    /// If true, the dimensions of the model along with the prefab of the model will be valid.
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="modelDimensions">The resulting model dimensions.</param>
    /// <param name="modelPrefab">The resulting model prefab.</param>
    /// <returns></returns>
    protected abstract bool TryGet3DModelFor(AUnitDesign design, out Vector3 modelDimensions, out GameObject modelPrefab);

    private void ShowDesignsUI() {
        _designsUIContainerWidget.alpha = Constants.OneF;
    }

    private void Show3DModelStage(Vector3 modelDimensions, GameObject modelPrefabGo) {
        D.AssertNull(_threeDModelStage);
        _threeDModelStage = NGUITools.AddChild(Gui3DStageFolder.Instance.Folder.gameObject, _threeDModelStagePrefab);
        D.AssertEqual(Layers.Default, (Layers)_threeDModelStage.layer);

        Camera stageCamera = _threeDModelStage.GetSingleComponentInImmediateChildren<Camera>();

        Vector3 viewportPosition = default(Vector3);
        float modelRadius = modelDimensions.magnitude / 2F; // hull radius range = .03 -> .35
        if (modelRadius > 0.3F) {
            viewportPosition = _3DCameraViewportPosition_Farthest;
        }
        else if (modelRadius > 0.16F) {
            viewportPosition = _3DCameraViewportPosition_Far;
        }
        else if (modelRadius > 0.09F) {
            viewportPosition = _3DCameraViewportPosition_Average;
        }
        else if (modelRadius > 0.05F) {
            viewportPosition = _3DCameraViewportPosition_Close;
        }
        else {
            viewportPosition = _3DCameraViewportPosition_Closest;
        }

        Vector3 modelDesiredPosition = stageCamera.ViewportToWorldPoint(viewportPosition);

        Revolver modelRevolver = _threeDModelStage.GetSingleComponentInImmediateChildren<Revolver>();
        modelRevolver.transform.position = modelDesiredPosition;
        modelRevolver.enabled = true;
        GameObject intendedModelParentGo = modelRevolver.gameObject;

        GameObject modelGo = NGUITools.AddChild(intendedModelParentGo, modelPrefabGo);

        modelGo.transform.localEulerAngles = _threeDModelRotationAngle;
        var modelCollider = modelGo.AddComponent<SphereCollider>();
        modelCollider.radius = modelRadius;
        modelGo.AddComponent<SpinWithMouse>();   // IMPROVE currently an NGUI example script

        //var modelMeshRenderers = modelGo.GetComponentsInChildren<MeshRenderer>();
        //D.Log("{0} found {1} MeshRenderers: {2}.", DebugName, modelMeshRenderers.Count(), modelMeshRenderers.Select(r => r.name).Concatenate());
        MeshRenderer modelMeshRenderer = modelGo.GetSingleComponentInImmediateChildren<MeshRenderer>();
        var modelMeshGo = modelMeshRenderer.gameObject;
        var modelCameraLosChgdComponents = modelMeshGo.GetComponentsInChildren<CameraLosChangedListener>();
        if (modelCameraLosChgdComponents.Any()) {
            modelCameraLosChgdComponents.ForAll(losChgComp => Destroy(losChgComp));   // not wanted for this display application
        }
        modelMeshGo.layer = (int)Layers.Default;

        D.Assert(!modelMeshRenderer.enabled);
        modelMeshRenderer.enabled = true;

        _3DModelStageUIContainerWidget.alpha = Constants.OneF;
    }

    private void ShowWindowControlsUI() {
        _windowControlsUIContainerWidget.alpha = Constants.OneF;
    }

    private void HideDesignerUI() {
        _designerUIContainerWidget.alpha = Constants.ZeroF;
        _designerUITitleLabel.text = DesignerUITitleRoot;
        RemoveAvailableEquipmentIcons();
        RemoveEquipmentStorageIcons();
        MyCustomCursor.Clear();
        Hide3DModelStage();
    }

    private void HideDesignsUI() {
        _designsUIContainerWidget.alpha = Constants.ZeroF;
    }

    private void Hide3DModelStage() {
        if (_threeDModelStage != null) {
            Destroy(_threeDModelStage);
            _threeDModelStage = null;
        }
        _3DModelStageUIContainerWidget.alpha = Constants.ZeroF;
    }

    private void HideWindowControlsUI() {
        _windowControlsUIContainerWidget.alpha = Constants.ZeroF;
    }

    private void UpdateDesign(UnitDesignIcon previousDesignIcon, AUnitDesign newDesign) {
        D.AssertEqual(_chosenDesignIcon, previousDesignIcon);
        D.AssertEqual(AUnitDesign.SourceAndStatus.Player_Current, newDesign.Status);

        // handle the previous design and its icon
        AUnitDesign previousDesign = previousDesignIcon.Design;
        // capture previousDesignStatus before it is potentially changed to obsolete
        AUnitDesign.SourceAndStatus previousDesignStatus = previousDesign.Status;
        if (previousDesignStatus == AUnitDesign.SourceAndStatus.Player_Current) {
            // current design that has just been updated to newDesign so obsolete previousDesign
            ObsoleteDesign(previousDesign.DesignName);
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
        AddToPlayerDesigns(newDesign);
        var newDesignIcon = CreateIcon(newDesign, _designIconSize, _registeredDesignIconsGrid.gameObject);
        AddIcon(newDesignIcon);

        // clear any prior selection
        ChangeChosenDesignIcon(null);
        _registeredDesignIconsGrid.repositionNow = true;
    }

    /// <summary>
    /// Adds and registers newDesign to the appropriate type of User designs in PlayerDesigns.
    /// </summary>
    /// <param name="newDesign">The new design.</param>
    protected abstract void AddToPlayerDesigns(AUnitDesign newDesign);

    /// <summary>
    /// Build the collection of icons that represent User registered designs.
    /// </summary>
    private void BuildRegisteredDesignIcons() {
        D.AssertEqual(Constants.Zero, _registeredDesignIcons.Count);
        RemoveRegisteredDesignIcons();   // OPTIMIZE Reqd to destroy the icon already present. Can be removed once reuse of icons is implemented

        IEnumerable<AUnitDesign> designs = GetRegisteredUserDesigns(_includeObsoleteDesigns);
        int desiredDesignsToAccommodateInGrid = designs.Count();

        Vector2 gridContainerViewSize = _registeredDesignIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerViewSize.x, (int)gridContainerViewSize.y);
        int gridColumns, unusedGridRows;
        AMultiSizeGuiIcon.IconSize iconSize = AMultiSizeGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredDesignsToAccommodateInGrid, _designIconPrefab, out unusedGridRows, out gridColumns);
        _designIconSize = iconSize;

        // configure grid for icon size
        IntVector2 iconDimensions = _designIconPrefab.GetIconDimensions(iconSize);
        _registeredDesignIconsGrid.cellHeight = iconDimensions.y;
        _registeredDesignIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        _registeredDesignIconsGrid.maxPerLine = gridColumns;

        designs.ForAll(design => {
            var designIcon = CreateIcon(design, iconSize, _registeredDesignIconsGrid.gameObject);
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
        int desiredStatsToAccommodateInGrid = availableEquipStats.Count();

        Vector2 gridContainerViewSize = _designerEquipmentIconsGrid.GetComponentInParent<UIPanel>().GetViewSize();
        IntVector2 gridContainerDimensions = new IntVector2((int)gridContainerViewSize.x, (int)gridContainerViewSize.y);

        int gridColumns, unusedGridRows;
        AMultiSizeGuiIcon.IconSize iconSize = AMultiSizeGuiIcon.DetermineGridIconSize(gridContainerDimensions, desiredStatsToAccommodateInGrid, _equipmentIconPrefab, out unusedGridRows, out gridColumns);

        // configure grid for icon size
        IntVector2 iconDimensions = _equipmentIconPrefab.GetIconDimensions(iconSize);
        _designerEquipmentIconsGrid.cellHeight = iconDimensions.y;
        _designerEquipmentIconsGrid.cellWidth = iconDimensions.x;

        // make grid gridColumns wide
        _designerEquipmentIconsGrid.maxPerLine = gridColumns;

        availableEquipStats.ForAll(eStat => CreateAndAddIcon(eStat, iconSize));
        _designerEquipmentIconsGrid.repositionNow = true;
    }

    /// <summary>
    /// Creates and returns a new design instance copied from design.
    /// <remarks>The content of the returned design will be identical to the content of design, including the RootDesignName 
    /// and SourceAndStatus setting. Clients will need to change these values if this is not what is desired.</remarks>
    /// </summary>
    /// <param name="design">The design.</param>
    /// <returns></returns>
    protected abstract AUnitDesign CopyDesignFrom(AUnitDesign design);

    /// <summary>
    /// Returns an empty template design using the provided designNameHint which
    /// can be null if no hint is necessary.
    /// </summary>
    /// <param name="designNameHint">A hint as to the name of the design. Can be null if there is no hint.</param>
    /// <returns></returns>
    protected abstract AUnitDesign GetEmptyTemplateDesign(string designNameHint);

    /// <summary>
    /// Creates the equipment storage representation of the provided design using EquipmentStorageIcons.
    /// </summary>
    /// <param name="design">The design.</param>
    private void InstallEquipmentStorageIconsFor(AUnitDesign design) {
        AUnitDesign workingDesign = CopyDesignFrom(design);
        _designerEquipmentStorage.InstallEquipmentStorageIconsFor(workingDesign);
    }

    private void RemoveRegisteredDesignIcons() {
        if (!_registeredDesignIcons.Any()) {
            // Could be newly Awakened in which case it will have an icon present for debug inspection
            IList<Transform> iconTransforms = _registeredDesignIconsGrid.GetChildList();
            if (iconTransforms.Any()) {
                D.AssertEqual(Constants.One, iconTransforms.Count);
                foreach (var it in iconTransforms) {
                    var icon = it.GetComponent<UnitDesignIcon>();
                    RemoveIcon(icon);
                }
            }
        }
        else {
            var iconsCopy = new List<UnitDesignIcon>(_registeredDesignIcons);
            iconsCopy.ForAll(i => {
                RemoveIcon(i);
            });
        }
    }

    private void RemoveAvailableEquipmentIcons() {
        IList<Transform> iconTransforms = _designerEquipmentIconsGrid.GetChildList();
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
    /// Creates a UnitDesignImageIcon from the provided design and parents it to parent.
    /// <remarks>Handled this way to avoid adding the icon generated by CreateDesign to the list of
    /// available designs. This icon is not an available design until the design is accepted by pressing
    /// the ApplyDesign button in the DesignerUI.</remarks>
    /// </summary>
    /// <param name="design">The design.</param>
    /// <param name="iconSize">Size of the icon to create.</param>
    /// <param name="parent">The parent.</param>
    /// <returns></returns>
    private UnitDesignIcon CreateIcon(AUnitDesign design, AMultiSizeGuiIcon.IconSize iconSize, GameObject parent) {
        GameObject designIconGo = NGUITools.AddChild(parent, _designIconPrefab.gameObject);
        designIconGo.name = design.DesignName + DesignIconExtension;
        UnitDesignIcon designIcon = designIconGo.GetSafeComponent<UnitDesignIcon>(); ;
        designIcon.Size = iconSize;
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
    private void AddIcon(UnitDesignIcon designIcon) {
        if (designIcon.transform.parent != _registeredDesignIconsGrid.transform) {
            UnityUtility.AttachChildToParent(designIcon.gameObject, _registeredDesignIconsGrid.gameObject);
        }
        var eventListener = UIEventListener.Get(designIcon.gameObject);
        eventListener.onDoubleClick += DesignIconDoubleClickedEventHandler;
        eventListener.onClick += DesignIconClickedEventHandler;

        _registeredDesignIcons.Add(designIcon);
    }

    private void CreateAndAddIcon(AEquipmentStat equipStat, AMultiSizeGuiIcon.IconSize iconSize) {
        GameObject equipIconGo = NGUITools.AddChild(_designerEquipmentIconsGrid.gameObject, _equipmentIconPrefab.gameObject);
        equipIconGo.name = equipStat.Name + EquipmentIconExtension;
        EquipmentIcon equipIcon = equipIconGo.GetSafeComponent<EquipmentIcon>();
        equipIcon.Size = iconSize;
        equipIcon.EquipmentStat = equipStat;

        UIEventListener.Get(equipIconGo).onDoubleClick += EquipmentIconDoubleClickedEventHandler;
    }

    private void RemoveIcon(UnitDesignIcon designIcon) {
        var eventListener = UIEventListener.Get(designIcon.gameObject);
        eventListener.onDoubleClick -= DesignIconDoubleClickedEventHandler;
        eventListener.onClick -= DesignIconClickedEventHandler;

        _registeredDesignIcons.Remove(designIcon);
        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(designIcon.gameObject);
    }

    private void RemoveIcon(EquipmentIcon equipIcon) {
        //D.Log("{0} is removing {1} from DesignerUI.", DebugName, equipIcon.DebugName);
        UIEventListener.Get(equipIcon.gameObject).onDoubleClick -= EquipmentIconDoubleClickedEventHandler;

        // Note: DestroyImmediate() because Destroy() doesn't always get rid of the existing icon before Reposition occurs on LateUpdate
        // This results in an extra 'empty' icon that stays until another Reposition() call, usually from sorting something
        DestroyImmediate(equipIcon.gameObject);
    }

    /// <summary>
    /// Returns all designs registered to the user, including obsolete designs if so indicated.
    /// </summary>
    /// <param name="includeObsolete">if set to <c>true</c> [include obsolete].</param>
    /// <returns></returns>
    protected abstract IEnumerable<AUnitDesign> GetRegisteredUserDesigns(bool includeObsolete);

    /// <summary>
    /// Returns all the available AEquipmentStats supported by this kind of design.
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats();

    protected override void ResetForReuse() {
        RemoveRegisteredDesignIcons();
        ChangeChosenDesignIcon(null);
        HideDesignerUI();
        D.AssertEqual(Constants.Zero, _registeredDesignIcons.Count);
    }

    public override void ActivateContent() {
        base.ActivateContent();
        _createDesignPopupWindow.ActivateContent();
        // Activate in case previous window was CmdWindow that didn't use it
        _createDesignPopupListContainer.gameObject.SetActive(true);
        _renameObsoleteDesignPopupWindow.ActivateContent();
    }

    protected override void Unsubscribe() {
        base.Unsubscribe();
        EventDelegate.Remove(onShowBegin, ShowBeginEventHandler);
    }

    #region Debug


    #endregion

}

