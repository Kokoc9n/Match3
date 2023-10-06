using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class Cell
{
    public Gem Gem;
    public Vector2Int Coord;
    public GameObject CellGameObject;
    public enum Type { Base, Void, Spawner };
    public Type CellType;

    private SpriteRenderer spriteRenderer;
    public Cell(Type type, Vector2Int coord)
    {
        Coord = coord;
        CellType = type;
        CellGameObject = new GameObject();
        spriteRenderer = CellGameObject.AddComponent<SpriteRenderer>();
        CellGameObject.transform.position = new Vector2(coord.x, coord.y);
    }
    public void UpdateGemSprite()
    {
        if(Gem != null)
            spriteRenderer.sprite = Gem.GemSprite;
        else spriteRenderer.sprite = null;
    }
    public void DropGem(List<Cell> list, int index, int width)
    {
        list[index + width].Gem = Gem;
        Gem = null;

        UpdateGemSprite();
        list[index + width].UpdateGemSprite();

        GemMoveAnimation(list[index + width].CellGameObject, this.CellGameObject.transform.position);

    }
    private void GemMoveAnimation(GameObject cell, Vector3 target)
    {
        cell.transform.DOMove(target,
            (float)GameManager.DELAY / ((float)GameManager.DROP_SPEED * (float)GameManager.DELAY)).From();
    }
    public void GemSelectedAnimation()
    {
        CellGameObject.transform.DOScale(new Vector3(1.2f, 1.2f),
                                         0.5f).SetLoops(-1, LoopType.Yoyo);
    }
    public void GemDeselectedAnimation()
    {
        CellGameObject.transform.DOKill();
        CellGameObject.transform.localScale = Vector3.one;
    }
    public void SwapGems(Cell target)
    {
        Gem gem = Gem;
        Gem = target.Gem;
        target.Gem = gem;

        Vector3 targetPos = target.CellGameObject.transform.position;
        Vector3 cellPos = CellGameObject.transform.position;

        target.CellGameObject.transform.DOMove(CellGameObject.transform.position,
                                               0.5f).From();
        CellGameObject.transform.DOMove(targetPos, 0.5f).From();
        

        target.CellGameObject.transform.position = targetPos;
        CellGameObject.transform.position = cellPos;
        UpdateGemSprite();
        target.UpdateGemSprite();
    }
    public bool CheckBelow(List<Cell> list, int index, int width)
    {
        if (list[index + width].Gem == null 
            && list[index + width].CellType != Type.Void 
            && list[index + width] != null)
        {
            return true;
        }
        return false;
    }
    public bool CheckDiagonal(List<Cell> list, int index, int width)
    {
        // Check both diagonals.
        if (list[index + width + 1].Gem == null 
            && list[index + width - 1].Gem == null
            && list[index + width + 1].CellType != Type.Void 
            && list[index + width - 1].CellType != Type.Void)
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
        else if (list[index + width + 1].Gem == null 
            && list[index + width + 1].CellType != Type.Void) 
        {
            // Gem not at the edge.
            if (list[index].Coord.x != list[0].Coord.x 
                && list[index].Coord.x != list[list.Count - 1].Coord.x) 
            {
                DropGem(list, index, width + 1);
                return true;
            }
        }
        // Left diagonal.
        else if (list[index + width - 1].Gem == null 
            && list[index + width - 1].CellType != Type.Void) 
        {
            // Gem not at the edge.
            if (list[index].Coord.x != list[0].Coord.x 
                && list[index].Coord.x != list[list.Count - 1].Coord.x) 
            {
                DropGem(list, index, width - 1);
                return true;
            }
        }
        return false;
    }
}
