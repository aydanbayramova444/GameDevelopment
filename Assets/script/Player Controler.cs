using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [SerializeField]private float speed;
    [SerializeField] private int maxHealt;
    [SerializeField] private bool IsDead;
    [SerializeField] private string PlayerName;
    public int scoreValue = 0;

    private Transform playerTranform;
    public Rigidbody characterBody;
    public GameObject player;

    private void Awake()
    {
        playerTranform = GetComponent<Transform>();
        characterBody = GetComponent<Rigidbody>();
    
    }
    private void Start()
    {
        Debug.Log("Player Name: " + PlayerName);
    }

    void Update()
    {

    }

    void FixedUpdate()
    {

    }

    private void OnTriggerEnter(Collider other)
{
    Debug.Log("Something touched: " + other.name);

    if (other.CompareTag("Coin"))
    {
        scoreValue += 10;
        Debug.Log("Score: " + scoreValue);

        Destroy(other.gameObject);
    }
}

}
