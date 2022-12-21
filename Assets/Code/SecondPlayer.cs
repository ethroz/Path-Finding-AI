using UnityEngine;

public class SecondPlayer : MonoBehaviour
{
    public static bool ChangeLevel;
    private GameObject Respawn;
    private GameObject[] NoCollisionObjects;
    private float ForwardSpeed = 10;
    private float TurnSpeed = 200;

    private void Start()
    {
        Respawn = Goal.Respawn;
        NoCollisionObjects = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < NoCollisionObjects.Length; i++)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), NoCollisionObjects[i].GetComponent<Collider>());
        }
    }

    private void Update()
    {
        float yaw = 0;
        if (Input.GetKey(KeyCode.LeftArrow))
            yaw = -TurnSpeed;
        if (Input.GetKey(KeyCode.RightArrow))
            yaw = TurnSpeed;
        transform.eulerAngles += Vector3.up * yaw * Time.deltaTime;

        float moveSpeed = 0;
        if (Input.GetKey(KeyCode.UpArrow))
            moveSpeed = ForwardSpeed;
        if (Input.GetKey(KeyCode.DownArrow))
            moveSpeed = -ForwardSpeed;

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (ChangeLevel)
        {
            Respawn = Goal.Respawn;
            transform.position = Respawn.transform.position + Respawn.transform.right;
            transform.eulerAngles = Respawn.transform.eulerAngles;
            ChangeLevel = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Finish")
        {
            gameObject.SetActive(false);
            Invoke("Timer", 1f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), collision.gameObject.GetComponent<Collider>());
        }
        if (collision.gameObject.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            Invoke("Timer", 1f);
        }
    }

    private void Timer()
    {
        transform.position = Respawn.transform.position + Respawn.transform.right;
        transform.eulerAngles = Respawn.transform.eulerAngles;
        gameObject.SetActive(true);
    }
}