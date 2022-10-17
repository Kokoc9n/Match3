using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using System.Threading;

[System.Serializable]
public class Cell
{
    public Gem gem;
    public Vector2Int coord;
    public GameObject cellGameObject; //change to private
    public enum Type { Base, Void, Spawner };
    public Type type;
    public Cell(Type type, Vector2Int coord)
    {
        this.coord = coord;
        this.type = type;
        cellGameObject = new GameObject();
        cellGameObject.AddComponent<SpriteRenderer>();
        cellGameObject.transform.position = new Vector2(coord.x, coord.y);
    }
    public void UpdateGemSprite()
    {
        if(gem != null) cellGameObject.GetComponent<SpriteRenderer>().sprite = gem.sprite;
        else cellGameObject.GetComponent<SpriteRenderer>().sprite = null;
    }
    public void DropGem(List<Cell> list, int index, int width)
    {
        list[index + width].gem = gem;
        gem = null;
        //Debug.Log(index + " dropped to: " + (index + width));

        UpdateGemSprite();
        list[index + width].UpdateGemSprite();

        GemMoveAnimation(list[index + width].cellGameObject, this.cellGameObject.transform.position);

    }
    private void GemMoveAnimation(GameObject cell, Vector3 target)
    {
        cell.transform.DOMove(target, (float)GridManager.delay / ((float)GridManager.dropSpeed * (float)GridManager.delay)).From();
    }
    public void GemSelectedAnimation()
    {
        cellGameObject.transform.DOScale(new Vector3(1.2f, 1.2f), 0.5f).SetLoops(-1, LoopType.Yoyo);
    }
    public void GemDeselectedAnimation()
    {
        cellGameObject.transform.DOKill();
        cellGameObject.transform.localScale = Vector3.one;
    }
    public void SwapGems(Cell target)
    {
        Gem gem = this.gem;
        this.gem = target.gem;
        target.gem = gem;

        Vector3 targetPos = target.cellGameObject.transform.position;
        Vector3 cellPos = this.cellGameObject.transform.position;

        target.cellGameObject.transform.DOMove(this.cellGameObject.transform.position, 1).From();
        this.cellGameObject.transform.DOMove(targetPos, 1).From();
        

        target.cellGameObject.transform.position = targetPos;
        this.cellGameObject.transform.position = cellPos;
        UpdateGemSprite();
        target.UpdateGemSprite();
    }
    public bool CheckBelow(List<Cell> list, int index, int width)
    {
        if (list[index + width].gem == null 
            && list[index + width].type != Type.Void 
            && list[index + width] != null)
        {
            return true;
        }
        return false;
    }
    public bool CheckDiagonal(List<Cell> list, int index, int width)
    {
        // Check both diagonals
        if (list[index + width + 1].gem == null 
            && list[index + width - 1].gem == null
            && list[index + width + 1].type != Type.Void 
            && list[index + width - 1].type != Type.Void)
        {
            // Gem not at the edge
            if (list[index].coord.x != list[0].coord.x 
                && list[index].coord.x != list[list.Count - 1].coord.x) 
            {
                DropGem(list, index, width + Random.Range(0, 2) * 2 - 1);
                return true;
            }
        }
        // Right diagonal
        else if (list[index + width + 1].gem == null && list[index + width + 1].type != Type.Void) 
        {
            // Gem not at the edge
            if (list[index].coord.x != list[0].coord.x && list[index].coord.x != list[list.Count - 1].coord.x) 
            {
                DropGem(list, index, width + 1);
                return true;
            }
        }
        // Left diagonal
        else if (list[index + width - 1].gem == null && list[index + width - 1].type != Type.Void) 
        {
            // Gem not at the edge
            if (list[index].coord.x != list[0].coord.x && list[index].coord.x != list[list.Count - 1].coord.x) 
            {
                DropGem(list, index, width - 1);
                return true;
            }
        }
        return false;
    }
}
