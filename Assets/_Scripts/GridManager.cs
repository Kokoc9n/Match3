using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

public class GridManager : MonoBehaviour
{
    [SerializeField]
    Gem[] gems;
    Tilemap map;
    int height, width;
    [HideInInspector]
    public List<Cell> cells = new List<Cell>();
    List<Cell> spawnerCells = new List<Cell>();

    List<Cell> rows;
    List<Cell> collums;

    public static int _Delay = 500;
    // Gem drop speed mod. (Delay / dropSpeed).
    public static int _DropSpeed = 18;
    [SerializeField]
    int supertileSize = 5;
    Cell firstSelected, secondSelected, swipeStartCell;
    [HideInInspector]
    public enum GameState { Selecting, Resolve, Spawn, End, Win, Lose }
    [HideInInspector]
    public GameState gameState;
    CancellationTokenSource tokenSource = new CancellationTokenSource();
    private void OnApplicationQuit()
    {
        tokenSource.Cancel();
    }
    public bool Win;
    [SerializeField]
    public int Turns = 10;
    [SerializeField]
    float radius = 3;
    private int startingTurns;
    [System.Serializable]
    public class ObjectiveGem
    {
        public int ObjectiveCount;
        public Gem g;
    }
    public ObjectiveGem[] Objectives;
    public ObjectiveGem[] startingValues;
    Vector2Int startPosition, endPosition;
    [SerializeField]
    float swipeRange = 0.2f;
    bool gemMoved;
    bool isBeingDragged;
    Vector3 sPos;

    public void RestartLevel()
    {
        firstSelected = null;
        Turns = startingTurns;
        Objectives = startingValues;
        FindObjectOfType<UIManager>().UpdateObjectives();
        foreach (Cell c in cells)
        {
            c.g = null;
        }
        _ = SpawnGems(cells);
        gameState = GameState.Selecting;
        _ = PlayLevel();
    }
    void Start()
    {
        Application.targetFrameRate = 60;
        map = transform.GetChild(0).GetComponent<Tilemap>();
        BoundsInt bounds = map.cellBounds;
        TileBase[] allTiles = map.GetTilesBlock(bounds);
        startingValues = new ObjectiveGem[Objectives.Length];
        for (int i = 0; i < Objectives.Length; i++)
        {
            ObjectiveGem o = new ObjectiveGem();
            o.g = Objectives[i].g;
            o.ObjectiveCount = Objectives[i].ObjectiveCount;
            startingValues[i] = o;
        }
        startingTurns = Turns;
        // Grid gameobjects spawning&assigning, adding to lists.
        SetupGrid();
        // Populating grid with gems.
        _ = SpawnGems(cells);
        gameState = GameState.Selecting;
        firstSelected = null;
        _ = PlayLevel();
    }
    private async Task PopMatched(List<Cell> list)
    {
        foreach(Cell c in list) 
        {
            for (int i = 0; i < Objectives.Length; i++)
            {
                if (Objectives[i].g == c.g)
                {
                    Objectives[i].ObjectiveCount--;
                }
            }
            c.g = null;
            c.UpdateGemSprite();
        }
        await Task.Yield();
    }
    private async Task InitDrop()
    {
        bool recursion = false;
        for (int i = 0; i < width * height - 1 - width; i++)
        {
            if(rows[i].g != null)
            {
                if (rows[i].CheckBelow(rows, i, width))
                {
                    rows[i].DropGem(rows, i, width);
                    await Task.Delay(_Delay / _DropSpeed);
                    recursion = gemMoved = true;
                }
            }
        }
        if (recursion)
        {
            recursion = gemMoved = false;
            await InitDrop();
        }
        await Task.Yield();
    }
    private async Task InitSlide()
    {
        bool recursion = false;
        for (int i = 0; i < width * height - 1 - width; i++)
        {
            if (rows[i].g != null)
            {
                if (rows[i].CheckDiagonal(rows, i, width))
                {
                    await Task.Delay(_Delay / _DropSpeed);
                    recursion = gemMoved = true;
                    await InitDrop();
                }
            }
        }
        if (recursion)
        {
            recursion = gemMoved = false;
            await InitSlide();
        }
        await Task.Yield();
    }
    private async Task<List<Cell>> SuperTile(List<Cell> list, int width, int index)
    {
        int rowNumber = Mathf.FloorToInt(index / width) + 1;
        List<Cell> superTile = new List<Cell>();
        // Start at first cell of the row/column.
        for (int i = width * rowNumber - width; i < width * rowNumber; i++)
        {
            if(list[i].t != Cell.Type.Void) superTile.Add(list[i]);
        }
        return superTile;
    }
    private async Task FollowPointer(Cell cell)
    {
        while (isBeingDragged)
        {
            Vector3 offset = Camera.main.ScreenToWorldPoint(Input.mousePosition) - sPos;
            Vector3 target =  Vector3.ClampMagnitude(offset, radius)
                + sPos 
                - new Vector3(0, 0, Camera.main.transform.position.z);
            cell.CellGameObject.transform.position = Vector3.MoveTowards(cell.CellGameObject.transform.position, target, 0.5f);
            await Task.Yield();
        }
    }
    private async Task<List<Cell>> GetMatched()
    {
        List<Cell> matchedCellsRows = new List<Cell>();
        List<Cell> matchedCellsColums = new List<Cell>();
        List<Cell> match3 = new List<Cell>();

        for (int i = 0; i < width * height - 1; i++)
        {
            if (rows[i + 1].Coord.y == rows[i].Coord.y &&
                rows[i + 1].g != null && rows[i].g != null &&
                rows[i].g.Color == rows[i + 1].g.Color)
            {
                if (!match3.Contains(rows[i])) { match3.Add(rows[i]); }
                match3.Add(rows[i + 1]);
            }
            else
            {
                if (match3.Count >= 3)
                {
                    matchedCellsRows.AddRange(match3);
                    // Supertile horizontal.
                    if (match3.Count >= supertileSize)
                    {
                        matchedCellsRows.AddRange(await SuperTile(rows, width, i));
                    }
                }
                
                match3.Clear();
            }
        }

        for (int i = 0; i < width * height - 1; i++)
        {
            if (collums[i + 1].Coord.x == collums[i].Coord.x && 
                collums[i + 1].g != null && collums[i].g != null &&
                collums[i].g.Color == collums[i + 1].g.Color)
            {
                if (!match3.Contains(collums[i])) { match3.Add(collums[i]); }
                match3.Add(collums[i + 1]);
            }
            else
            {
                if (match3.Count >= 3)
                {
                    matchedCellsColums.AddRange(match3);
                    // Supertile vertical.
                    if (match3.Count >= supertileSize) 
                    {
                        matchedCellsColums.AddRange(await SuperTile(collums, height, i));
                    }
                }

                match3.Clear();
            }
        }

        matchedCellsColums.AddRange(matchedCellsRows);
        matchedCellsColums = matchedCellsColums.Distinct().ToList();
        
        return matchedCellsColums;
    }
    private async Task SpawnGems(List<Cell> list)
    {
        foreach(Cell cell in list)
        {
            if (cell.t == Cell.Type.Base || cell.t == Cell.Type.Spawner && cell.g == null)
            {
                cell.g = gems[Random.Range(0, gems.Length)];
                cell.UpdateGemSprite();
            }
        }
        await Task.Yield();
    }
    void SetupGrid()
    {
        for (int i = -100; i < 100; i++)
        {
            for (int k = -100; k < 100; k++)
            {
                if(map.GetTile(new Vector3Int(i, k, 0)) != null)
                {
                    if (map.GetTile(new Vector3Int(i, k, 0)).name == "Base")
                    {
                        cells.Add(new Cell(Cell.Type.Base, new Vector2Int(i, k)));
                    }
                    else if (map.GetTile(new Vector3Int(i, k, 0)).name == "Spawner")
                    {
                        Cell cell = new Cell(Cell.Type.Spawner, new Vector2Int(i, k));
                        cells.Add(cell);
                        spawnerCells.Add(cell);
                    }
                    else if (map.GetTile(new Vector3Int(i, k, 0)).name == "Void")
                    {
                        cells.Add(new Cell(Cell.Type.Void, new Vector2Int(i, k)));
                    }
                } 
            }
        }
        width = Mathf.Abs(cells[0].Coord.x - cells[cells.Count - 1].Coord.x) + 1;
        height = Mathf.Abs(cells[0].Coord.y - cells[cells.Count - 1].Coord.y) + 1;

        rows = new List<Cell>(cells.OrderByDescending(i => i.Coord.y).ToList());
        collums = new List<Cell>(cells.OrderBy(i => i.Coord.x).ToList());
    }
    private async Task SelectGems()
    {
        while (gameState == GameState.Selecting)
        {
            if (tokenSource.Token.IsCancellationRequested)
            {
                gameState = GameState.End;
                break;
            }
            if (Input.GetMouseButtonDown(0))
            {
                startPosition = Vector2Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if(firstSelected == null)
                {
                    swipeStartCell = cells.Where(n => n.Coord == startPosition).FirstOrDefault();
                    sPos = swipeStartCell.CellGameObject.transform.position;
                    isBeingDragged = true;
                    _ = FollowPointer(swipeStartCell);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                isBeingDragged = false;
                swipeStartCell.CellGameObject.transform.position = sPos;

                if (firstSelected != null)
                {
                    secondSelected = cells.Where(n => n.Coord == startPosition).FirstOrDefault();

                    if (secondSelected.t != Cell.Type.Void
                        && Mathf.Abs(Vector2.Distance(firstSelected.Coord, secondSelected.Coord)) <= 1
                        && firstSelected != secondSelected)
                    {
                        Turns--;
                        firstSelected.GemDeselectedAnimation();
                        gameState = GameState.Resolve;

                        firstSelected.SwapGems(secondSelected);
                        firstSelected = secondSelected = null;
                        await Task.Delay(_Delay);
                        return;
                    }
                }
                else
                {
                    firstSelected = cells.Where(n => n.Coord == startPosition).FirstOrDefault();
                    secondSelected = null;
                    // Animation.
                    firstSelected.GemSelectedAnimation();
                }
                endPosition = Vector2Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if (firstSelected == secondSelected || swipeStartCell == secondSelected && secondSelected == null) 
                {
                    // Reset.
                    firstSelected.GemDeselectedAnimation();

                    firstSelected = secondSelected = swipeStartCell = null;
                    return;
                }
                // Swipe logic. Checking difference between first and second position.
                if (startPosition != endPosition && swipeStartCell != null && Vector2.Distance(firstSelected.Coord, startPosition) < radius / 3)
                {
                    float deltaX = endPosition.x - startPosition.x;
                    float deltaY = endPosition.y - startPosition.y;
                    if ((deltaX > swipeRange || deltaX < -swipeRange))
                    {
                        if (startPosition.x < endPosition.x)
                        {
                            // Right.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x + 1, firstSelected.Coord.y)).FirstOrDefault();
                        }
                        else
                        {
                            // Left.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x - 1, firstSelected.Coord.y)).FirstOrDefault();
                        }
                    }
                    else if ((deltaY <= -swipeRange || deltaY >= swipeRange))
                    {
                        if (startPosition.y < endPosition.y)
                        {
                            // Up.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x, firstSelected.Coord.y + 1)).FirstOrDefault();
                        }
                        else
                        {
                            // Down.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x, firstSelected.Coord.y - 1)).FirstOrDefault();
                        }
                    }
                    if (secondSelected.t != Cell.Type.Void)
                    {
                        firstSelected.GemDeselectedAnimation();
                        firstSelected.SwapGems(secondSelected);
                        firstSelected = secondSelected = null;
                        Turns--;
                        gameState = GameState.Resolve;
                        await Task.Delay(_Delay);
                    }
                }
            }
            await Task.Yield();
        }
    }

    private async Task PlayLevel()
    {
        while (gameState != GameState.End)
        {
            if (gameState == GameState.Selecting)
            {
                await SelectGems();
            }
            else if (gameState == GameState.Resolve)
            {
                List<Cell> matched = await GetMatched();
                if (matched.Count > 0)
                {
                    await PopMatched(matched);
                    await Task.Delay(_Delay);
                    await InitDrop();
                    await InitSlide();
                    while (gemMoved)
                    {
                        await InitDrop();
                        await InitSlide();
                        await InitDrop();
                    }
                }
                gameState = GameState.Spawn;
                await Task.Delay(_Delay);
            }
            else if(gameState == GameState.Spawn)
            {
                while (spawnerCells.Where(n => n.g == null).Any())
                {
                    await SpawnGems(spawnerCells);
                    await Task.Delay(_Delay);
                    await InitDrop();
                    await InitSlide();
                    while (gemMoved)
                    {
                        await InitDrop();
                        await InitSlide();
                        await InitDrop();
                    }
                }
                List<Cell> matched = await GetMatched();
                if (matched.Count == 0)
                {
                    gameState = GameState.Selecting;
                    await Task.Delay(_Delay);
                }
                else
                {
                    gameState = GameState.Resolve;
                    await Task.Delay(_Delay);
                }
            }

            loopStart:
            int count = 0;
            foreach (ObjectiveGem objective in Objectives)
            {
                if (objective.ObjectiveCount >= 0) count += objective.ObjectiveCount;
            }

            if(Turns == 0)
            {
                List<Cell> matched = await GetMatched();

                if (matched.Count > 0)
                {
                    await PopMatched(matched);
                    await Task.Delay(_Delay);
                    await InitDrop();
                    await InitSlide();
                    while (gemMoved)
                    {
                        await InitDrop();
                        await InitSlide();
                        await InitDrop();
                    }
                }
                await Task.Delay(_Delay);
                while (spawnerCells.Where(n => n.g == null).Any())
                {
                    await SpawnGems(spawnerCells);
                    await Task.Delay(_Delay);
                    await InitDrop();
                    await InitSlide();
                    while (gemMoved)
                    {
                        await InitDrop();
                        await InitSlide();
                        await InitDrop();
                    }
                }
                matched = await GetMatched();
                if (matched.Count == 0)
                {
                    await Task.Delay(_Delay);
                }
                else
                {
                    await Task.Delay(_Delay);
                    goto loopStart;
                }
                if (count == 0)
                {
                    // Win.
                    Win = true;
                    gameState = GameState.End;
                }
                else
                {
                    // Defeat.
                    Win = false;
                    gameState = GameState.End;
                }
            }
            await Task.Yield();
        }

    }
}
