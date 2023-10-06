using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] TMP_Text turns;
    [SerializeField] GameObject objectiveList, objectivePrefab, endScreen;
    List<TMP_Text> list = new List<TMP_Text>();
    GameManager gridManager;
    void Start()
    {
        gridManager = FindObjectOfType<GameManager>();
        for(int i = 0; i < gridManager.Objectives.Length; i++)
        {
            GameObject objectiveObject = Instantiate(objectivePrefab, objectiveList.transform);
            list.Add(objectiveObject.GetComponent<TMP_Text>());
            list[i].transform.GetChild(0).GetComponent<Image>().sprite = gridManager.Objectives[i].g.GemSprite;
        }
    }
    public void UpdateObjectives()
    {
        foreach(Transform t in objectiveList.transform)
        {
            if (t.name != "Turn") Destroy(t.gameObject);
        }
        list.Clear();
        foreach (GemObjective gem in gridManager.Objectives)
        {
            GameObject g = Instantiate(objectivePrefab, objectiveList.transform);
            list.Add(g.GetComponent<TMP_Text>());
        }
        for (int i = 0; i < gridManager.Objectives.Length; i++)
        {
            list[i].transform.GetChild(0).GetComponent<Image>().sprite = gridManager.Objectives[i].g.GemSprite;
        }
    }
    void Update()
    {
        for(int i = 0; i < list.Count; i++)
        {
            if (gridManager.Objectives[i].ObjectiveCount > 0)
            {
                list[i].text = gridManager.Objectives[i].ObjectiveCount.ToString();
            }
            else list[i].text = "Done!";
        }
        turns.text = gridManager.Turns.ToString() + " Turns left";
        if(gridManager.State == GameManager.GameState.End)
        {
            endScreen.SetActive(true);
            if(gridManager.Win)
            {
                endScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "Win";
                endScreen.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = "Restart";
            }
            else
            {
                endScreen.transform.GetChild(0).GetComponent<TMP_Text>().text = "Try again";
                endScreen.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text = "Restart";
            }
            
        }
        else
        {
            endScreen.SetActive(false);
        }
    }
}
