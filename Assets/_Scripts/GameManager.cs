using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

public class GameManager : MonoBehaviour
{
    public const int DELAY = 500;
    public const int DROP_SPEED = 18;
    [HideInInspector]
    public List<Cell> cells = new List<Cell>();
    public int Turns = 10;
    public bool Win;
    public GemObjective[] Objectives;
    public GemObjective[] startingValues;
    public enum GameState { Selecting, Resolve, Spawn, End, Win, Lose }
    public GameState State { get; private set; }

    [SerializeField] Gem[] gems;
    [SerializeField] int supertileSize = 5;
    [SerializeField] float swipeRange = 0.2f;
    [SerializeField] float radius = 3;
    private Tilemap map;
    private int height, width;
    private List<Cell> spawnerCells = new List<Cell>();

    private List<Cell> rows;
    private Vector2Int startPosition, endPosition;
    private int startingTurns;

    private bool gemMoved;
    private bool isBeingDragged;
    private Vector3 sPos;

    private Cell firstSelected, secondSelected, swipeStartCell;
    CancellationTokenSource tokenSource = new CancellationTokenSource();


    private void Awake()
    {
        map = transform.GetChild(0).GetComponent<Tilemap>();
    }
    void Start()
    {
        ObjectivesInit();

        SetupGrid();
        _ = SpawnGems(cells);

        State = GameState.Selecting;
        firstSelected = null;
        _ = PlayLevel();
    }
    public void RestartLevel()
    {
        firstSelected = null;
        Turns = startingTurns;
        Objectives = startingValues;
        FindObjectOfType<UIManager>().UpdateObjectives();
        foreach (Cell c in cells)
        {
            c.Gem = null;
        }
        _ = SpawnGems(cells);
        State = GameState.Selecting;
        _ = PlayLevel();
    }

    private void ObjectivesInit()
    {
        startingValues = new GemObjective[Objectives.Length];
        for (int i = 0; i < Objectives.Length; i++)
        {
            GemObjective o = new GemObjective();
            o.gem = Objectives[i].gem;
            o.ObjectiveCount = Objectives[i].ObjectiveCount;
            startingValues[i] = o;
        }
        startingTurns = Turns;
    }

    private async Task PopMatched(List<Cell> list)
    {
        foreach(Cell c in list) 
        {
            for (int i = 0; i < Objectives.Length; i++)
            {
                if (Objectives[i].gem == c.Gem)
                {
                    Objectives[i].ObjectiveCount--;
                }
            }
            c.Gem = null;
            c.UpdateGemSprite();
        }
        await Task.Yield();
    }
    private async Task InitDrop()
    {
        bool moved = false;
        for (int i = 0; i < width * height - 1 - width; i++)
        {
            if(rows[i].Gem != null)
            {
                if (rows[i].CheckBelow(rows, i, width))
                {
                    rows[i].DropGem(rows, i, width);
                    await Task.Delay(DELAY / DROP_SPEED);
                    moved = gemMoved = true;
                }
            }
        }
        if (moved)
        {
            moved = gemMoved = false;
            await InitDrop();
        }
        await Task.Yield();
    }
    private async Task InitSlide()
    {
        bool recursion = false;
        for (int i = 0; i < width * height - 1 - width; i++)
        {
            if (rows[i].Gem != null)
            {
                if (rows[i].CheckDiagonal(rows, i, width))
                {
                    await Task.Delay(DELAY / DROP_SPEED);
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
            if(list[i].CellType != Cell.Type.Void) superTile.Add(list[i]);
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
            cell.CellGameObject.transform.position =
                Vector3.MoveTowards(cell.CellGameObject.transform.position,
                                    target,
                                    0.5f);
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
                rows[i + 1].Gem != null && rows[i].Gem != null &&
                rows[i].Gem.Color == rows[i + 1].Gem.Color)
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
            if (cells[i + 1].Coord.x == cells[i].Coord.x && 
                cells[i + 1].Gem != null && cells[i].Gem != null &&
                cells[i].Gem.Color == cells[i + 1].Gem.Color)
            {
                if (!match3.Contains(cells[i])) { match3.Add(cells[i]); }
                match3.Add(cells[i + 1]);
            }
            else
            {
                if (match3.Count >= 3)
                {
                    matchedCellsColums.AddRange(match3);
                    // Supertile vertical.
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
        
        return matchedCellsColums;
    }
    private async Task SpawnGems(List<Cell> list)
    {
        foreach(Cell cell in list)
        {
            if (cell.CellType == Cell.Type.Base 
                || cell.CellType == Cell.Type.Spawner 
                && cell.Gem == null)
            {
                cell.Gem = gems[Random.Range(0, gems.Length)];
                cell.UpdateGemSprite();
            }
        }
        await Task.Yield();
    }
    private void SetupGrid()
    {
        for (int i = -20; i < 20; i++)
        {
            for (int k = -20; k < 20; k++)
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
    }
    private async Task SelectGems()
    {
        while (State == GameState.Selecting)
        {
            if (tokenSource.Token.IsCancellationRequested)
            {
                State = GameState.End;
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

                    if (secondSelected.CellType != Cell.Type.Void
                        && Mathf.Abs(Vector2.Distance(firstSelected.Coord, secondSelected.Coord)) <= 1
                        && firstSelected != secondSelected)
                    {
                        Turns--;
                        firstSelected.GemDeselectedAnimation();
                        State = GameState.Resolve;

                        firstSelected.SwapGems(secondSelected);
                        firstSelected = secondSelected = null;
                        await Task.Delay(DELAY);
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
                if (firstSelected == secondSelected 
                    || swipeStartCell == secondSelected 
                    && secondSelected == null) 
                {
                    // Reset.
                    firstSelected.GemDeselectedAnimation();

                    firstSelected = secondSelected = swipeStartCell = null;
                    return;
                }
                // Swipe logic. Checking difference between first and second position.
                if (startPosition != endPosition && swipeStartCell != null 
                    && Vector2.Distance(firstSelected.Coord, startPosition) < radius / 3)
                {
                    float deltaX = endPosition.x - startPosition.x;
                    float deltaY = endPosition.y - startPosition.y;
                    if ((deltaX > swipeRange || deltaX < -swipeRange))
                    {
                        if (startPosition.x < endPosition.x)
                        {
                            // Right.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x + 1,
                                                                                        firstSelected.Coord.y)).FirstOrDefault();
                        }
                        else
                        {
                            // Left.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x - 1,
                                                                                        firstSelected.Coord.y)).FirstOrDefault();
                        }
                    }
                    else if ((deltaY <= -swipeRange || deltaY >= swipeRange))
                    {
                        if (startPosition.y < endPosition.y)
                        {
                            // Up.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x,
                                                                                        firstSelected.Coord.y + 1)).FirstOrDefault();
                        }
                        else
                        {
                            // Down.
                            secondSelected = cells.Where(n => n.Coord == new Vector2Int(firstSelected.Coord.x,
                                                                                        firstSelected.Coord.y - 1)).FirstOrDefault();
                        }
                    }
                    if (secondSelected.CellType != Cell.Type.Void)
                    {
                        firstSelected.GemDeselectedAnimation();
                        firstSelected.SwapGems(secondSelected);
                        firstSelected = secondSelected = null;
                        Turns--;
                        State = GameState.Resolve;
                        await Task.Delay(DELAY);
                    }
                }
            }
            await Task.Yield();
        }
    }

    private async Task PlayLevel()
    {
        while (State != GameState.End)
        {   
            if (State == GameState.Selecting)
            {
                await SelectGems();
            }
            else if (State == GameState.Resolve)
            {
                List<Cell> matched = await GetMatched();
                if (matched.Count > 0)
                {
                    await PopMatched(matched);
                    await Task.Delay(DELAY);
                    await InitDrop();
                    await InitSlide();
                    while (gemMoved)
                    {
                        await InitDrop();
                        await InitSlide();
                        await InitDrop();
                    }
                }
                State = GameState.Spawn;
                await Task.Delay(DELAY);
            }
            else if (State == GameState.Spawn)
            {
                while (spawnerCells.Where(n => n.Gem == null).Any())
                {
                    await SpawnGems(spawnerCells);
                    await Task.Delay(DELAY);
                    await InitDrop();
                    await InitSlide();
                }
                List<Cell> matched = await GetMatched();
                int count = 0;
                foreach (GemObjective objective in Objectives)
                {
                    if (objective.ObjectiveCount >= 0) count += objective.ObjectiveCount;
                }
                if (gemMoved || matched.Count > 0)
                    State = GameState.Resolve;
                else if (Turns != 0 && count > 0)
                    State = GameState.Selecting;
                else if(count == 0)
                {
                    Win = true;
                    firstSelected = secondSelected = null;
                    break;
                }
                else
                {
                    Win = false;
                    firstSelected = secondSelected = null;
                    break;
                }
            }
            await Task.Yield();
        }
        State = GameState.End;
    }
    private void OnApplicationQuit()
    {
        tokenSource.Cancel();
    }
}
