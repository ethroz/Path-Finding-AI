using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public static Transform Self;
    private int NextGoal = 1;
    public bool Close = false;
    public static float[] TrainingTime = new float[] { 6, 10, 23, 6, 27 };
    public static int Finished = 0;
    public static bool HasFinished;
    public static bool IsPopTraining;
    private bool IsDeepTraining;
    private int PopulationSize = 100;
    private int CompletionCount = 0;
    public static int GenerationNumber = 0;
    private int[] PopulationLayers = new int[] { 7, 50, 50, 2 };
    private int[] DeepLayers = new int[] { 7, 50, 50, 2 };
    public static int Type = 0;
    public static List<PopulationNetwork> BotNets;
    private PopulationNetwork Best;
    public static NeuralNetwork BestDeepNet;
    public static NeuralNetwork DeepNet;
    private AIScript DeepBot;
    private List<AIPopulation> BotList = null;
    public static bool Rays = false;
    private static Camera BirdEyeView;
    public static List<Vector3> BEVPositions = new List<Vector3>();
    private static Camera ThirdPerson;
    private static GameObject Respawn;
    public GameObject AiBot;
    public GameObject PopulationBot;
    private GameObject[] NoCollisionObjects;
    private float ForwardSpeed = 8;
    private float TurnSpeed = 200;
    private static Transform[] LevelObjects;
    private RaycastHit[] Hits;

    private void Start()
    {
        BirdEyeView = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        ThirdPerson = GameObject.FindWithTag("PlayerCamera").GetComponent<Camera>();
        BirdEyeView.enabled = !Close;
        ThirdPerson.enabled = Close;
        NoCollisionObjects = GameObject.FindGameObjectsWithTag("Player");
        Physics.IgnoreCollision(GetComponent<Collider>(), PopulationBot.GetComponent<Collider>());
        for (int i = 0; i < NoCollisionObjects.Length; i++)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), NoCollisionObjects[i].GetComponent<Collider>());
        }
        for (int i = 0; i < Goal.Levels.Count; i++)
        {
            Vector3 p = Goal.Levels[i].transform.position;
            p.y = 31;
            p.x = -2.5f;
            BEVPositions.Add(p);
        }
        LevelObjects = new Transform[Goal.Goals[Goal.CurrentLevel].Length];
        for (int i = 0; i < LevelObjects.Length; i++)
        {
            LevelObjects[i] = Goal.Goals[Goal.CurrentLevel][i];
        }
        Respawn = LevelObjects[0].gameObject;
        transform.position = Respawn.transform.position;
        transform.rotation = Respawn.transform.rotation;
        Background.FetchAllDocuments();
        Background.FetchAllWeights();
        InitAIPopulationNeuralNetworks(true);
        BestDeepNet = Background.BestDeepNet();
        DeepNet = new NeuralNetwork(DeepLayers, Type);
        DeepBot = Instantiate(AiBot, Respawn.transform.position, Respawn.transform.rotation).GetComponent<AIScript>();
        DeepBot.gameObject.SetActive(false);
    }
	
	private void Update()
    {
        UserInput();
    }

    private void LateUpdate()
    {
        if (Rays)
        {
            Hits = new RaycastHit[7];
            for (int i = 0; i < Hits.Length; i++)
            {
                Vector3 direction = new Vector3(-Mathf.Cos(Mathf.Deg2Rad * (((180 / (Hits.Length - 1)) * i) + transform.eulerAngles.y)), 0,
                    Mathf.Sin(Mathf.Deg2Rad * (((180 / (Hits.Length - 1)) * i) + transform.eulerAngles.y)));

                if (Physics.Raycast(transform.position, direction, out Hits[i], Mathf.Infinity))
                    Debug.DrawRay(transform.position, direction * Hits[i].distance, Color.magenta);
            }
        }
        if (IsPopTraining)
        {
            GenerationNumber++;
            BotNets.Sort();
            Best = BotNets[BotNets.Count - 1];
            if (Finished >= 10)
            {
                Debug.Log("Completed with " + Finished);
                CompletionCount++;
            }
            IsPopTraining = false;
            if (CompletionCount == 5)
            {
                if (Goal.CurrentLevel != Goal.Levels.Count - 1)
                    Background.SaveNewNetwork(Best.weights, 1, Best.time, (Goal.CurrentLevel + 1).ToString());
                Finished = 0;
                CompletionCount = 0;
                IsDeepTraining = true;
                return;
            }
            Finished = 0;
            for (int i = 0; i < PopulationSize; i++)
            {
                BotNets[i] = new PopulationNetwork(BotNets[PopulationSize - 1]);
                if (i == PopulationSize - 1)
                    continue;
                else if (i >= Mathf.Round(19 * PopulationSize / 20))
                    BotNets[i].Mutate(0.5f);
                else if (i >= Mathf.Round(4 * PopulationSize / 5))
                    BotNets[i].Mutate(2);
                else if (i >= Mathf.Round(3 * PopulationSize / 5))
                    BotNets[i].Mutate(5);
                else if (i >= Mathf.Round(PopulationSize * 3 / 10))
                    BotNets[i].Mutate(10);
                else
                    BotNets[i].Mutate(100);
            }

            for (int i = 0; i < PopulationSize; i++)
            {
                BotNets[i].SetFitness(0f);
            }

            if (Goal.CurrentLevel < Goal.Levels.Count - 1)
                Invoke("PopTimer", TrainingTime[Goal.CurrentLevel]);
            else
                Invoke("PopTimer", Mathf.Ceil(0.6f * RandomGenerator.Must));

            BotList = new List<AIPopulation>();

            for (int i = 0; i < PopulationSize; i++)
            {
                AIPopulation AIPop = (Instantiate(PopulationBot, Respawn.transform.position, Respawn.transform.rotation)).GetComponent<AIPopulation>();
                if (Goal.CurrentLevel < Goal.Levels.Count - 1)
                    AIPop.Initialize(BotNets[i], LevelObjects, TrainingTime[Goal.CurrentLevel]);
                else
                    AIPop.Initialize(BotNets[i], LevelObjects, Mathf.Ceil(0.6f * RandomGenerator.Must));
                BotList.Add(AIPop);
            }
        }
        if (IsDeepTraining)
        {
            if (HasFinished)
                Debug.Log("Success!");
            IsDeepTraining = false;
            DeepBot.Initialize(DeepNet, LevelObjects, true);
            DeepBot.gameObject.SetActive(true);
        }
    }

    private void UserInput()
    {
        float yaw = 0;
        float moveSpeed = 0;

        if (Input.GetKeyDown(KeyCode.Delete))
            Debug.ClearDeveloperConsole();
        if (Input.GetKeyDown(KeyCode.C))
            ChangeLevel();
        if (Input.GetKeyDown(KeyCode.Keypad5))
        {
            SetLevel(Goal.Levels.Count - 1);
        }
        if (Input.GetKeyDown(KeyCode.T))
            Test();
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            BirdEyeView.enabled = !BirdEyeView.enabled;
            ThirdPerson.enabled = !ThirdPerson.enabled;
            Close = !Close;
        }
        if (Input.GetKey(KeyCode.A))
            yaw = -TurnSpeed;
        if (Input.GetKey(KeyCode.D))
            yaw = TurnSpeed;
        if (Input.GetKey(KeyCode.W))
            moveSpeed = ForwardSpeed;
        if (Input.GetKey(KeyCode.S))
            moveSpeed = -ForwardSpeed;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.ClearDeveloperConsole();
            IsPopTraining = !IsPopTraining;
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!DeepBot.gameObject.activeSelf)
            {
                DeepBot.Initialize(DeepNet, LevelObjects, false);
                DeepBot.gameObject.SetActive(true);
            }
            else
                DeepBot.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.R))
            Rays = !Rays;
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (Goal.CurrentLevel != Goal.Levels.Count - 1)
                Background.SaveNewNetwork(Best.weights, 1, Best.time, (Goal.CurrentLevel + 1).ToString());
            else
                Background.SaveNewNetwork(Best.weights, 1, Best.time, RandomGenerator.Seed);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (Goal.CurrentLevel != Goal.Levels.Count - 1)
                Background.SaveNewNetwork(DeepBot.Net.GetWeights(), 0, DeepBot.Net.Time, (Goal.CurrentLevel + 1).ToString());
            else
                Background.SaveNewNetwork(DeepBot.Net.GetWeights(), 0, DeepBot.Net.Time, RandomGenerator.Seed);
        }
        if (Input.GetKey(KeyCode.O) && Input.GetKeyDown(KeyCode.Alpha1))
            System.Diagnostics.Process.Start(Background.DeepDocuments[Background.DeepDocuments.Count - 1]);
        if (Input.GetKey(KeyCode.O) && Input.GetKeyDown(KeyCode.Alpha2))
            System.Diagnostics.Process.Start(Background.NaturalDocuments[Background.NaturalDocuments.Count - 1]);
        if (Input.GetKeyDown(KeyCode.B))
            Background.Test();
        if (Input.GetKeyDown(KeyCode.G))
            RandomGenerator.GenerateMoves();
        if (Input.GetKeyDown("`") && BestDeepNet != null)
        {
            if (!DeepBot.gameObject.activeSelf)
            {
                DeepBot.Initialize(BestDeepNet, LevelObjects, false);
                DeepBot.gameObject.SetActive(true);
            }
            else
                DeepBot.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            RandomGenerator.Must++;
            Debug.Log(RandomGenerator.Must);
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            RandomGenerator.Must--;
            Debug.Log(RandomGenerator.Must);
        }

        Self.eulerAngles += Vector3.up * yaw * Time.deltaTime;
        Self.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private void Test()
    {
        Debug.Log("Run Test");
        //Write Code Here
        Debug.Log(RandomGenerator.Seed);
    }

    public static void UpdateLevel()
    {
        LevelObjects = new Transform[Goal.Goals[Goal.CurrentLevel].Length];
        for (int i = 0; i < LevelObjects.Length; i++)
        {
            LevelObjects[i] = Goal.Goals[Goal.CurrentLevel][i];
        }
        Respawn = LevelObjects[0].gameObject;
        Self.position = Respawn.transform.position;
        Self.rotation = Respawn.transform.rotation;
    }

    private void SetLevel(int index)
    {
        Goal.SetLevel(index);

        LevelObjects = new Transform[Goal.Goals[Goal.CurrentLevel].Length];
        for (int i = 0; i < LevelObjects.Length; i++)
        {
            LevelObjects[i] = Goal.Goals[Goal.CurrentLevel][i];
        }
        Respawn = LevelObjects[0].gameObject;
        transform.position = Respawn.transform.position;
        transform.rotation = Respawn.transform.rotation;

        BirdEyeView.transform.position = BEVPositions[Goal.CurrentLevel];
    }

    public static void ChangeLevel()
    {
        Goal.UpdateLevel();

        LevelObjects = new Transform[Goal.Goals[Goal.CurrentLevel].Length];
        for (int i = 0; i < LevelObjects.Length; i++)
        {
            LevelObjects[i] = Goal.Goals[Goal.CurrentLevel][i];
        }
        Respawn = LevelObjects[0].gameObject;
        Self.position = Respawn.transform.position;
        Self.rotation = Respawn.transform.rotation;

        BirdEyeView.transform.position = BEVPositions[Goal.CurrentLevel];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Finish")
        {
            NextGoal = 1;
            gameObject.SetActive(false);
            ThirdPerson.enabled = false;
            BirdEyeView.enabled = true;
            Invoke("Timer", 1f);
        }
        else if (other.gameObject.CompareTag("Goal"))
        {
            if (NextGoal < other.gameObject.GetComponent<GoalScript>().GoalNumber + 1)
                NextGoal = other.gameObject.GetComponent<GoalScript>().GoalNumber + 1;
            Debug.Log(NextGoal);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            BirdEyeView.enabled = true;
            Invoke("Timer", 1f);
        }
    }

    private void Timer()
    {
        gameObject.SetActive(true);
        transform.position = Respawn.transform.position;
        transform.eulerAngles = Respawn.transform.eulerAngles;
        gameObject.GetComponent<Rigidbody>().freezeRotation = true;
        if (Close)
        {
            ThirdPerson.enabled = true;
            BirdEyeView.enabled = false;
        }
    }

    private void PopTimer()
    {
        for (int i = 0; i < BotList.Count; i++)
            if (BotList[i] != null)
                Destroy(BotList[i].gameObject);
        IsPopTraining = true;
    }

    private void InitAIPopulationNeuralNetworks(bool knew)
    {
        if (knew && Background.NaturalWeights.Count > 0)
        {
            Best = new PopulationNetwork(Background.NaturalWeights[0]);
            BotNets = new List<PopulationNetwork>();
            for (int i = 0; i < PopulationSize; i++)
            {
                PopulationNetwork net = new PopulationNetwork(Best.weights);
                if (i == PopulationSize - 1)
                    goto Skip;
                else if (i >= Mathf.Round(19 * PopulationSize / 20))
                    net.Mutate(0.5f);
                else if (i >= Mathf.Round(4 * PopulationSize / 5))
                    net.Mutate(2);
                else if (i >= Mathf.Round(3 * PopulationSize / 5))
                    net.Mutate(5);
                else if (i >= Mathf.Round(PopulationSize * 3 / 10))
                    net.Mutate(10);
                else
                    net.Mutate(100);
                Skip:
                BotNets.Add(net);
            }
            return;
        }
        if (PopulationSize % 2 != 0)
        {
            PopulationSize = 2;
        }

        BotNets = new List<PopulationNetwork>();

        for (int i = 0; i < PopulationSize; i++)
        {
            PopulationNetwork net = new PopulationNetwork(PopulationLayers);
            net.Mutate(100);
            BotNets.Add(net);
        }
    }
}