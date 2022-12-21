using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class AIPopulation : MonoBehaviour
{
    private float TrainingTime;
    private PopulationNetwork Net;
    private Stopwatch timer;
    private Transform[] Goals;
    private int NextGoal = 1;
    private bool Initialized = false;
    private float ForwardSpeed = 8;
    private float TurnSpeed = 200;
    private GameObject[] Exceptions;
    private RaycastHit[] Hits;
    private Vector3 Direction;
    private float[] Distances;
    private float MinimumDistance;
    private float[] Output;

    private void Start()
    {
        Exceptions = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < Exceptions.Length; i++)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), Exceptions[i].gameObject.GetComponent<Collider>(), true);
        }
    }

    private void Update()
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
                if (Distances[i] - 0.5f < MinimumDistance)
                    MinimumDistance = Distances[i] - 0.5f;
            }
            Output = Net.FeedForward(Distances);
            DetermineOutput();
        }
    }

    private void DetermineOutput()
    {
        transform.position += transform.forward * Output[0] * ForwardSpeed * Time.deltaTime;
        transform.eulerAngles += Vector3.up * Output[1] * TurnSpeed * Time.deltaTime;
    }

    private void DetermineCloseness()
    {
        float close = Vector3.Distance(transform.position, Goals[NextGoal].position);
        float max = Vector3.Distance(Goals[NextGoal - 1].position, Goals[NextGoal].position);
        Net.AddFitness((60 * (NextGoal - 1)) + ((60 * (max - close)) / max));
    }

    private float GetTime()
    {
        timer.Stop();
        return timer.ElapsedMilliseconds / 1000f;
    }

    private void DetermineMinDistance()
    {
        Net.AddFitness(MinimumDistance * 24);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Finish")
        {
            timer.Stop();
            PlayerScript.Finished++;
            Net.AddFitness(2000);
            Net.AddFitness(1000 * (TrainingTime - GetTime()) / TrainingTime);
            Net.time = 1000 * (TrainingTime - GetTime()) / TrainingTime;
            if (PlayerScript.Rays)
                Debug.Log(MinimumDistance);
            DetermineMinDistance();
            gameObject.SetActive(false);
            Initialized = false;
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
            gameObject.SetActive(false);
            Initialized = false;
            if (NextGoal == 1)
                Net.AddFitness(-60f * GetTime());
            else
                DetermineCloseness();
        }
    }

    public void Initialize(PopulationNetwork net, Transform[] goals, float time)
    {
        MinimumDistance = 2f;
        Net = net;
        Initialized = true;
        TrainingTime = time;
        NextGoal = 1;
        Goals = new Transform[goals.Length];
        timer = new Stopwatch();
        timer.Start();
        for (int i = 0; i < goals.Length; i++)
        {
            Goals[i] = goals[i];
        }
    }
}