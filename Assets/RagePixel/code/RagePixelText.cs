using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class RagePixelText : MonoBehaviour
{
    public Vector3 accuratePosition;
    public RagePixelSpriteSheet spriteSheet;
        
    private RagePixelSpriteSheet lastCellSpriteSheetCache;
    private RagePixelCell lastCellCache;
    private int lastCellCacheKey;
    private RagePixelSpriteSheet lastRowSpriteSheetCache;
    private RagePixelRow lastRowCache;
    private int lastRowCacheKey;

    public string text = "HELLO WORLD";
    public int textKerning = 2;
    public int textLeading = 7;
    public int textSpaceWidth = 5;

    public int currentRowKey;
    public int currentCellKey;
    public int ZLayer;
    public bool meshIsDirty = false;
    public bool vertexColorsAreDirty = false;

    public Color tintColor = new Color(1f, 1f, 1f, 1f);
    private bool toBeRefreshed;

    public bool editMode = false;

    void Awake()
    {
        lastRowSpriteSheetCache = null;
        lastCellSpriteSheetCache = null;
        lastRowCache = null;
        lastCellCache = null;
        lastCellCacheKey = 0;
        lastRowCacheKey = 0;

        meshIsDirty = true;
        vertexColorsAreDirty = true;

        if (!Application.isPlaying)
        {
            MeshFilter meshFilter = null;
            MeshRenderer meshRenderer = null;

            meshRenderer = gameObject.GetComponent("MeshRenderer") as MeshRenderer;
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>() as MeshRenderer;
            }

            meshFilter = gameObject.GetComponent("MeshFilter") as MeshFilter;
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>() as MeshFilter;
            }

            if (meshFilter.sharedMesh != null)
            {
                RagePixelText[] ragePixelSprites = GameObject.FindObjectsOfType(typeof(RagePixelText)) as RagePixelText[];

                foreach (RagePixelText ragePixelSprite in ragePixelSprites)
                {
                    MeshFilter otherMeshFilter = ragePixelSprite.GetComponent(typeof(MeshFilter)) as MeshFilter;
                    if (otherMeshFilter != null)
                    {
                        if (otherMeshFilter.sharedMesh == meshFilter.sharedMesh && otherMeshFilter != meshFilter)
                        {
                            meshFilter.mesh = new Mesh();
                            toBeRefreshed = true;
                        }
                    }
                }
            }

            if (meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = new Mesh();
                toBeRefreshed = true;
            }
        }
    }

    void Start()
    {

    }

    void OnEnable()
    {

    }

    public void SnapToScale()
    {
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public void SnapToIntegerPosition()
    {
        if (!Application.isPlaying)
        {
            //transform.rotation = Quaternion.identity;
            transform.localEulerAngles = new Vector3(0f, 0f, transform.localEulerAngles.z);
        }
        SnapToScale();
        transform.localPosition = new Vector3(Mathf.RoundToInt(transform.localPosition.x), Mathf.RoundToInt(transform.localPosition.y), ZLayer);
    }

    public void SnapToIntegerPosition(float divider)
    {
        transform.rotation = Quaternion.identity;
        SnapToScale();
        transform.localPosition = new Vector3(Mathf.RoundToInt(transform.localPosition.x * divider) / divider, Mathf.RoundToInt(transform.localPosition.y * divider) / divider, ZLayer);
    }

    public void refreshMesh(bool replaceMeshFilter = false)
    {
        MeshRenderer meshRenderer = GetComponent(typeof(MeshRenderer)) as MeshRenderer;
        MeshFilter meshFilter = GetComponent(typeof(MeshFilter)) as MeshFilter;

        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>() as MeshRenderer;
        }
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>() as MeshFilter;
        }

        if (meshFilter.sharedMesh == null || replaceMeshFilter)
        {
            DestroyImmediate(meshFilter.sharedMesh);
            meshFilter.mesh = new Mesh();
        }

        if (meshFilter.sharedMesh.vertexCount == 0)
        {
            meshIsDirty = true;
        }

        GenerateMesh(meshFilter.sharedMesh);

        if (!Application.isPlaying)
        {
            SnapToIntegerPosition();
            SnapToScale();
        }
        else
        {
            //SnapToScale();
        }

        if (meshRenderer.sharedMaterial != spriteSheet.atlas)
        {
            meshRenderer.sharedMaterial = spriteSheet.atlas;
        }
    }

    public void GenerateMesh(Mesh mesh)
    {
        if (meshIsDirty)
        {
            mesh.Clear();
        }

        int[] triangles = null;
        Vector3[] verts = null;
        Vector2[] uvs = null;
        Color[] colors = null;

        Rect uv = new Rect();

        int tIndex = 0;
        int uvIndex = 0;
        int vIndex = 0;
        int cIndex = 0;

        float offX;
        float offY;

        RagePixelRow currentRow = GetCurrentRow();
        RagePixelCell currentCell = GetCurrentCell();
                
        Vector3 pos = new Vector3();

        string txt = "";

        int charCount = 0;

        if (!editMode)
        {
            for (int chIndex = 0; chIndex < text.Length; chIndex++)
            {
                string character = text.Substring(chIndex, 1);
                if (character != " " && character != "\n")
                {
                    foreach (RagePixelRow r in spriteSheet.rows)
                    {
                        if (r != null)
                        {
                            if (r.fontCharacter != null)
                            {
                                if (r.fontCharacter.Equals(character))
                                {
                                    charCount++;
                                    txt += character;
                                }
                            }
                        }
                    }
                }
                else
                {
                    txt += character;
                }
            }
        }
        else
        {
            charCount = 1;
        }

        triangles = new int[6 * charCount];
        verts = new Vector3[4 * charCount];
        uvs = new Vector2[4 * charCount];
        colors = new Color[4 * charCount];

        if (!editMode)
        {
            for (int chIndex = 0; chIndex < txt.Length; chIndex++)
            {
                string character = txt.Substring(chIndex, 1);
                currentRow = null;
                currentCell = null;
                
                if (character.Equals(" "))
                {
                    pos.x += textSpaceWidth;
                }
                else if (character.Equals("\n"))
                {
                    pos.x = 0f;
                    pos.y -= textLeading;
                }
                else
                {
                    foreach (RagePixelRow r in spriteSheet.rows)
                    {
                        if (r != null)
                        {
                            if (r.fontCharacter != null)
                            {
                                if (r.fontCharacter.Equals(character))
                                {
                                    currentRow = r;
                                    currentCell = r.cells[0];
                                }
                            }
                        }
                    }

                    if (currentRow != null)
                    {
                        pos.y += currentRow.fontYOffset;

                        triangles[tIndex++] = vIndex;
                        triangles[tIndex++] = vIndex + 1;
                        triangles[tIndex++] = vIndex + 2;
                        triangles[tIndex++] = vIndex;
                        triangles[tIndex++] = vIndex + 2;
                        triangles[tIndex++] = vIndex + 3;

                        offY = (float)currentRow.pixelSizeY;
                        offX = (float)currentRow.pixelSizeX;

                        verts[vIndex++] = pos + new Vector3(0f, offY, 0f);
                        verts[vIndex++] = pos + new Vector3(0f + offX, offY, 0f);
                        verts[vIndex++] = pos + new Vector3(0f + offX, 0f, 0f);
                        verts[vIndex++] = pos + new Vector3(0f, 0f, 0f);

                        pos.x += offX + textKerning;

                        uv = currentCell.uv;

                        uvs[uvIndex++] = new Vector2(uv.xMin, uv.yMin + uv.height);
                        uvs[uvIndex++] = new Vector2(uv.xMin + uv.width, uv.yMin + uv.height);
                        uvs[uvIndex++] = new Vector2(uv.xMin + uv.width, uv.yMin);
                        uvs[uvIndex++] = new Vector2(uv.xMin, uv.yMin);

                        if (vertexColorsAreDirty || meshIsDirty)
                        {
                            colors[cIndex++] = tintColor;
                            colors[cIndex++] = tintColor;
                            colors[cIndex++] = tintColor;
                            colors[cIndex++] = tintColor;
                        }

                        pos.y -= currentRow.fontYOffset;
                    }
                }
            }
        }
        else
        {
            triangles[tIndex++] = vIndex;
            triangles[tIndex++] = vIndex + 1;
            triangles[tIndex++] = vIndex + 2;
            triangles[tIndex++] = vIndex;
            triangles[tIndex++] = vIndex + 2;
            triangles[tIndex++] = vIndex + 3;

            offY = (float)currentRow.pixelSizeY;
            offX = (float)currentRow.pixelSizeX;

            verts[vIndex++] = pos + new Vector3(0f, offY, 0f);
            verts[vIndex++] = pos + new Vector3(0f + offX, offY, 0f);
            verts[vIndex++] = pos + new Vector3(0f + offX, 0f, 0f);
            verts[vIndex++] = pos + new Vector3(0f, 0f, 0f);

            uv = currentCell.uv;

            uvs[uvIndex++] = new Vector2(uv.xMin, uv.yMin + uv.height);
            uvs[uvIndex++] = new Vector2(uv.xMin + uv.width, uv.yMin + uv.height);
            uvs[uvIndex++] = new Vector2(uv.xMin + uv.width, uv.yMin);
            uvs[uvIndex++] = new Vector2(uv.xMin, uv.yMin);

            if (vertexColorsAreDirty || meshIsDirty)
            {
                colors[cIndex++] = tintColor;
                colors[cIndex++] = tintColor;
                colors[cIndex++] = tintColor;
                colors[cIndex++] = tintColor;
            }
        }
        
        
        if (meshIsDirty)
        {
            mesh.vertices = verts;
            mesh.triangles = triangles;
        }
        if (vertexColorsAreDirty || meshIsDirty)
        {
            mesh.colors = colors;
        }

        mesh.uv = uvs;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        meshIsDirty = false;
        vertexColorsAreDirty = false;
    }

    public void checkKeyIntegrity()
    {
        if (!GetCurrentRow().key.Equals(currentRowKey))
        {
            currentRowKey = GetCurrentRow().key;
        }
        if (!GetCurrentCell().key.Equals(currentCellKey))
        {
            currentCellKey = GetCurrentCell().key;
        }
    }

    public void OnDestroy()
    {
        MeshFilter meshFilter = GetComponent(typeof(MeshFilter)) as MeshFilter;
        if (meshFilter != null)
        {
            DestroyImmediate(meshFilter.sharedMesh);
        }
    }

    public void shiftRow(int amount)
    {
        int currIndex = spriteSheet.GetIndex(currentRowKey);
        if (currIndex + amount >= 0 && currIndex + amount < spriteSheet.rows.Length)
        {
            currentRowKey = spriteSheet.rows[currIndex + amount].key;
            currentCellKey = GetCurrentRow().cells[0].key;
        }
        else
        {
            if (currIndex < 0)
            {
                //noop
            }
            else
            {
                if (amount > 0)
                {
                    currentRowKey = spriteSheet.rows[0].key;
                }
                else
                {
                    currentRowKey = spriteSheet.rows[spriteSheet.rows.Length - 1].key;
                }
                currentCellKey = GetCurrentRow().cells[0].key;
            }
        }
    }

    public void selectRow(int index)
    {
        if (index >= 0 && index < spriteSheet.rows.Length)
        {
            currentRowKey = spriteSheet.rows[index].key;
        }
    }
    public void selectCell(int index)
    {
        currentCellKey = GetCurrentRow().cells[0].key;
    }

    public string getCurrentRowName()
    {
        return GetCurrentRow().name;
    }

    public RagePixelRow GetCurrentRow()
    {
        if (Application.isPlaying)
        {
            if (lastRowSpriteSheetCache != null)
            {
                if (lastRowCacheKey == currentRowKey && lastRowSpriteSheetCache.Equals(spriteSheet))
                {
                    return lastRowCache;
                }
                else
                {
                    lastRowCache = spriteSheet.GetRow(currentRowKey);
                    lastRowCacheKey = currentRowKey;
                    lastRowSpriteSheetCache = spriteSheet;
                    return lastRowCache;
                }
            }
            else
            {
                lastRowCache = spriteSheet.GetRow(currentRowKey);
                lastRowCacheKey = currentRowKey;
                lastRowSpriteSheetCache = spriteSheet;
                return lastRowCache;
            }
        }
        else
        {
            return spriteSheet.GetRow(currentRowKey);
        }
    }

    public RagePixelCell GetCurrentCell()
    {
        if (Application.isPlaying)
        {
            if (lastCellSpriteSheetCache != null)
            {
                if (lastCellCacheKey == currentCellKey && lastCellSpriteSheetCache.Equals(spriteSheet))
                {
                    return lastCellCache;
                }
                else
                {
                    lastCellCache = GetCurrentRow().GetCell(currentCellKey);
                    lastCellCacheKey = currentCellKey;
                    lastCellSpriteSheetCache = spriteSheet;
                    return lastCellCache;
                }
            }
            else
            {
                lastCellCache = GetCurrentRow().GetCell(currentCellKey);
                lastCellCacheKey = currentCellKey;
                lastCellSpriteSheetCache = spriteSheet;
                return lastCellCache;
            }
        }
        else
        {
            return GetCurrentRow().GetCell(currentCellKey);
        }
    }

    public int GetCurrentCellIndex()
    {
        return GetCurrentRow().GetIndex(GetCurrentCell().key);
    }

    public int GetCurrentRowIndex()
    {
        return spriteSheet.GetIndex(currentRowKey);
    }

    public void OnDrawGizmosSelected()
    {
        if (toBeRefreshed)
        {
            refreshMesh();
            toBeRefreshed = false;
        }
    }

    void Update()
    {
        
    }

    private void DrawRectangle(Rect rect, Color color)
    {
        Color oldColor = Gizmos.color;

        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin, 0f), new Vector3(rect.xMax, rect.yMin, 0f));
        Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMin, 0f), new Vector3(rect.xMax, rect.yMax, 0f));
        Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMax, 0f), new Vector3(rect.xMin, rect.yMax, 0f));
        Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMax, 0f), new Vector3(rect.xMin, rect.yMin, 0f));

        Gizmos.color = oldColor;
    }


    // API
    public void SetSprite(string name)
    {
        int key = spriteSheet.GetRowByName(name).key;
        if (key != 0)
        {
            currentRowKey = spriteSheet.GetRowByName(name).key;
            currentCellKey = GetCurrentRow().cells[0].key;
            meshIsDirty = true;
            refreshMesh();
        }
        else
        {
            Debug.Log("ERROR: No RagePixel sprite with name " + name + " found!");
        }
    }

    public void SetSprite(string name, int frameIndex)
    {
        int key = spriteSheet.GetRowByName(name).key;
        if (key != 0)
        {
            currentRowKey = spriteSheet.GetRowByName(name).key;
            if (GetCurrentRow().cells.Length > frameIndex)
            {
                currentCellKey = GetCurrentRow().cells[frameIndex].key;
                meshIsDirty = true;
                refreshMesh();
            }
            else
            {
                Debug.Log("ERROR: RagePixel has only " + GetCurrentRow().cells.Length + " frames!");
            }
        }
        else
        {
            Debug.Log("ERROR: No RagePixel sprite with name " + name + " found!");
        }
    }

    public void SetTintColor(Color color)
    {
        tintColor = color;
        vertexColorsAreDirty = true;
        refreshMesh();
    }

}
