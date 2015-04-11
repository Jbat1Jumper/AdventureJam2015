using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(RagePixelText))]
public class RagePixelTextEditor : Editor
{
    private float handleSize = 0.01f;

    private bool justSelected = false;
    private bool paintUndoSaved = false;

    private int defaultSceneButtonWidth = 32;
    private int defaultSceneButtonHeight = 32;
    private bool atlasTextureIsDirty = false;

    private Color32[] colorReplaceBuffer;
    private RagePixelTexelRect selection;
    private bool selectionActive = false;

    private RagePixelTexel selectionStart;
    private RagePixelTexel frontBufferPosition;
    private RagePixelTexel frontBufferDragStartPosition;
    private RagePixelTexel frontBufferDragStartMousePosition;

    private RagePixelBitmap backBuffer;
    private RagePixelBitmap frontBuffer;

    public enum Mode { Default = 0, Pen, Fill, Scale, Resize, Select };
    public Mode mode = Mode.Default;

    public Vector2 rectangleStart;

    protected bool animationStripEnabled = true;

    private Camera sceneCamera
    {
        get
        {
            if (SceneView.lastActiveSceneView != null)
            {
                return SceneView.lastActiveSceneView.camera;
            }
            else
            {
                return null;
            }

        }
    }

    private int scenePixelWidth
    {
        get
        {
            if (sceneCamera != null)
            {
                return (int)sceneCamera.pixelWidth;
            }
            else
            {
                return (int)Screen.width;
            }
        }
    }

    private int scenePixelHeight
    {
        get
        {
            if (sceneCamera != null)
            {
                return (int)sceneCamera.pixelHeight;
            }
            else
            {
                return (int)Screen.height;
            }
        }
    }

    private MeshRenderer _meshRenderer;
    public MeshRenderer meshRenderer
    {
        get
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = ragePixelText.GetComponent<MeshRenderer>();
            }
            return _meshRenderer;
        }
    }

    private MeshFilter _meshFilter;
    public MeshFilter meshFilter
    {
        get
        {
            if (_meshFilter == null)
            {
                _meshFilter = ragePixelText.GetComponent<MeshFilter>();
            }
            return _meshFilter;
        }
    }

    private Texture2D _spritesheetTexture;
    public Texture2D spritesheetTexture
    {
        get
        {
            if (_spritesheetTexture == null)
            {
                _spritesheetTexture = ragePixelText.spriteSheet.atlas.GetTexture("_MainTex") as Texture2D;
            }
            return _spritesheetTexture;
        }
    }

    private RagePixelText _ragePixelText;
    public RagePixelText ragePixelText
    {
        get
        {
            if (_ragePixelText == null)
            {
                _ragePixelText = target as RagePixelText;
            }
            return _ragePixelText;
        }
    }

    private RagePixelAnimStripGUI _animStripGUI;
    public RagePixelAnimStripGUI animStripGUI
    {
        get
        {
            if (_animStripGUI == null)
            {
                _animStripGUI = new RagePixelAnimStripGUI(spriteSheetGUI);
                _animStripGUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
                _animStripGUI.currentCellKey = ragePixelText.currentCellKey;
                _animStripGUI.positionX = defaultSceneButtonWidth + 10;
                _animStripGUI.positionY = scenePixelHeight - _animStripGUI.thumbnailSize - 7;
                _animStripGUI.maxWidth = scenePixelWidth - defaultSceneButtonWidth * 3 - 20;
            }
            return _animStripGUI;
        }
    }

    private RagePixelSpriteSheetGUI _spriteSheetGUI;
    public RagePixelSpriteSheetGUI spriteSheetGUI
    {
        get
        {
            if (_spriteSheetGUI == null)
            {
                _spriteSheetGUI = new RagePixelSpriteSheetGUI();
                spriteSheetGUI.currentRowKey = ragePixelText.currentRowKey;
                spriteSheetGUI.spriteSheet = ragePixelText.spriteSheet;
            }
            return _spriteSheetGUI;
        }
    }

    private RagePixelColorPickerGUI _paintColorPickerGUI;
    public RagePixelColorPickerGUI paintColorPickerGUI
    {
        get
        {
            if (_paintColorPickerGUI == null)
            {
                _paintColorPickerGUI = new RagePixelColorPickerGUI();
                _paintColorPickerGUI.gizmoVisible = false;
                _paintColorPickerGUI.visible = false;
                _paintColorPickerGUI.gizmoPositionX = 5;
                _paintColorPickerGUI.gizmoPositionY = 5;
                _paintColorPickerGUI.positionX = _paintColorPickerGUI.gizmoPositionX + _paintColorPickerGUI.gizmoPixelWidth;
                _paintColorPickerGUI.positionY = _paintColorPickerGUI.gizmoPositionY;
            }
            return _paintColorPickerGUI;
        }
    }
    private RagePixelColorPickerGUI _replaceColorPickerGUI;
    public RagePixelColorPickerGUI replaceColorPickerGUI
    {
        get
        {
            if (_replaceColorPickerGUI == null)
            {
                _replaceColorPickerGUI = new RagePixelColorPickerGUI();
                _replaceColorPickerGUI.gizmoVisible = false;
                _replaceColorPickerGUI.visible = false;
                _replaceColorPickerGUI.gizmoPositionX = (int)paintColorPickerGUI.gizmoBounds.xMax + (int)defaultSceneButtonWidth;
                _replaceColorPickerGUI.gizmoPositionY = 5;
                _replaceColorPickerGUI.gizmoPixelWidth = (int)defaultSceneButtonWidth;
                _replaceColorPickerGUI.gizmoPixelHeight = (int)defaultSceneButtonWidth;
                _replaceColorPickerGUI.positionX = _replaceColorPickerGUI.gizmoPositionX + (int)defaultSceneButtonWidth;
                _replaceColorPickerGUI.positionY = _replaceColorPickerGUI.gizmoPositionY;
            }
            return _replaceColorPickerGUI;
        }
    }

    private bool showGrid9Gizmo = false;

    public void OnDestroy()
    {
        spriteSheetGUI.CleanExit();
        animStripGUI.CleanExit();
        paintColorPickerGUI.CleanExit();
        replaceColorPickerGUI.CleanExit();
    }

    public override void OnInspectorGUI()
    {
        if (ragePixelText.spriteSheet == null)
        {
            ragePixelText.spriteSheet = (RagePixelSpriteSheet)EditorGUILayout.ObjectField("Sprite Sheet", ragePixelText.spriteSheet, typeof(RagePixelSpriteSheet), false, null);
            if (ragePixelText.spriteSheet != null)
            {
                if (ragePixelText.currentRowKey == 0 || ragePixelText.currentCellKey == 0)
                {
                    ragePixelText.currentRowKey = ragePixelText.spriteSheet.rows[0].key;
                    ragePixelText.currentCellKey = ragePixelText.spriteSheet.rows[0].cells[0].key;
                }
                _spritesheetTexture = null;
                ragePixelText.meshIsDirty = true;
                ragePixelText.refreshMesh();
            }
        }
        else if (!Application.isPlaying)
        {
            int spriteSheetGUIMargin = 7;

            RagePixelSpriteSheet oldSheet = ragePixelText.spriteSheet;
            ragePixelText.spriteSheet =
                (RagePixelSpriteSheet)EditorGUILayout.ObjectField(
                    "Sprite sheet",
                    ragePixelText.spriteSheet,
                    typeof(RagePixelSpriteSheet),
                    false
                    );

            if (ragePixelText.spriteSheet != null)
            {

                if (oldSheet != ragePixelText.spriteSheet)
                {
                    if (ragePixelText.currentRowKey == 0 || ragePixelText.currentCellKey == 0)
                    {
                        ragePixelText.currentRowKey = ragePixelText.spriteSheet.rows[0].key;
                        ragePixelText.currentCellKey = ragePixelText.spriteSheet.rows[0].cells[0].key;
                    }
                    _spritesheetTexture = null;
                    ragePixelText.meshIsDirty = true;
                    ragePixelText.refreshMesh();
                }

                spriteSheetGUI.maxWidth = Screen.width - spriteSheetGUIMargin;
                spriteSheetGUI.spriteSheet = ragePixelText.spriteSheet;
                spriteSheetGUI.currentRowKey = ragePixelText.currentRowKey;
                animStripGUI.currentCellKey = ragePixelText.currentCellKey;

                Color oldColor = ragePixelText.tintColor;
                ragePixelText.tintColor = EditorGUILayout.ColorField("Tint Color", ragePixelText.tintColor);

                if (ragePixelText.tintColor != oldColor)
                {
                    ragePixelText.vertexColorsAreDirty = true;
                    ragePixelText.refreshMesh(false);
                }

                int oldKerning = ragePixelText.textKerning;
                ragePixelText.textKerning = EditorGUILayout.IntField("Padding", ragePixelText.textKerning);
                if (oldKerning != ragePixelText.textKerning)
                {
                    ragePixelText.meshIsDirty = true;
                    ragePixelText.refreshMesh(false);
                }

                int oldLeading = ragePixelText.textLeading;
                ragePixelText.textLeading = EditorGUILayout.IntField("Row height", ragePixelText.textLeading);
                if (oldLeading != ragePixelText.textLeading)
                {
                    ragePixelText.meshIsDirty = true;
                    ragePixelText.refreshMesh(false);
                }

                int oldSpaceWidth = ragePixelText.textSpaceWidth;
                ragePixelText.textSpaceWidth = EditorGUILayout.IntField("Space width", ragePixelText.textSpaceWidth);
                if (oldSpaceWidth != ragePixelText.textSpaceWidth)
                {
                    ragePixelText.meshIsDirty = true;
                    ragePixelText.refreshMesh(false);
                }

                GUILayout.Space(10f);

                string oldText = ragePixelText.text;
                ragePixelText.text = EditorGUILayout.TextArea(ragePixelText.text, GUILayout.Height(100f));
                if (!oldText.Equals(ragePixelText.text))
                {
                    ragePixelText.meshIsDirty = true;
                    ragePixelText.refreshMesh(false);
                }
                GUILayout.Space(10f);
            }
        }
        else
        {
            GUILayout.Space(5f);
            EditorUtility.SetSelectedWireframeHidden(meshRenderer, true);
            GUILayout.Label("Inspector GUI disabled in play mode");
            GUILayout.Space(5f);
        }
 
    }

    public void OnSceneGUI()
    {
        InvokeOnSelectedEvent();

        if (!Application.isPlaying)
        {
            if (ragePixelText.spriteSheet != null)
			{

				// Lets snap
				if (Event.current.type == EventType.mouseUp)
				{
					ragePixelText.SnapToIntegerPosition();
				}

				if (ragePixelText.editMode)//ragePixelText.editMode)
				{
                    SceneGUIInit();
                    HandleCameraWarnings();

                    bool colorReplaced = false;

                    // GUI
                    Handles.BeginGUI();
                    HandleColorPickerGUI();
                    HandlePaintGUI();
                    HandleAnimationGUI();
                    DrawGizmos();
                    Handles.EndGUI();

                    HandleKeyboard();

                    switch (mode)
                    {
                        case (Mode.Default):
                            HandleModeDefault();
                            break;
                        case (Mode.Pen):
                            HandleModePen();
                            break;
                        case (Mode.Fill):
                            HandleModeFill();
                            break;
                        case (Mode.Select):
                            HandleModeSelect();
                            break;
                        case (Mode.Resize):
                            HandleModeResize();
                            break;
                    }

                    if (Event.current.type == EventType.mouseUp)
                    {
                        paintUndoSaved = false;
                    }

                    if (atlasTextureIsDirty && Event.current.type == EventType.mouseUp || colorReplaced)
                    {
                        saveTexture();
                        animStripGUI.isDirty = true;
                        spriteSheetGUI.isDirty = true;
                        atlasTextureIsDirty = false;
                    }
                }
            }
        }
        else
        {
            DrawSpriteBoundsGizmo();
        }
    }

    public void HandleModeDefault()
    {

    }

    public void HandleModePen()
    {
        if (Event.current.type == EventType.mouseDown || Event.current.type == EventType.mouseDrag)
        {
            Vector3 mouseWorldPosition = sceneScreenToWorldPoint(Event.current.mousePosition);

            RagePixelTexel texel = WorldToTexelCoords(spritesheetTexture, ragePixelText.transform, mouseWorldPosition);

            if (texel.X >= 0 && texel.X < ragePixelText.GetCurrentRow().pixelSizeX &&
                texel.Y >= 0 && texel.Y < ragePixelText.GetCurrentRow().pixelSizeY)
            {
                Rect uv = ragePixelText.GetCurrentCell().uv;
                int minX = Mathf.FloorToInt(spritesheetTexture.width * uv.xMin);
                int minY = Mathf.FloorToInt(spritesheetTexture.height * uv.yMin);

                if (Event.current.button == 0)
                {
                    SavePaintUndo();

                    atlasTextureIsDirty = true;
                    spritesheetTexture.SetPixel(
                        minX + texel.X,
                        minY + texel.Y,
                        paintColorPickerGUI.selectedColor);

                    spritesheetTexture.Apply();
                    paintColorPickerGUI.selectedColor = spritesheetTexture.GetPixel(
                        minX + texel.X,
                        minY + texel.Y);
                }
                else
                {
                    paintColorPickerGUI.selectedColor = spritesheetTexture.GetPixel(
                        minX + texel.X,
                        minY + texel.Y);
                    replaceColorPickerGUI.selectedColor = paintColorPickerGUI.selectedColor;
                }
            }
            else
            {
                if (Event.current.button == 1)
                {
                    paintColorPickerGUI.selectedColor = new Color(0f, 0f, 0f, 0f);
                    replaceColorPickerGUI.selectedColor = paintColorPickerGUI.selectedColor;
                }
            }

            Event.current.Use();
        }
    }

    public void HandleModeFill()
    {
        if (Event.current.type == EventType.mouseDown)
        {
            Vector3 mouseWorldPosition = sceneScreenToWorldPoint(Event.current.mousePosition);

            RagePixelTexel texel = WorldToTexelCoords(spritesheetTexture, ragePixelText.transform, mouseWorldPosition);

            Rect uv = ragePixelText.GetCurrentCell().uv;
            int minX = Mathf.FloorToInt(spritesheetTexture.width * uv.xMin);
            int minY = Mathf.FloorToInt(spritesheetTexture.height * uv.yMin);
            int maxX = Mathf.FloorToInt(spritesheetTexture.width * uv.xMax);
            int maxY = Mathf.FloorToInt(spritesheetTexture.height * uv.yMax);

            if (texel.X >= 0 && texel.X < ragePixelText.GetCurrentRow().pixelSizeX &&
                texel.Y >= 0 && texel.Y < ragePixelText.GetCurrentRow().pixelSizeY)
            {
                if (Event.current.button == 0)
                {
                    Color fillTargetColor = spritesheetTexture.GetPixel(minX + texel.X, minY + texel.Y);

                    if (!fillTargetColor.Equals(paintColorPickerGUI.selectedColor))
                    {
                        SavePaintUndo();
                        atlasTextureIsDirty = true;
                        FloodFill(
                            fillTargetColor,
                            paintColorPickerGUI.selectedColor,
                            spritesheetTexture,
                            minX + texel.X,
                            minY + texel.Y,
                            minX,
                            minY,
                            maxX,
                            maxY);
                        spritesheetTexture.Apply();
                        paintColorPickerGUI.selectedColor = spritesheetTexture.GetPixel(
                        minX + texel.X,
                        minY + texel.Y);
                    }
                }
                else
                {
                    paintColorPickerGUI.selectedColor = spritesheetTexture.GetPixel(
                        minX + texel.X,
                        minY + texel.Y);
                    replaceColorPickerGUI.selectedColor = paintColorPickerGUI.selectedColor;
                }
            }
            else
            {
                if (Event.current.button == 1)
                {
                    paintColorPickerGUI.selectedColor = new Color(0f, 0f, 0f, 0f);
                    replaceColorPickerGUI.selectedColor = paintColorPickerGUI.selectedColor;
                }
            }
            Event.current.Use();
        }
    }

    public void HandleModeSelect()
    {
        if (Event.current.type == EventType.mouseDown || Event.current.type == EventType.mouseDrag || Event.current.type == EventType.mouseUp)
        {
            Vector3 mouseWorldPosition = sceneScreenToWorldPoint(Event.current.mousePosition);

            RagePixelTexel texel = WorldToTexelCoords(spritesheetTexture, ragePixelText.transform, mouseWorldPosition);
            int spriteWidth = ragePixelText.GetCurrentRow().pixelSizeX;
            int spriteHeight = ragePixelText.GetCurrentRow().pixelSizeY;

            if (texel.X >= 0 && texel.X < spriteWidth &&
                texel.Y >= 0 && texel.Y < spriteHeight)
            {
                if (Event.current.type == EventType.mouseDown)
                {
                    if (Event.current.button == 0)
                    {
                        if (selection == null)
                        {
                            selectionActive = false;
                            selectionStart = texel;
                            selection = new RagePixelTexelRect(texel.X, texel.Y, texel.X, texel.Y);
                        }
                        else
                        {
                            if (texel.X < frontBufferPosition.X || texel.Y < frontBufferPosition.Y || texel.X > frontBufferPosition.X + selection.Width() || texel.Y > frontBufferPosition.Y + selection.Height())
                            {
                                selectionActive = false;
                                selectionStart = texel;
                                selection = new RagePixelTexelRect(texel.X, texel.Y, texel.X, texel.Y);
                            }
                            else
                            {
                                frontBufferDragStartMousePosition = texel;
                                frontBufferDragStartPosition = frontBufferPosition;
                            }
                        }
                    }
                }
                if (Event.current.type == EventType.mouseDrag)
                {
                    if (selectionActive)
                    {
                        RagePixelTexel movement = new RagePixelTexel(texel.X - frontBufferDragStartMousePosition.X, texel.Y - frontBufferDragStartMousePosition.Y);
                        frontBufferPosition = new RagePixelTexel(frontBufferDragStartPosition.X + movement.X, frontBufferDragStartPosition.Y + movement.Y);

                        Rect spriteUV = ragePixelText.GetCurrentCell().uv;

                        PasteBitmapToSpritesheet(new RagePixelTexel(0, 0), spriteUV, backBuffer);
                        PasteBitmapToSpritesheetAlpha(frontBufferPosition, spriteUV, frontBuffer);

                        spritesheetTexture.Apply();
                    }
                    else
                    {
                        selection = new RagePixelTexelRect(selectionStart.X, selectionStart.Y, texel.X, texel.Y);
                    }
                }
                if (Event.current.type == EventType.mouseUp && !selectionActive)
                {
                    if (selection.Width() > 1 || selection.Height() > 1)
                    {
                        SavePaintUndo();

                        Rect spriteUV = ragePixelText.GetCurrentCell().uv;

                        frontBuffer = GrabRectFromSpritesheet(selection);
                        CutRectInSpritesheet(selection, spriteUV);
                        backBuffer = GrabSprite(spriteUV);

                        frontBufferPosition = new RagePixelTexel(selection.X, selection.Y);
                        frontBufferDragStartPosition = new RagePixelTexel(selection.X, selection.Y);

                        PasteBitmapToSpritesheetAlpha(frontBufferPosition, spriteUV, frontBuffer);

                        selectionActive = true;
                        spritesheetTexture.Apply();
                    }
                    else
                    {
                        selection = null;
                        selectionActive = false;
                    }
                }
                if (selectionActive && Event.current.type == EventType.mouseUp)
                {
                    spritesheetTexture.Apply();
                    atlasTextureIsDirty = true;
                }
            }
            else
            {
                if (Event.current.type != EventType.mouseDrag)
                {
                    mode = Mode.Default;
                }

            }
            if (Event.current.type != EventType.mouseUp)
            {
                Event.current.Use();
            }
        }
    }

    public void HandleModeResize()
    {
        bool isMouseUp = false;
        if (Event.current.type == EventType.MouseUp)
        {
            isMouseUp = true;
        }

        Vector2 newSize = Handles.FreeMoveHandle(ragePixelText.transform.position + new Vector3(ragePixelText.GetCurrentRow().newPixelSizeX, ragePixelText.GetCurrentRow().newPixelSizeY, 0f) + GetPivotOffset(), Quaternion.identity, sceneCamera.orthographicSize * handleSize, new Vector3(1f, 1f, 0f), Handles.CircleCap) - ragePixelText.transform.position - GetPivotOffset();
        newSize.x = Mathf.Max(newSize.x, 1f);
        newSize.y = Mathf.Max(newSize.y, 1f);

        ragePixelText.GetCurrentRow().newPixelSizeX = Mathf.Clamp(Mathf.RoundToInt(newSize.x), 1, 2048);
        ragePixelText.GetCurrentRow().newPixelSizeY = Mathf.Clamp(Mathf.RoundToInt(newSize.y), 1, 2048);

        if (ragePixelText.GetCurrentRow().pixelSizeX != Mathf.RoundToInt(newSize.x) || ragePixelText.GetCurrentRow().pixelSizeY != Mathf.RoundToInt(newSize.y))
        {
            if (isMouseUp)
            {
                ragePixelText.GetCurrentRow().ClearUndoHistory();

                bool isGrowing =
                    ragePixelText.GetCurrentRow().newPixelSizeX > ragePixelText.GetCurrentRow().pixelSizeX ||
                    ragePixelText.GetCurrentRow().newPixelSizeY > ragePixelText.GetCurrentRow().pixelSizeY;

                spriteSheetGUI.isDirty = true;
                animStripGUI.isDirty = true;
                ragePixelText.meshIsDirty = true;

                RagePixelUtil.RebuildAtlas(ragePixelText.spriteSheet, isGrowing, "resize");
            }
        }
    }

    public void SceneGUIInit()
	{
		Debug.Log ("SceneGUIInit");
        GUI.backgroundColor = new Color(1f, 1f, 1f, 1f);
        GUI.color = new Color(1f, 1f, 1f, 1f);

        if (!showGrid9Gizmo)
        {
            EditorUtility.SetSelectedWireframeHidden(meshRenderer, true);
        }
        else
        {
            EditorUtility.SetSelectedWireframeHidden(meshRenderer, false);
        }

        if (mode != Mode.Default || mode != Mode.Resize || animStripGUI.visible)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        if (Event.current.type == EventType.mouseUp)
        {
            ragePixelText.SnapToIntegerPosition();
        }

        Tools.pivotMode = PivotMode.Pivot;
    }

    public void HandleColorPickerGUI()
    {
        if (paintColorPickerGUI.gizmoVisible)
        {
            if (GUI.Button(paintColorPickerGUI.gizmoBounds, paintColorPickerGUI.colorGizmoTexture) && !replaceColorPickerGUI.visible)
            {
                paintColorPickerGUI.visible = !paintColorPickerGUI.visible;
            }

            float left = paintColorPickerGUI.visible ? paintColorPickerGUI.bounds.xMax : paintColorPickerGUI.gizmoBounds.xMax;

            if (GUI.Button(new Rect(left, replaceColorPickerGUI.bounds.yMin, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Replace))
            {
                replaceColorPickerGUI.visible = true;
                paintColorPickerGUI.visible = false;
                replaceColorPickerGUI.selectedColor = paintColorPickerGUI.selectedColor;
                colorReplaceBuffer = spritesheetTexture.GetPixels32();
            }

            if (paintColorPickerGUI.visible)
            {
                paintColorPickerGUI.HandleGUIEvent(Event.current);
                GUI.DrawTexture(paintColorPickerGUI.bounds, paintColorPickerGUI.colorPickerTexture);
            }
        }

        if (replaceColorPickerGUI.visible)
        {
            if (GUI.Button(replaceColorPickerGUI.gizmoBounds, replaceColorPickerGUI.colorGizmoTexture))
            {
                //noop
            }
            if (replaceColorPickerGUI.HandleGUIEvent(Event.current))
            {
                /*replaceColorPickerGUI.selectedColor =*/
                ragePixelText.spriteSheet.replaceColor(colorReplaceBuffer, paintColorPickerGUI.selectedColor, replaceColorPickerGUI.selectedColor, ragePixelText.GetCurrentRow());
            }
            if (GUI.Button(new Rect(replaceColorPickerGUI.bounds.xMax + 5, replaceColorPickerGUI.bounds.yMin, 102, 16), "Apply to sprite"))
            {
                foreach (RagePixelCell cell in ragePixelText.GetCurrentRow().cells)
                {
                    ragePixelText.spriteSheet.saveUndo(colorReplaceBuffer, cell);
                }
                paintColorPickerGUI.selectedColor = ragePixelText.spriteSheet.replaceColor(colorReplaceBuffer, paintColorPickerGUI.selectedColor, replaceColorPickerGUI.selectedColor, ragePixelText.GetCurrentRow());
                replaceColorPickerGUI.selectedColor = paintColorPickerGUI.selectedColor;
                spriteSheetGUI.isDirty = true;
                animStripGUI.isDirty = true;
                saveTexture();
                ragePixelText.refreshMesh();
                EditorUtility.SetDirty(ragePixelText);
                replaceColorPickerGUI.visible = false;
            }
            if (GUI.Button(new Rect(replaceColorPickerGUI.bounds.xMax + 5, replaceColorPickerGUI.bounds.yMin + 20, 102, 16), "Apply to atlas"))
            {
                foreach (RagePixelRow row in ragePixelText.spriteSheet.rows)
                {
                    foreach (RagePixelCell cell in row.cells)
                    {
                        ragePixelText.spriteSheet.saveUndo(colorReplaceBuffer, cell);
                    }
                }
                ragePixelText.spriteSheet.replaceColor(colorReplaceBuffer, paintColorPickerGUI.selectedColor, replaceColorPickerGUI.selectedColor);
                paintColorPickerGUI.selectedColor = replaceColorPickerGUI.selectedColor;
                spriteSheetGUI.isDirty = true;
                animStripGUI.isDirty = true;
                saveTexture();
                ragePixelText.refreshMesh();
                EditorUtility.SetDirty(ragePixelText);
                replaceColorPickerGUI.visible = false;
            }
            if (GUI.Button(new Rect(replaceColorPickerGUI.bounds.xMax + 5, replaceColorPickerGUI.bounds.yMin + 40, 102, 16), "Cancel"))
            {
                spriteSheetGUI.isDirty = true;
                animStripGUI.isDirty = true;
                spritesheetTexture.SetPixels32(colorReplaceBuffer);
                spritesheetTexture.Apply();
                saveTexture();
                EditorUtility.SetDirty(ragePixelText);
                replaceColorPickerGUI.visible = false;
            }

            GUI.DrawTexture(replaceColorPickerGUI.bounds, replaceColorPickerGUI.colorPickerTexture);
        }
    }

    public void HandlePaintGUI()
    {
        int guiPosX = 5;
        int guiPosY = (int)(scenePixelHeight / 2f - defaultSceneButtonWidth * 5f * 0.5f);

        GUI.color = GetSceneButtonColor(mode == Mode.Default);
        if (GUI.Button(new Rect(guiPosX, guiPosY, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Cursor))
        {
            paintColorPickerGUI.gizmoVisible = false;
            mode = Mode.Default;
            Tools.current = Tool.Move;
        }

        GUI.color = GetSceneButtonColor(mode == Mode.Pen);
        if (GUI.Button(new Rect(guiPosX, guiPosY += defaultSceneButtonWidth, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Pen))
        {
            paintColorPickerGUI.gizmoVisible = true;
            mode = Mode.Pen;
            Tools.current = Tool.None;
        }

        GUI.color = GetSceneButtonColor(mode == Mode.Fill);
        if (GUI.Button(new Rect(guiPosX, guiPosY += defaultSceneButtonWidth, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Fill))
        {
            paintColorPickerGUI.gizmoVisible = true;
            mode = Mode.Fill;
            Tools.current = Tool.None;
        }

        GUI.color = GetSceneButtonColor(mode == Mode.Select);
        if (GUI.Button(new Rect(4, guiPosY += defaultSceneButtonWidth, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Select))
        {
            selection = null;
            selectionActive = false;
            mode = Mode.Select;
            Tools.current = Tool.None;
        }

        GUI.color = GetSceneButtonColor(mode == Mode.Resize);
        if (GUI.Button(new Rect(4, guiPosY += defaultSceneButtonWidth, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Resize))
        {
            selection = null;
            selectionActive = false;
            mode = Mode.Resize;
            Tools.current = Tool.None;
        }
    }

    public void HandleAnimationGUI()
    {
        int guiPosX = 5;

        if (animationStripEnabled)
        {
            GUI.color = GetSceneButtonColor(animStripGUI.visible);
            if (GUI.Button(new Rect(guiPosX, scenePixelHeight - 40, defaultSceneButtonWidth, defaultSceneButtonWidth), RagePixelGUIIcons.Animation))
            {
                animStripGUI.visible = !animStripGUI.visible;
                animStripGUI.isDirty = true;
            }

            if (animStripGUI.visible)
            {
                GUI.color = Color.white;
                animStripGUI.maxWidth = scenePixelWidth - defaultSceneButtonWidth * 3 - 20 - 20;
                animStripGUI.positionY = scenePixelHeight - 8 - animStripGUI.pixelHeight;

                GUI.DrawTexture(animStripGUI.bounds, animStripGUI.animStripTexture);

                GUI.color = RagePixelGUIIcons.greenButtonColor;
                if (GUI.Button(new Rect(scenePixelWidth - (defaultSceneButtonWidth + 6f) * 2 - 5, scenePixelHeight - 40, defaultSceneButtonWidth + 6f, defaultSceneButtonHeight), "NEW"))
                {
                    int index = ragePixelText.GetCurrentRow().GetIndex(ragePixelText.currentCellKey) + 1;
                    RagePixelCell cell = ragePixelText.GetCurrentRow().InsertCell(index, RagePixelUtil.RandomKey());
                    ragePixelText.currentCellKey = cell.key;
                    RagePixelUtil.RebuildAtlas(ragePixelText.spriteSheet, true, "AddCell");
                    atlasTextureIsDirty = true;
                    spriteSheetGUI.isDirty = true;
                    animStripGUI.isDirty = true;
                }
                GUI.color = RagePixelGUIIcons.redButtonColor;
                if (GUI.Button(new Rect(scenePixelWidth - (defaultSceneButtonWidth + 6f) * 1 - 5, scenePixelHeight - 40, defaultSceneButtonWidth + 6f, defaultSceneButtonHeight), "DEL"))
                {
                    if (ragePixelText.GetCurrentRow().cells.Length > 1)
                    {
                        if (EditorUtility.DisplayDialog("Delete selected frame (no undo)?", "Are you sure?", "Delete", "Cancel"))
                        {
                            int index = ragePixelText.GetCurrentRow().GetIndex(ragePixelText.currentCellKey);
                            ragePixelText.GetCurrentRow().RemoveCellByKey(ragePixelText.currentCellKey);
                            RagePixelUtil.RebuildAtlas(ragePixelText.spriteSheet, false, "DeleteCell");
                            ragePixelText.currentCellKey = ragePixelText.GetCurrentRow().cells[Mathf.Clamp(index, 0, ragePixelText.GetCurrentRow().cells.Length - 1)].key;
                            atlasTextureIsDirty = true;
                            spriteSheetGUI.isDirty = true;
                            animStripGUI.isDirty = true;
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Cannot delete", "Cannot delete the last frame.", "OK");
                    }
                }

                if (animStripGUI.HandleGUIEvent(Event.current))
                {
                    ragePixelText.currentCellKey = animStripGUI.currentCellKey;
                    ragePixelText.refreshMesh();
                }
                else
                {
                    //animStripGUI.visible = false;
                }
            }
        }
    }

    public void HandleKeyboard()
    {
        Handles.EndGUI();

        if (Event.current.keyCode == KeyCode.Z && (Event.current.control || Event.current.command) && Event.current.alt && Event.current.type == EventType.keyDown)
        {
            DoPaintUndo();
            animStripGUI.isDirty = true;
            spriteSheetGUI.isDirty = true;
            atlasTextureIsDirty = true;
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.D && (Event.current.control || Event.current.command) && Event.current.alt && Event.current.type == EventType.keyDown && mode == Mode.Select)
        {
            backBuffer.PasteBitmap(selection.X, selection.Y, frontBuffer);
            animStripGUI.isDirty = true;
            spriteSheetGUI.isDirty = true;
            atlasTextureIsDirty = true;
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.X && (Event.current.control || Event.current.command) && Event.current.alt && Event.current.type == EventType.keyDown && mode == Mode.Select)
        {
            SavePaintUndo();
            RagePixelUtil.Settings.clipboard = new RagePixelBitmap(frontBuffer.pixels, frontBuffer.Width(), frontBuffer.Height());
            selectionActive = false;
            Rect currentUV = ragePixelText.GetCurrentCell().uv;
            Rect selectionUV = new Rect(
                currentUV.xMin + (float)selection.X / (float)spritesheetTexture.width,
                currentUV.yMin + (float)selection.Y / (float)spritesheetTexture.height,
                (float)(selection.X2 - selection.X + 1) / (float)spritesheetTexture.width,
                (float)(selection.Y2 - selection.Y + 1) / (float)spritesheetTexture.height
                );
            RagePixelUtil.clearPixels(spritesheetTexture, selectionUV);
            spritesheetTexture.Apply();
            atlasTextureIsDirty = true;

            selection = null;
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.C && (Event.current.control || Event.current.command) && Event.current.alt && Event.current.type == EventType.keyDown && mode == Mode.Select)
        {
            RagePixelUtil.Settings.clipboard = new RagePixelBitmap(frontBuffer.pixels, frontBuffer.Width(), frontBuffer.Height());
            selection = null;
            selectionActive = false;
            Event.current.Use();
        }
        if (Event.current.keyCode == KeyCode.V && (Event.current.control || Event.current.command) && Event.current.alt && Event.current.type == EventType.keyDown)
        {
            if (RagePixelUtil.Settings.clipboard != null)
            {
                mode = Mode.Select;

                SavePaintUndo();

                Rect spriteUV = ragePixelText.GetCurrentCell().uv;

                selection = new RagePixelTexelRect(
                    0,
                    0,
                    Mathf.Min(RagePixelUtil.Settings.clipboard.Width(), ragePixelText.GetCurrentRow().pixelSizeX),
                    Mathf.Min(RagePixelUtil.Settings.clipboard.Height(), ragePixelText.GetCurrentRow().pixelSizeY)
                    );

                backBuffer = GrabSprite(spriteUV);
                frontBuffer = RagePixelUtil.Settings.clipboard;

                frontBufferPosition = new RagePixelTexel(0, 0);
                frontBufferDragStartPosition = new RagePixelTexel(0, 0);

                PasteBitmapToSpritesheetAlpha(frontBufferPosition, spriteUV, frontBuffer);

                selectionActive = true;
                spritesheetTexture.Apply();

                Event.current.Use();
            }
        }
    }

    public void HandleCameraWarnings()
    {
        int guiPosY = 0;
        int guiPosX = 0;

        if (RagePixelUtil.Settings.showCameraWarnings)
        {
            if (!sceneCamera.orthographic || !SceneCameraFacingCorrectly())
            {
                guiPosY = 5;
                if (!sceneCamera.orthographic)
                {
                    guiPosX = (int)sceneCamera.pixelWidth / 2 - 90;
                    GUI.color = Color.red;
                    GUI.Label(new Rect(guiPosX, guiPosY, sceneCamera.pixelWidth, 20), "WARNING: sceneview camera is perspective");
                    guiPosY += 15;
                }
                if (!SceneCameraFacingCorrectly())
                {
                    guiPosX = (int)sceneCamera.pixelWidth / 2 - 105;
                    GUI.color = Color.red;
                    GUI.Label(new Rect(guiPosX, guiPosY, sceneCamera.pixelWidth, 20), "WARNING: Sceneview camera orientation != BACK");
                }

                guiPosX = (int)sceneCamera.pixelWidth - 180;
                guiPosY = 42;
                GUI.Label(new Rect(guiPosX, guiPosY, sceneCamera.pixelWidth, 20), "Right-click -->");
                GUI.color = Color.white;
            }
        }
    }

    public void OnSelected()
    {
        if (ragePixelText.spriteSheet == null && !RagePixelUtil.Settings.initialSpritesheetGenerated)
        {
            InitializeEmptyProject();
        }

        mode = Mode.Default;
        Tools.current = Tool.Move;
    }

    public void InitializeEmptyProject()
    {
        ragePixelText.transform.localScale = new Vector3(1f, 1f, 1f);
        List<UnityEngine.Object> assets = RagePixelUtil.allAssets;

        int count = 0;
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is RagePixelSpriteSheet)
            {
                count++;
            }
        }

        if (count == 0)
        {
            if (EditorUtility.DisplayDialog("RagePixel Spritesheet", "Create new RagePixel spritesheet automatically?", "Create", "Cancel"))
            {
                ragePixelText.spriteSheet = RagePixelUtil.CreateNewSpritesheet();
                ragePixelText.currentRowKey = ragePixelText.spriteSheet.rows[0].key;
                ragePixelText.currentCellKey = ragePixelText.GetCurrentRow().cells[0].key;
                ragePixelText.refreshMesh();
                RagePixelUtil.Settings.initialSpritesheetGenerated = true;
            }

            if (Camera.main != null)
            {
                if (Camera.main.GetComponent(typeof(RagePixelCamera)) == null)
                {
                    if (EditorUtility.DisplayDialog("RagePixel Camera", "Setup RagePixel camera automatically?\nCamera resolution = 1024x768\nPixel size = 2.", "Do it", "Cancel"))
                    {
                        RagePixelCamera ragecam = Camera.main.gameObject.AddComponent(typeof(RagePixelCamera)) as RagePixelCamera;
                        ragecam.resolutionPixelWidth = (int)1024;
                        ragecam.resolutionPixelHeight = (int)768;

                        ragecam.pixelSize = 2;
                        RagePixelUtil.ResetCamera(ragecam);
                    }
                }
            }

        }
        else
        {
            /*
            ragePixelText.spriteSheet = sheet;
            ragePixelText.currentRowKey = ragePixelText.spriteSheet.rows[0].key;
            ragePixelText.currentCellKey = ragePixelText.GetCurrentRow().cells[0].key;
            ragePixelText.refreshMesh();
            */
        }
    }

    public void DrawGizmos()
    {
        DrawSpriteBoundsGizmo();

        if (showGrid9Gizmo)
        {
            DrawGrid9Gizmo();
        }
    }

    public void DrawGrid9Gizmo()
    {
        
    }

    public void DrawSpriteBoundsGizmo()
    {
        Vector3 offset = new Vector3();
        Color spriteGizmoCol = Color.white;

        switch (mode)
        {
            case (Mode.Resize):
                spriteGizmoCol = new Color(0.7f, 1f, 0.7f, 0.3f);
                offset.x = ragePixelText.GetCurrentRow().newPixelSizeX;
                offset.y = ragePixelText.GetCurrentRow().newPixelSizeY;

                break;
            case (Mode.Default):
            case (Mode.Fill):
            case (Mode.Pen):
            case (Mode.Scale):
            case (Mode.Select):
                DrawSelectionBox();

                spriteGizmoCol = new Color(1f, 1f, 1f, 0.2f);
                offset.x = ragePixelText.GetCurrentRow().pixelSizeX;
                offset.y = ragePixelText.GetCurrentRow().pixelSizeY;
                break;
        }

        Vector3[] spriteGizmoVerts = new Vector3[4];

        spriteGizmoVerts[0] = worldToSceneScreenPoint(ragePixelText.transform.TransformPoint(GetPivotOffset() + new Vector3(0f, 0f, 0f)));
        spriteGizmoVerts[1] = worldToSceneScreenPoint(ragePixelText.transform.TransformPoint(GetPivotOffset() + new Vector3(0f, offset.y, 0f)));
        spriteGizmoVerts[2] = worldToSceneScreenPoint(ragePixelText.transform.TransformPoint(GetPivotOffset() + new Vector3(offset.x, offset.y, 0f)));
        spriteGizmoVerts[3] = worldToSceneScreenPoint(ragePixelText.transform.TransformPoint(GetPivotOffset() + new Vector3(offset.x, 0f, 0f)));

        Handles.DrawSolidRectangleWithOutline(spriteGizmoVerts, new Color(0f, 0f, 0f, 0f), spriteGizmoCol);
    }

    public void cancelColorReplacing()
    {
        spriteSheetGUI.spriteSheetTexture.SetPixels32(colorReplaceBuffer);
        spriteSheetGUI.spriteSheetTexture.Apply();
        saveTexture();
    }

    public void SavePaintUndo()
    {
        if (!paintUndoSaved)
        {
            paintUndoSaved = true;
            ragePixelText.spriteSheet.saveUndo(ragePixelText.GetCurrentCell());
        }
    }

    public void DoPaintUndo()
    {
        paintUndoSaved = false;
        ragePixelText.spriteSheet.DoUndo(ragePixelText.GetCurrentCell());
        EditorUtility.SetDirty(ragePixelText);
    }

    public void DrawSelectionBox()
    {
        if (selection != null)
        {
            Vector3[] verts = new Vector3[4];
            if (!selectionActive)
            {
                verts[0] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(selection.X, selection.Y)));
                verts[1] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(selection.X + selection.Width(), selection.Y)));
                verts[2] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(selection.X + selection.Width(), selection.Y + selection.Height())));
                verts[3] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(selection.X, selection.Y + selection.Height())));
                Handles.DrawSolidRectangleWithOutline(verts, new Color(0f, 0f, 0f, 0.04f), new Color(0f, 0f, 0f, 0.5f));
            }
            else
            {
                verts[0] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(frontBufferPosition.X, frontBufferPosition.Y)));
                verts[1] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(frontBufferPosition.X + selection.Width(), frontBufferPosition.Y)));
                verts[2] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(frontBufferPosition.X + selection.Width(), frontBufferPosition.Y + selection.Height())));
                verts[3] = worldToSceneScreenPoint(TexelCoordsToWorld(spritesheetTexture, ragePixelText.transform, new RagePixelTexel(frontBufferPosition.X, frontBufferPosition.Y + selection.Height())));
                Handles.DrawSolidRectangleWithOutline(verts, new Color(1f, 1f, 1f, 0.04f), new Color(1f, 1f, 1f, 0.5f));
            }
        }
    }

    public void HandleInspectorSpritesheetRemove()
    {
        if (ragePixelText.spriteSheet.rows.Length > 1)
        {
            if (EditorUtility.DisplayDialog("Delete selected sprite?", "Are you sure?", "Delete", "Cancel"))
            {
                int index = ragePixelText.spriteSheet.GetIndex(ragePixelText.GetCurrentRow().key);
                ragePixelText.spriteSheet.RemoveRowByKey(ragePixelText.GetCurrentRow().key);
                if (index > 0)
                {
                    ragePixelText.currentRowKey = ragePixelText.spriteSheet.rows[index - 1].key;
                }
                else
                {
                    ragePixelText.currentRowKey = ragePixelText.spriteSheet.rows[0].key;
                }
                RagePixelUtil.RebuildAtlas(ragePixelText.spriteSheet, false, "RemoveRow");
                spriteSheetGUI.isDirty = true;
                animStripGUI.isDirty = true;
            }
        }
    }

    public void RefreshPreviewLayer()
    {
        if (meshRenderer != null)
        {
            if (meshRenderer.sharedMaterials.Length == 1)
            {
                Material[] materials = new Material[2];
                Material layerMaterial = new Material(Shader.Find("RagePixel/Basic"));

                Texture2D texture = new Texture2D(ragePixelText.GetCurrentRow().pixelSizeX, ragePixelText.GetCurrentRow().pixelSizeY);
                texture.SetPixel(0, 0, new Color(0f, 1f, 0f, 1f));
                layerMaterial.SetTexture("_MainTex", texture);
                materials[0] = meshRenderer.sharedMaterials[0];
                materials[1] = layerMaterial;

                meshRenderer.sharedMaterials = materials;
            }
        }
    }

    public void FloodFill(Color oldColor, Color color, Texture2D tex, int fX, int fY, int minX, int minY, int maxX, int maxY)
    {
        tex.SetPixel(fX, fY, color);
        for (int y = Mathf.Max(fY - 1, minY); y <= Mathf.Min(fY + 1, maxY); y++)
        {
            for (int x = Mathf.Max(fX - 1, minX); x <= Mathf.Min(fX + 1, maxX); x++)
            {
                if (x == fX || y == fY)
                {
                    if (tex.GetPixel(x, y).Equals(oldColor))
                    {
                        FloodFill(oldColor, color, tex, x, y, minX, minY, maxX, maxY);
                    }
                }
            }
        }
    }

    public Color[] getScaledImage(Texture2D src, int width, int height, Color bgColor)
    {
        Color[] pixels = new Color[width * height];

        float ratioX = (float)src.width / (float)width;
        float ratioY = (float)src.height / (float)height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcX = Mathf.Clamp(Mathf.FloorToInt((float)x * ratioX), 0, src.width - 1);
                int srcY = Mathf.Clamp(Mathf.FloorToInt((float)y * ratioY), 0, src.height - 1);
                Color pixel = src.GetPixel(srcX, srcY);
                if (pixel.a >= 0.99f)
                {
                    pixels[x + y * width] = pixel;
                }
                else
                {
                    pixels[x + y * width] = pixel * pixel.a + bgColor * (1f - pixel.a);
                }
            }
        }
        return pixels;
    }

    public void ShowDebugInfo()
    {

        if (meshFilter != null)
        {
            Mesh m = meshFilter.sharedMesh;

            if (m != null)
            {
                Handles.BeginGUI();
                for (int i = 0; i < m.vertexCount; i++)
                {
                    //float screenHeight = sceneCamera.orthographicSize * 2f; 

                    Vector3 screenPos = worldToSceneScreenPoint(ragePixelText.transform.TransformPoint(m.vertices[i]));

                    Rect r = new Rect(screenPos.x + 5, screenPos.y, 200, 20);
                    GUI.Label(r, "POS:" + m.vertices[i].ToString());
                    Rect r2 = new Rect(screenPos.x + 5, screenPos.y + 12, 200, 20);
                    GUI.Label(r2, "UV: " + m.uv[i].x.ToString() + "," + m.uv[i].y.ToString());
                }
                Handles.EndGUI();
            }
            else
            {

            }
        }
    }

    public RagePixelTexel WorldToTexelCoords(Texture2D tex, Transform t, Vector3 worldPos)
    {
        RagePixelTexel coords = new RagePixelTexel();
        Vector3 localPos = t.InverseTransformPoint(worldPos) - GetPivotOffset();

        coords.X = Mathf.FloorToInt(localPos.x);
        coords.Y = Mathf.FloorToInt(localPos.y);

        if (coords.X >= 0 && coords.Y >= 0 && coords.X < ragePixelText.GetCurrentRow().pixelSizeX && coords.Y < ragePixelText.GetCurrentRow().pixelSizeY)
        {
            coords.X = coords.X % ragePixelText.GetCurrentRow().pixelSizeX;
            coords.Y = coords.Y % ragePixelText.GetCurrentRow().pixelSizeY;
        }

        return coords;
    }

    public Vector3 GetPivotOffset()
    {

        Vector3 pivotOffset = new Vector3();

        pivotOffset.x = 0f;
        pivotOffset.y = 0f;

        return pivotOffset;
    }


    public Vector3 TexelCoordsToWorld(Texture2D tex, Transform t, RagePixelTexel texel)
    {
        Vector3 v = new Vector3(texel.X, texel.Y, 0f);
        //v.Scale(new Vector3(1f/t.localScale.x,1f/t.localScale.y,1f));
        return t.TransformPoint(v + GetPivotOffset());
    }

    public Vector3 worldToSceneScreenPoint(Vector3 worldPos)
    {
        Camera sceneCamera = SceneView.lastActiveSceneView.camera;
        Vector3 screenPos = sceneCamera.WorldToScreenPoint(worldPos);
        return new Vector3(screenPos.x + 1f, -screenPos.y + sceneCamera.pixelHeight + 3f, 0f);
    }

    public Vector3 sceneScreenToWorldPoint(Vector3 sceneScreenPoint)
    {
        Camera sceneCamera = SceneView.lastActiveSceneView.camera;
        float screenHeight = sceneCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * sceneCamera.aspect;

        Vector3 worldPos = new Vector3(
            (sceneScreenPoint.x / sceneCamera.pixelWidth) * screenWidth - screenWidth * 0.5f,
            ((-(sceneScreenPoint.y) / sceneCamera.pixelHeight) * screenHeight + screenHeight * 0.5f),
            0f);

        worldPos += sceneCamera.transform.position;
        worldPos.z = 0f;

        return worldPos;
    }

    public void saveTexture()
    {
        RagePixelUtil.SaveSpritesheetTextureToDisk(ragePixelText.spriteSheet);
    }

    public RagePixelBitmap GrabSprite(Rect spriteUV)
    {
        return new RagePixelBitmap(
            ragePixelText.spriteSheet.getImage(
                ragePixelText.GetCurrentRowIndex(),
                ragePixelText.GetCurrentCellIndex()
                ),
            (int)(spriteUV.width * spritesheetTexture.width),
            (int)(spriteUV.height * spritesheetTexture.height)
            );
    }

    public RagePixelBitmap GrabRectFromSpritesheet(RagePixelTexelRect rect)
    {
        return new RagePixelBitmap(
            ragePixelText.spriteSheet.getImage(
                ragePixelText.GetCurrentRowIndex(), ragePixelText.GetCurrentCellIndex(),
                rect.X,
                rect.Y,
                rect.Width(),
                rect.Height()
                ),
            rect.Width(),
            rect.Height()
            );
    }

    public void CutRectInSpritesheet(RagePixelTexelRect rect, Rect spriteUV)
    {
        for (int y = rect.Y; y <= rect.Y2; y++)
        {
            for (int x = rect.X; x <= rect.X2; x++)
            {
                spritesheetTexture.SetPixel(
                    (int)(spriteUV.x * spritesheetTexture.width) + x,
                    (int)(spriteUV.y * spritesheetTexture.height) + y,
                    new Color(0f, 0f, 0f, 0f)
                    );
            }
        }
    }

    public void PasteBitmapToSpritesheet(RagePixelTexel position, Rect spriteUV, RagePixelBitmap bitmap)
    {
        for (int y = Mathf.Max(position.Y, 0); y < position.Y + bitmap.Height() && y < (int)(spriteUV.height * spritesheetTexture.height); y++)
        {
            for (int x = Mathf.Max(position.X, 0); x < position.X + bitmap.Width() && x < (int)(spriteUV.width * spritesheetTexture.width); x++)
            {
                spritesheetTexture.SetPixel(
                    (int)(spriteUV.x * spritesheetTexture.width) + x,
                    (int)(spriteUV.y * spritesheetTexture.height) + y,
                    bitmap.GetPixel(x - position.X, y - position.Y)
                    );
            }
        }
    }

    public void PasteBitmapToSpritesheetAlpha(RagePixelTexel position, Rect spriteUV, RagePixelBitmap bitmap)
    {
        for (int y = Mathf.Max(position.Y, 0); y < position.Y + bitmap.Height() && y < (int)(spriteUV.height * spritesheetTexture.height); y++)
        {
            for (int x = Mathf.Max(position.X, 0); x < position.X + bitmap.Width() && x < (int)(spriteUV.width * spritesheetTexture.width); x++)
            {
                Color src = bitmap.GetPixel(x - position.X, y - position.Y);
                Color trg = spritesheetTexture.GetPixel((int)(spriteUV.x * spritesheetTexture.width) + x, (int)(spriteUV.y * spritesheetTexture.height) + y);

                spritesheetTexture.SetPixel(
                    (int)(spriteUV.x * spritesheetTexture.width) + x,
                    (int)(spriteUV.y * spritesheetTexture.height) + y,
                    src + (1f - src.a) * trg
                    );
            }
        }
    }



    public void InvokeOnSelectedEvent()
    {
        if (!justSelected)
        {
            OnSelected();
            justSelected = true;
        }
    }

    public Camera GetSceneCamera()
    {
        Camera cam = SceneView.lastActiveSceneView.camera;
        return cam;
    }

    public int GetAtlasCellCount(RagePixelSpriteSheet[] spriteSheets, Material atlas)
    {
        int count = 0;

        foreach (RagePixelSpriteSheet sheet in spriteSheets)
        {
            if (sheet.atlas.Equals(atlas))
            {
                foreach (RagePixelRow row in sheet.rows)
                {
                    count += row.cells.Length;
                }
            }
        }

        return count;
    }

    public bool SceneCameraFacingCorrectly()
    {
        if (sceneCamera != null)
        {
            if (sceneCamera.transform.forward.z > 0.999999f && Mathf.Abs(sceneCamera.transform.forward.y) < 0.000001f && Mathf.Abs(sceneCamera.transform.forward.x) < 0.000001f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public Color GetSceneButtonColor(bool active)
    {
        if (active)
        {
            return new Color(0.8f, 0.925f, 1f, 1f);
        }
        else
        {
            return new Color(1f, 1f, 1f, 0.5f);
        }
    }

    private int RandomKey()
    {
        int val = (int)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        if (val == 0)
        {
            val++;
        }
        return (int)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }
}

