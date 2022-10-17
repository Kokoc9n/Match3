using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using DG.Tweening;

public class GridManager : MonoBehaviour
{
    [SerializeField]
    Gem[] gems;
    Tilemap map;
    int height, width;
    [HideInInspector]
    public List<Cell> cells = new List<Cell>();
    List<Cell> spawnerCells = new List<Cell>();

    public static int delay = 1000;
    // Gem drop speed mod. (Delay / dropSpeed).
    public static int dropSpeed = 18;
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
    public bool win;
    [SerializeField]
    public int turns = 10;
    [SerializeField]
    float radius = 3;
    private int startingTurns;
    [System.Serializable]
    public class ObjectiveGem
    {
        public int objectiveCount;
        public Gem gem;
    }
    public ObjectiveGem[] objectives;
    private ObjectiveGem[] startingValues;
    Vector2Int startPosition, endPosition;
    [SerializeField]
    float swipeRange = 0.2f;
    bool gemMoved;
    bool isBeingDragged;
    Vector3 sPos;

    public void RestartLevel()
    {
        turns = startingTurns;
        objectives = startingValues;
        foreach(Cell c in cells)
        {
            c.gem = null;
        }
        _ = SpawnGems(cells);
        gameState = GameState.Selecting;

        firstSelected = null;
        //endState = false;
        _ = PlayLevel();
    }
    void Start()
    {
        Application.targetFrameRate = 60;
        map = transform.GetChild(0).GetComponent<Tilemap>();
        BoundsInt bounds = map.cellBounds;
        TileBase[] allTiles = map.GetTilesBlock(bounds);
        startingValues = objectives;
        startingTurns = turns;
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
            c.gem = null;
            c.UpdateGemSprite();
        }
        await Task.Yield();
    }
    private async Task InitDrop()
    {
        bool recursion = false;
        // Cells to rows format
        cells = cells.OrderByDescending(n => n.coord.y).ToList();
        for (int i = 0; i < width * height - 1 - width; i++)
        {
            if(cells[i].gem != null)
            {
                if (cells[i].CheckBelow(cells, i, width))
                {
                    // Drop gem to last cell? one cell down?
                    cells[i].DropGem(cells, i, width);
                    await Task.Delay(delay / dropSpeed);
                    recursion = gemMoved = true;
                }
            }
        }
        if (recursion)
        {
            Debug.Log("recursion");
            recursion = gemMoved = false;
            await InitDrop();
        }
        //await Task.Delay(300);
        await Task.Yield();
    }
    private async Task InitSlide()
    {
        bool recursion = false;
        // Cells to rows format
        cells = cells.OrderByDescending(n => n.coord.y).ToList();
        for (int i = 0; i < width * height - 1 - width; i++)
        {
            if (cells[i].gem != null)
            {
                if (cells[i].CheckDiagonal(cells, i, width))
                {
                    await Task.Delay(delay / dropSpeed);
                    recursion = gemMoved = true;
                    await InitDrop();
                }
            }
        }
        if (recursion)
        {
            Debug.Log("recursion Slide");
            recursion = gemMoved = false;
            await InitSlide();
        }
        //await Task.Delay(300);
        await Task.Yield();
    }
    private async Task<List<Cell>> SuperTile(List<Cell> list, int width, int index)
    {
        int rowNumber = Mathf.FloorToInt(index / width) + 1;
        List<Cell> superTile = new List<Cell>();
        // Start at first cell of the row/column.
        for (int i = width * rowNumber - width; i < width * rowNumber; i++)
        {
            if(list[i].type != Cell.Type.Void) superTile.Add(list[i]);
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
            cell.cellGameObject.transform.position = Vector3.MoveTowards(cell.cellGameObject.transform.position, target, 0.5f);
            Debug.Log("following");
            await Task.Yield();
        }
    }
    private async Task<List<Cell>> GetMatched()
    {
        List<Cell> matchedCellsRows = new List<Cell>();
        List<Cell> matchedCellsColums = new List<Cell>();
        List<Cell> match3 = new List<Cell>();

        cells = cells.OrderByDescending(i => i.coord.y).ToList();
        for (int i = 0; i < width * height - 1; i++)
        {
            if (cells[i + 1].coord.y == cells[i].coord.y && cells[i + 1].gem != null && cells[i].gem != null &&
                cells[i].gem.color == cells[i + 1].gem.color)
            {
                if (!match3.Contains(cells[i])) match3.Add(cells[i]);
                match3.Add(cells[i + 1]);
            }
            else
            {
                if (match3.Count >= 3)
                {
                    if (match3.Count >= supertileSize) 
                    // Supertile horizontal
                    matchedCellsRows.AddRange(match3);
                    {
                        matchedCellsRows.AddRange(await SuperTile(cells, width, i));
                    }
                }
                
                match3.Clear();
            }
        }
        cells = cells.OrderBy(i => i.coord.x).ToList();
        for (int i = 0; i < width * height - 1; i++)
        {
            if (cells[i + 1].coord.x == cells[i].coord.x && cells[i + 1].gem != null && cells[i].gem != null &&
                cells[i].gem.color == cells[i + 1].gem.color)
            {
                if (!match3.Contains(cells[i])) match3.Add(cells[i]);
                match3.Add(cells[i + 1]);
            }
            else
            {
                if (match3.Count >= 3)
                {
                    matchedCellsColums.AddRange(match3);
                    // Supertile vertical
                    if (match3.Count >= supertileSize) 
                    {
                        matchedCellsColums.AddRange(await SuperTile(cells, height, i));
                    }
                }

                match3.Clear();
            }
        }

        matchedCellsColums.AddRange(matchedCellsRows);
        matchedCellsColums = matchedCellsColums.Distinct().ToList();
        foreach (Cell c in matchedCellsColums)
        {
            for(int i = 0; i < objectives.Length; i++)
            {
                if(objectives[i].gem == c.gem)
                {
                    objectives[i].objectiveCount--;
                }
            }
        }
        return matchedCellsColums;
    }
    private async Task SpawnGems(List<Cell> list)
    {
        foreach(Cell cell in list)
        {
            if (cell.type == Cell.Type.Base || cell.type == Cell.Type.Spawner && cell.gem == null)
            {
                cell.gem = gems[Random.Range(0, gems.Length)];
                cell.UpdateGemSprite();
            }
        }
        await Task.Yield();
    }
    void SetupGrid()
    {
        // Fix bounds to true map.size bounds to avoid useless iterations

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
        width = Mathf.Abs(cells[0].coord.x - cells[cells.Count - 1].coord.x) + 1;
        height = Mathf.Abs(cells[0].coord.y - cells[cells.Count - 1].coord.y) + 1;
    }
    private async Task SelectGems()
    {
        Debug.Log("SelectGems called");
        while (gameState == GameState.Selecting)
        {
            if (tokenSource.Token.IsCancellationRequested)
            {
                gameState = GameState.End;
                break;
            }
            Debug.Log("selectingStateLoop");
            if (Input.GetMouseButtonDown(0))
            {
                startPosition = Vector2Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if(firstSelected == null)
                {
                    swipeStartCell = cells.Where(n => n.coord == startPosition).FirstOrDefault();
                    sPos = swipeStartCell.cellGameObject.transform.position;
                    isBeingDragged = true;
                    _ = FollowPointer(swipeStartCell);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                isBeingDragged = false;
                swipeStartCell.cellGameObject.transform.position = sPos;

                if (firstSelected != null)
                {
                    secondSelected = cells.Where(n => n.coord == startPosition).FirstOrDefault();

                    if (secondSelected.type != Cell.Type.Void
                        && Mathf.Abs(Vector2.Distance(firstSelected.coord, secondSelected.coord)) <= 1
                        && firstSelected != secondSelected)
                    {
                        turns--;
                        firstSelected.GemDeselectedAnimation();
                        gameState = GameState.Resolve;

                        firstSelected.SwapGems(secondSelected);
                        firstSelected = secondSelected = null;
                        await Task.Delay(delay);
                        return;
                    }
                }
                else
                {
                    firstSelected = cells.Where(n => n.coord == startPosition).FirstOrDefault();
                    secondSelected = null;
                    // Animation
                    firstSelected.GemSelectedAnimation();
                    Debug.Log(firstSelected.coord + " First selected");
                }
                endPosition = Vector2Int.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
                if (firstSelected == secondSelected || swipeStartCell == secondSelected && secondSelected == null) 
                {
                    // Reset.
                    print("same cell selected");
                    firstSelected.GemDeselectedAnimation();

                    firstSelected = secondSelected = swipeStartCell = null;
                    return;
                }
                // Swipe logic. Checking difference between first and second position.
                if (startPosition != endPosition && swipeStartCell != null && Vector2.Distance(firstSelected.coord, startPosition) < radius / 3)
                {
                    float deltaX = endPosition.x - startPosition.x;
                    float deltaY = endPosition.y - startPosition.y;
                    if ((deltaX > swipeRange || deltaX < -swipeRange))
                    {
                        if (startPosition.x < endPosition.x)
                        {
                            // Right.
                            secondSelected = cells.Where(n => n.coord == new Vector2Int(firstSelected.coord.x + 1, firstSelected.coord.y)).FirstOrDefault();
                        }
                        else
                        {
                            // Left.
                            secondSelected = cells.Where(n => n.coord == new Vector2Int(firstSelected.coord.x - 1, firstSelected.coord.y)).FirstOrDefault();
                        }
                    }
                    else if ((deltaY <= -swipeRange || deltaY >= swipeRange))
                    {
                        if (startPosition.y < endPosition.y)
                        {
                            // Up.
                            secondSelected = cells.Where(n => n.coord == new Vector2Int(firstSelected.coord.x, firstSelected.coord.y + 1)).FirstOrDefault();
                        }
                        else
                        {
                            // Down.
                            secondSelected = cells.Where(n => n.coord == new Vector2Int(firstSelected.coord.x, firstSelected.coord.y - 1)).FirstOrDefault();
                        }
                    }
                    if (secondSelected.type != Cell.Type.Void)
                    {
                        firstSelected.GemDeselectedAnimation();
                        firstSelected.SwapGems(secondSelected);
                        firstSelected = secondSelected = null;
                        turns--;
                        gameState = GameState.Resolve;
                        await Task.Delay(delay);
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
                if (matched.Count != 0) // > 0 ?
                {
                    await PopMatched(matched);
                    await Task.Delay(delay);
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
                await Task.Delay(delay);
            }
            else if(gameState == GameState.Spawn)
            {
                while (spawnerCells.Where(n => n.gem == null).Any())
                {
                    await SpawnGems(spawnerCells);
                    await Task.Delay(delay);
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
                    await Task.Delay(delay);
                }
                else
                {
                    gameState = GameState.Resolve;
                    await Task.Delay(delay);
                }
            }

            int count = 0;
            foreach (ObjectiveGem objective in objectives)
            {
                if (objective.objectiveCount >= 0) count += objective.objectiveCount;
            }
            if (count != 0 && turns == 0)
            {
                // Defeat.
                win = false;
                gameState = GameState.End;
            }
            else if(count == 0)
            {
                // Win.
                win = true;
                gameState = GameState.End;
            }
            await Task.Yield();
        }

    }
}
