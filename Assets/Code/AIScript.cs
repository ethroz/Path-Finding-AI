using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class AIScript : MonoBehaviour
{
    public NeuralNetwork Net;
    private Transform[] Goals;
    private Stopwatch timer;
    private int Finishes;
    private int Deaths;
    private int NextGoal;
    private bool Initialized = false;
    private bool Train;
    private float ForwardSpeed = 8;
    private float TurnSpeed = 200;
    private GameObject[] Exceptions;
    private RaycastHit[] Hits;
    private Vector3 Direction;
    private float[] Distances;
    private float[] Output;
    private float[] ExpectedOutput;

    private void Start()
    {
        Exceptions = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < Exceptions.Length; i++)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), Exceptions[i].gameObject.GetComponent<Collider>(), true);
        }
    }

    private void LateUpdate()
    {
        if (Initialized)
        {
            Hits = new RaycastHit[7];
            Distances = new float[7];
            Output = new float[2];
            for (int i = 0; i < Hits.Length; i++)
            {
                Direction = new Vector3(-Mathf.Cos(Mathf.Deg2Rad * (((180 / (Hits.Length - 1)) * i) + transform.eulerAngles.y)), 0,
                    Mathf.Sin(Mathf.Deg2Rad * (((180 / (Hits.Length - 1)) * i) + transform.eulerAngles.y)));

                if (Physics.Raycast(transform.position, Direction, out Hits[i], Mathf.Infinity))
                    if (PlayerScript.Rays)
                        Debug.DrawRay(transform.position, Direction * Hits[i].distance, Color.magenta);

                Distances[i] = Hits[i].distance;
            }

            Output = Net.FeedForward(Distances);
            DetermineOutput();
            if (Train)
                BackPropagate();
        }
    }

    private void DetermineOutput()
    {
        transform.position += transform.forward * Output[0] * ForwardSpeed * Time.deltaTime;
        transform.eulerAngles += Vector3.up * Output[1] * TurnSpeed * Time.deltaTime;
    }

    private void BackPropagate()
    {
        ExpectedOutput = new float[2];

        for (int i = 0; i < ExpectedOutput.Length; i++)
        {
            ExpectedOutput[i] = Background.NetEmulation(Distances)[i];
        }

        if (ExpectedOutput[0] >= 0.98f)
            ExpectedOutput[0] = 1;

        //Debug.Log(ExpectedOutput[0]);
        //Debug.Log(ExpectedOutput[1]);

        Net.BackProp(ExpectedOutput);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Finish")
        {
            timer.Stop();
            Net.Time = (timer.ElapsedMilliseconds / 1000f);
            Debug.Log("Time: " + Net.Time);
            Initialized = false;
            gameObject.SetActive(false);
            Finishes++;
            Deaths = 0;
            if (Finishes < 3)
            {
                Invoke("Timer", 1f);
                return;
            }
            if (Goal.CurrentLevel != Goal.Levels.Count - 1)
                Net.Level = (Goal.CurrentLevel + 1).ToString();
            else
                Net.Level = RandomGenerator.Seed;
            if (Train)
            {
                Background.SaveNewNetwork(Net.GetWeights(), 0, Net.Time, Net.Level);
                if (Goal.CurrentLevel != Goal.Levels.Count - 1)
                    PlayerScript.ChangeLevel();
                else
                {
                    RandomGenerator.GenerateMoves();
                    RandomGenerator.Must++;
                }
                PlayerScript.IsPopTraining = true;
            }
        }
        else if (other.gameObject.CompareTag("Goal"))
        {
            if (NextGoal < other.gameObject.GetComponent<GoalScript>().GoalNumber + 1)
                NextGoal = other.gameObject.GetComponent<GoalScript>().GoalNumber + 1;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), collision.gameObject.GetComponent<Collider>(), true);
        }
        if (collision.gameObject.CompareTag("Wall"))
        {
            timer.Stop();
            Initialized = false;
            gameObject.SetActive(false);
            Deaths++;
            if (Deaths == 5)
            {
                if (!Train)
                    return;
                Debug.Log("Failed");
                PlayerScript.IsPopTraining = true;
                for (int i = 0; i < PlayerScript.BotNets.Count; i++)
                {
                    PlayerScript.BotNets[i].Mutate(20);
                }
                return;
            }
            Invoke("Timer", 1f);
        }
    }

    private void Timer()
    {
        transform.position = Goals[0].position;
        transform.rotation = Goals[0].rotation;
        gameObject.SetActive(true);
        Initialized = true;
        timer = new Stopwatch();
        timer.Start();
    }

    public void Initialize(NeuralNetwork net, Transform[] goals, bool train)
    {
        Net = net;
        Initialized = true;
        NextGoal = 1;
        Train = train;
        Finishes = 0;
        if (!train)
            Finishes = 2;
        Deaths = 0;
        Goals = new Transform[goals.Length];
        for (int i = 0; i < Goals.Length; i++)
        {
            Goals[i] = goals[i];
        }
        transform.position = goals[0].position;
        transform.rotation = goals[0].rotation;
        gameObject.GetComponent<Rigidbody>().freezeRotation = true;
        timer = new Stopwatch();
        timer.Start();
    }
}