using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    TMP_Text turns;
    List<TMP_Text> list = new List<TMP_Text>();
    List<Image> imageList = new List<Image>();
    GridManager gridManager;
    [SerializeField]
    GameObject objectiveList, objectivePrefab, endScreen;
    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        foreach(GridManager.ObjectiveGem gem in gridManager.objectives)
        {
            GameObject g = Instantiate(objectivePrefab, objectiveList.transform);
            list.Add(g.GetComponent<TMP_Text>());   
        }
        for(int i = 0; i < gridManager.objectives.Length; i++)
        {
            list[i].transform.GetChild(0).GetComponent<Image>().sprite = gridManager.objectives[i].gem.sprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (gridManager.objectives[i].objectiveCount > 0) list[i].text = gridManager.objectives[i].objectiveCount.ToString();
            else list[i].text = "Done!";
        }
        turns.text = gridManager.turns.ToString() + "Turns left";
        if(gridManager.gameState == GridManager.GameState.End)
        {
            endScreen.SetActive(true);
            if(gridManager.win)
            {
                endScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "Win";
                endScreen.transform.GetChild(1).gameObject.SetActive(false);
            }
            else
            {
                endScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "Try again";
                endScreen.transform.GetChild(1).gameObject.SetActive(true);
            }
            
        }
        else
        {
            endScreen.SetActive(false);
        }
    }
}
