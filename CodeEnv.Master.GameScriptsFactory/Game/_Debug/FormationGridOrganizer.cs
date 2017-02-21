// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationGridOrganizer.cs
// Debug class for use in edit mode (via ContextMenu.Execute) that automates the positioning and SlotID 
// of existing placeholders within a FormationGrid according to the index setting in <c>ElementIndexer</c>. 
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
using GridFramework.Grids;
using GridFramework.Renderers.Rectangular;
using UnityEngine;

/// <summary>
/// Debug class for use in edit mode (via ContextMenu.Execute) that automates the positioning and SlotID 
/// of existing placeholders within a FormationGrid according to the index setting in <c>ElementIndexer</c>. 
/// <remarks>The desired number of layers, rows and placeholders (under each row) must be present along with a ElementIndexer
/// with its index value set for each layer, row and placeholder.</remarks>
/// <remarks>It is safe to leave on a formation grid as it can only be activated from the ContextMenu.</remarks>
/// </summary>
public class FormationGridOrganizer : AMonoBase {

    private const string SlotIdNameFormat = "Slot_{0}_{1}_{2}";

    public Vector3 cellSize = Vector3.zero;

    public Vector3 gridDimensions = Vector3.zero;

    private float _initialValue;
    private float _incrementValue;

    [ContextMenu("Organize")]
    private void Organize() {
        D.Log("{0}.Organize() called.", GetType().Name);
        ValidateValueSettings();
        InitializeLocalReferences();
        InitializeGrid();
        PositionGridElements();
    }

    // 2.14.17 Not currently used. Good approach but already handled by EditModeController.EnableRenderers
    [System.Obsolete]
    ////[ContextMenu("ToggleShowMesh")]
    private void ToggleShowMesh() {
        var placeholderMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        bool areRenderersEnabled = placeholderMeshRenderers.First().enabled;
        D.Assert(placeholderMeshRenderers.All(pmr => pmr.enabled == areRenderersEnabled));
        placeholderMeshRenderers.ForAll(pmr => pmr.enabled = !areRenderersEnabled);
    }

    private void ValidateValueSettings() {
        D.Assert(cellSize != default(Vector3));
        D.AssertApproxEqual(cellSize.x, cellSize.y);
        D.AssertApproxEqual(cellSize.y, cellSize.z);
        D.Assert(gridDimensions != default(Vector3));
        D.Assert(gridDimensions.x > Constants.ZeroF && gridDimensions.y > Constants.ZeroF && gridDimensions.z > Constants.ZeroF);
        ValidateAsInteger(gridDimensions.x);
        ValidateAsInteger(gridDimensions.y);
        ValidateAsInteger(gridDimensions.z);
    }

    private void InitializeLocalReferences() {
        // all cellSize values are the same
        _initialValue = cellSize.x / 2F;
        _incrementValue = cellSize.x;
    }

    private void InitializeGrid() {
        var grid = GetComponent<RectGrid>();
        grid.Spacing = cellSize;
        var gridRenderer = GetComponent<Parallelepiped>();
        // In version 2 RectGrid size is always relative //_grid.relativeSize = true;
        gridRenderer.From = Vector3.zero;
        gridRenderer.To = gridDimensions;
    }

    private void PositionGridElements() {
        var layerElements = gameObject.GetSafeComponentsInImmediateChildren<ElementIndexer>();
        ValidateElements(layerElements);
        ElementIndexer[] orderedLayerElements = layerElements.OrderBy(le => le.index).ToArray();
        for (int i = 0; i < orderedLayerElements.Length; i++) {
            var layerTransform = orderedLayerElements[i].transform;
            layerTransform.SetLocalPositionY(_initialValue + i * _incrementValue);

            var rowElements = layerTransform.gameObject.GetSafeComponentsInImmediateChildren<ElementIndexer>();
            ValidateElements(rowElements);
            ElementIndexer[] orderedRowElements = rowElements.OrderBy(re => re.index).ToArray();
            for (int j = 0; j < orderedRowElements.Length; j++) {
                var rowTransform = orderedRowElements[j].transform;
                rowTransform.SetLocalPositionZ(_initialValue + j * _incrementValue);

                var placeholderElements = rowTransform.gameObject.GetComponentsInImmediateChildren<ElementIndexer>();
                ValidateElements(placeholderElements);
                ElementIndexer[] orderedPlaceholderElements = placeholderElements.OrderBy(pe => pe.index).ToArray();
                if (orderedPlaceholderElements.Any()) {
                    var orderedIndexes = orderedPlaceholderElements.Select(pe => pe.index).ToArray();
                    for (int k = 0; k < orderedPlaceholderElements.Length; k++) {
                        var placeholder = orderedPlaceholderElements[k].GetComponent<FormationStationPlaceholder>();
                        int index = orderedIndexes[k];
                        placeholder.transform.SetLocalPositionX(_initialValue + index * _incrementValue);
                        string slotIdName = SlotIdNameFormat.Inject(i, j, index);
                        FormationStationSlotID slotID = Enums<FormationStationSlotID>.Parse(slotIdName);
                        placeholder.slotID = slotID;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates the elements.
    /// </summary>
    /// <param name="rowElements">The row elements.</param>
    /// <returns></returns>
    private void ValidateElements(ElementIndexer[] rowElements) {
        var indexesFound = new List<int>(rowElements.Length);
        rowElements.ForAll(e => {
            int index = e.index;
            D.AssertNotEqual(Constants.MinusOne, index, "{0}.{1} not set.".Inject(e.gameObject.name, typeof(ElementIndexer).Name));
            if (indexesFound.Contains(index)) {
                D.Warn("Duplicate {0} index {1} found. Order will not be deterministic.", typeof(ElementIndexer).Name, index);
            }
            indexesFound.Add(index);
        });
    }

    private void ValidateAsInteger(float value) {
        D.Assert(Mathfx.Approx((value * 10F) % 10F, Constants.ZeroF, 0.01F));
    }

    [System.Obsolete]
    private void DisableAllPlaceholderRenderers() {
        var placeholderMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        placeholderMeshRenderers.ForAll(pmr => pmr.enabled = false);
    }

    protected override void Cleanup() {
        ////DisableAllPlaceholderRenderers();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

