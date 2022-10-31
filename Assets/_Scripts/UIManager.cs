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
        foreach(GridManager.ObjectiveGem gem in gridManager.Objectives)
        {
            GameObject g = Instantiate(objectivePrefab, objectiveList.transform);
            list.Add(g.GetComponent<TMP_Text>());   
        }
        for(int i = 0; i < gridManager.Objectives.Length; i++)
        {
            list[i].transform.GetChild(0).GetComponent<Image>().sprite = gridManager.Objectives[i].g.s;
        }
    }
    void Update()
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (gridManager.Objectives[i].ObjectiveCount > 0) list[i].text = gridManager.Objectives[i].ObjectiveCount.ToString();
            else list[i].text = "Done!";
        }
        turns.text = gridManager.Turns.ToString() + "Turns left";
        if(gridManager.gameState == GridManager.GameState.End)
        {
            endScreen.SetActive(true);
            if(gridManager.Win)
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
