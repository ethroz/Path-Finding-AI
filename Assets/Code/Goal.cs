using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public static int CurrentLevel = 0;
    public static List<GameObject> Levels = new List<GameObject>();
    public static List<Transform[]> Goals = new List<Transform[]>();
    public static GameObject Respawn;

    private void Awake()
    {
        GameObject[] allGoals = GameObject.FindGameObjectsWithTag("Goal");
        GameObject[] levelsArray = GameObject.FindGameObjectsWithTag("Level");
        for (int i = 0; i < levelsArray.Length; i++)
        {
            Levels.Add(levelsArray[i]);
        }
        GameObject[] levelsTemp = new GameObject[Levels.Count];
        for (int i = 0; i < levelsTemp.Length; i++)
        {
            levelsTemp[i] = Levels[i];
        }
        for (int i = 0; i < Levels.Count; i++)
        {
            GameObject temp = null;
            for (int j = 0; j < Levels.Count; j++)
            {
                if (levelsTemp[j].name == "Level " + (i + 1))
                {
                    temp = levelsTemp[j];
                    break;
                }
            }
            Levels[i] = temp;
        }
        for (int i = 0; i < Levels.Count; i++)
        {
            List<Transform> levelGoals = new List<Transform>();
            for (int j = 0; j < allGoals.Length; j++)
            {
                if (allGoals[j].GetComponentInParent<TagScript>().gameObject.name == "Level " + (i + 1).ToString())
                    levelGoals.Add(allGoals[j].transform);
            }
            Goals.Add(levelGoals.ToArray());
            string[] goalNames = new string[levelGoals.Count];
            for (int j = 0; j < levelGoals.Count; j++)
            {
                goalNames[j] = levelGoals[j].gameObject.name;
            }
            for (int j = 0; j < levelGoals.Count; j++)
            {
                Transform temp = null;
                for (int k = 0; k < levelGoals.Count; k++)
                {
                    if (j == 0)
                    {
                        if (goalNames[k] == "Respawn")
                        {
                            temp = levelGoals[k];
                            break;
                        }
                    }
                    else if (j == levelGoals.Count - 1)
                    {
                        if (goalNames[k] == "Finish")
                        {
                            temp = levelGoals[k];
                            break;
                        }
                    }
                    else if (j == 1)
                    {
                        if (goalNames[k] == "Goal")
                        {
                            temp = levelGoals[k];
                            break;
                        }
                    }
                    else
                    {
                        if (goalNames[k] == "Goal (" + (j - 1) + ")")
                        {
                            temp = levelGoals[k];
                            break;
                        }
                    }
                }
                if (temp == null)
                    Debug.Log("Fail: " + j);
                Goals[i][j] = temp;
            }
        }
        for (int i = 0; i < Levels.Count; i++)
        {
            for (int j = 0; j < Goals[i].Length; j++)
            {
                Goals[i][j].gameObject.GetComponent<GoalScript>().GoalNumber = j;
            }
        }
        Respawn = Goals[CurrentLevel][0].gameObject;
    }

    private void Update()
    {
        for (int i = 0; i < Goals.Count; i++)
        {
            for (int j = 0; j < Goals[i].Length - 1; j++)
            {
                Debug.DrawLine(Goals[i][j].position, Goals[i][j + 1].position, Color.blue);
            }
        }
    }

    public static void UpdateLevel()
    {
        CurrentLevel = (CurrentLevel + 1) % Levels.Count;
        Respawn = Goals[CurrentLevel][0].gameObject;
        SecondPlayer.ChangeLevel = true;
    }

    public static void SetLevel(int index)
    {
        CurrentLevel = index;
        Respawn = Goals[CurrentLevel][0].gameObject;
        SecondPlayer.ChangeLevel = true;
    }

    public static void AddLevel(GameObject level, Transform[] levelObjects)
    {
        Levels.Add(level);
        Goals.Add(levelObjects);
        Vector3 p = level.transform.position;
        p.y = 30 * Mathf.Abs(2 * (p.x - 14.5f)) / 33;
        PlayerScript.BEVPositions.Add(p);
        PlayerScript.UpdateLevel();
        Respawn = Goals[CurrentLevel][0].gameObject;
        SecondPlayer.ChangeLevel = true;
    }

    public static void RemoveLevel(int index)
    {
        Levels.RemoveAt(index);
        Goals.RemoveAt(index);
        PlayerScript.BEVPositions.RemoveAt(index);
    }
}