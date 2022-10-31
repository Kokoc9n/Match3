using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class Cell
{
    public Gem g;
    public Vector2Int Coord;
    public GameObject CellGameObject;
    public enum Type { Base, Void, Spawner };
    public Type t;
    public Cell(Type type, Vector2Int coord)
    {
        this.Coord = coord;
        this.t = type;
        CellGameObject = new GameObject();
        CellGameObject.AddComponent<SpriteRenderer>();
        CellGameObject.transform.position = new Vector2(coord.x, coord.y);
    }
    public void UpdateGemSprite()
    {
        if(g != null) CellGameObject.GetComponent<SpriteRenderer>().sprite = g.s;
        else CellGameObject.GetComponent<SpriteRenderer>().sprite = null;
    }
    public void DropGem(List<Cell> list, int index, int width)
    {
        list[index + width].g = g;
        g = null;

        UpdateGemSprite();
        list[index + width].UpdateGemSprite();

        GemMoveAnimation(list[index + width].CellGameObject, this.CellGameObject.transform.position);

    }
    private void GemMoveAnimation(GameObject cell, Vector3 target)
    {
        cell.transform.DOMove(target, (float)GridManager._Delay / ((float)GridManager._DropSpeed * (float)GridManager._Delay)).From();
    }
    public void GemSelectedAnimation()
    {
        CellGameObject.transform.DOScale(new Vector3(1.2f, 1.2f), 0.5f).SetLoops(-1, LoopType.Yoyo);
    }
    public void GemDeselectedAnimation()
    {
        CellGameObject.transform.DOKill();
        CellGameObject.transform.localScale = Vector3.one;
    }
    public void SwapGems(Cell target)
    {
        Gem gem = this.g;
        this.g = target.g;
        target.g = gem;

        Vector3 targetPos = target.CellGameObject.transform.position;
        Vector3 cellPos = this.CellGameObject.transform.position;

        target.CellGameObject.transform.DOMove(this.CellGameObject.transform.position, 0.5f).From();
        this.CellGameObject.transform.DOMove(targetPos, 0.5f).From();
        

        target.CellGameObject.transform.position = targetPos;
        this.CellGameObject.transform.position = cellPos;
        UpdateGemSprite();
        target.UpdateGemSprite();
    }
    public bool CheckBelow(List<Cell> list, int index, int width)
    {
        if (list[index + width].g == null 
            && list[index + width].t != Type.Void 
            && list[index + width] != null)
        {
            return true;
        }
        return false;
    }
    public bool CheckDiagonal(List<Cell> list, int index, int width)
    {
        // Check both diagonals.
        if (list[index + width + 1].g == null 
            && list[index + width - 1].g == null
            && list[index + width + 1].t != Type.Void 
            && list[index + width - 1].t != Type.Void)
        {
            // Gem not at the edge.
            if (list[index].Coord.x != list[0].Coord.x 
                && list[index].Coord.x != list[list.Count - 1].Coord.x) 
            {
                DropGem(list, index, width + Random.Range(0, 2) * 2 - 1);
                return true;
            }
        }
        // Right diagonal.
        else if (list[index + width + 1].g == null && list[index + width + 1].t != Type.Void) 
        {
            // Gem not at the edge.
            if (list[index].Coord.x != list[0].Coord.x && list[index].Coord.x != list[list.Count - 1].Coord.x) 
            {
                DropGem(list, index, width + 1);
                return true;
            }
        }
        // Left diagonal.
        else if (list[index + width - 1].g == null && list[index + width - 1].t != Type.Void) 
        {
            // Gem not at the edge.
            if (list[index].Coord.x != list[0].Coord.x && list[index].Coord.x != list[list.Count - 1].Coord.x) 
            {
                DropGem(list, index, width - 1);
                return true;
            }
        }
        return false;
    }
}
