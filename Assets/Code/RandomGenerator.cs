using System.Collections.Generic;
using UnityEngine;

public class RandomGenerator : MonoBehaviour
{
    public static string Seed;
    private static int Size = 10;
    public static int Must = 100;
    public static int Minimum = 0;
    private static bool Initialized = false;
    public static GameObject LevelObject;
    public static GameObject Straight;
    public static GameObject Left;
    public static GameObject Right;
    public static GameObject End;
    private static GameObject Chosen;
    public static GameObject Respawn;
    public static GameObject Goals;
    public static GameObject Finish;
    private static int Direction;
    private static List<Transform> LevelObjects;
    private static Vector3[,] Positions = new Vector3[Size, Size];
    private static bool[,] Path = new bool[Size, Size];
    private static List<int> Directions;
    private static List<Vector3> Moves;
    private static int Count;
    private static int Attempts;

    private void Awake()
    {
        Straight = Resources.Load("Straight") as GameObject;
        Left = Resources.Load("Left") as GameObject;
        Right = Resources.Load("Right") as GameObject;
        End = Resources.Load("End") as GameObject;
        Respawn = Resources.Load("Respawn") as GameObject;
        Goals = Resources.Load("Goal") as GameObject;
        Finish = Resources.Load("Finish") as GameObject;
        LevelObject = GameObject.Find("Level 6");
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Positions[i, j] = new Vector3(11f - (6 * i), 0, (LevelObject.transform.position.z + 3.5f) + (6 * j));
            }
        }
        if (Size % 2 == 1)
            LevelObject.transform.position = Positions[(Size - 1) / 2, (Size - 1) / 2];
        else
            LevelObject.transform.position = Positions[Size / 2, Size / 2] + (3 * (new Vector3(1, 0, -1)));
        transform.position = LevelObject.transform.position;
        ResetBools();
    }

    private void Start()
    {
        GenerateMoves();
    }

    private void Update()
    {
        while (Initialized)
        {
            if (Count == Moves.Count)
            {
                Initialized = false;
                Goal.AddLevel(LevelObject, LevelObjects.ToArray());
                return;
            }
            Build();
            PlaceGoals();
            Count++;
        }
    }

    private static void Build()
    {
        if (Count % (Moves.Count - 1) == 0)
            Chosen = End;
        else
        {
            if (Directions[Count - 1] == Directions[Count])
                Chosen = Straight;
            else if (Directions[Count - 1] % 3 != 0)
            {
                if (Directions[Count] - Directions[Count - 1] == 1)
                    Chosen = Right;
                else
                    Chosen = Left;
            }
            else
            {
                if (Directions[Count] == 0 || Directions[Count] == 1)
                    Chosen = Right;
                else
                    Chosen = Left;
            }
        }

        GameObject temp;
        if (Count < Moves.Count - 1)
            temp = Instantiate(Chosen, Moves[Count], Quaternion.Euler(0, 90 * Directions[Count], 0), LevelObject.transform);
        else
            temp = Instantiate(Chosen, Moves[Count], Quaternion.Euler(0, 90 * ((Directions[Count - 1] + 2) % 4), 0), LevelObject.transform);
        temp.name = temp.name.Remove(temp.name.Length - 7);
        if (temp.name == "Right")
            Direction = -1;
        else if (temp.name == "Left")
            Direction = 1;
        else
            Direction = 0;
        temp.name = "Wall";
        temp.AddComponent<TagScript>();
    }

    private static void PlaceGoals()
    {
        float offset = Direction * 45;
        if (Count == 0)
            Chosen = Respawn;
        else if (Count == Moves.Count - 1)
            Chosen = Finish;
        else
            Chosen = Goals;

        if (Count == 0)
            LevelObjects.Add(Instantiate(Chosen, Moves[Count] + Vector3.up, Quaternion.Euler(0, 90 * Directions[Count] + offset - 90, 0), LevelObject.transform).GetComponent<Transform>());

        else if (Count < Moves.Count - 1)
            LevelObjects.Add(Instantiate(Chosen, Moves[Count] + Vector3.up, Quaternion.Euler(0, 90 * Directions[Count] + offset, 0), LevelObject.transform).GetComponent<Transform>());
        else
        {
            Vector3 adjustment;
            switch (Directions[Count - 1])
            {
                case 0:
                    adjustment = Vector3.right;
                    break;
                case 1:
                    adjustment = Vector3.back;
                    break;
                case 2:
                    adjustment = Vector3.left;
                    break;
                default:
                    adjustment = Vector3.forward;
                    break;
            }
            LevelObjects.Add(Instantiate(Chosen, Moves[Count] + Vector3.up + adjustment, Quaternion.Euler(0, 90 * Directions[Count - 1] + offset, 0), LevelObject.transform).GetComponent<Transform>());
        }

        LevelObjects[Count].name = LevelObjects[Count].name.Remove(LevelObjects[Count].name.Length - 7);
        LevelObjects[Count].gameObject.GetComponent<GoalScript>().GoalNumber = Count;
    }

    private static void ResetBools()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Path[i, j] = false;
            }
        }
    }

    public static void GenerateMoves()
    {
        if (Goal.Levels.Contains(LevelObject))
        {
            GameObject[] replace = GameObject.FindGameObjectsWithTag("Wall");
            for (int i = 0; i < replace.Length; i++)
            {
                if (replace[i].GetComponent<TagScript>() != null)
                    Destroy(replace[i]);
            }
            replace = GameObject.FindGameObjectsWithTag("Goal");
            for (int i = 0; i < replace.Length; i++)
            {
                if (replace[i].GetComponent<TagScript>() != null)
                    Destroy(replace[i]);
            }
            Goal.RemoveLevel(Goal.Levels.Count - 1);
        }
        
        Initialized = true;
        Count = 0;
        LevelObjects = new List<Transform>();
        Attempts = 0;

        bool ignore = false;

        if (Size >= 6)
            if (Must != 0)
                Minimum = Must;
        if (Size >= 10)
            Minimum = Must = 0;
        if (Must == 0 && Minimum == 0)
            ignore = true;
        else if (Minimum > Size * Size)
            Minimum = Size * Size;
        else if (Minimum != 0 && Minimum < 4)
            Minimum = 4;
        else if (Must > Size * Size)
            Must = Size * Size;
        else if (Must < 4)
            Must = 4;
        Redo:
        Attempts++;
        ResetBools();
        Seed = "";
        Moves = new List<Vector3>();
        Directions = new List<int>();
        int x = Random.Range(0, Size - 1);
        int z = Random.Range(0, Size - 1);
        x = Size - 1;
        z = Size - 1;
        bool[] possible = new bool[4];
        Moves.Add(Positions[x, z]);
        Path[x, z] = true;
        while (true)
        {
            Seed += ((10 * (z + 1)) + (x + 1)).ToString();

            for (int i = 0; i < 4; i++)
                possible[i] = true;

            if (x == 0 || Path[x - 1, z])
                possible[2] = false;
            if (x == Size - 1 || Path[x + 1, z])
                possible[0] = false;

            if (z == 0 || Path[x, z - 1])
                possible[3] = false;
            if (z == Size - 1 || Path[x, z + 1])
                possible[1] = false;

            int temp = 0;
            for (int i = 0; i < 4; i++)
                if (possible[i])
                    temp++;
            if (temp == 0)
                goto Done;
            int direction = Random.Range(0, temp);
            temp = 0;
            for (int i = 0; i < 4; i++)
            {
                if (possible[i])
                    temp++;
                if (temp == direction + 1)
                {
                    temp = i;
                    break;
                }
            }
            Directions.Add(temp);
            if (temp == 0)
                x++;
            else if (temp == 1)
                z++;
            else if (temp == 2)
                x--;
            else
                z--;
            Moves.Add(Positions[x, z]);
            Path[x, z] = true;
        }
        Done:
        if (ignore)
            goto Pass;
        else if (Moves.Count < Minimum)
            goto Redo;
        else if (Minimum == 0 && Moves.Count != Must)
            goto Redo;
        Pass:
        Debug.Log("Attempts: " + Attempts);
        while (Initialized)
        {
            if (Count == Moves.Count)
            {
                Initialized = false;
                Goal.AddLevel(LevelObject, LevelObjects.ToArray());
                return;
            }
            Build();
            PlaceGoals();
            Count++;
        }
    }
}