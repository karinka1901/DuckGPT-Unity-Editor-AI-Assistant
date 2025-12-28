using UnityEngine;

public class test : MonoBehaviour
{
    public GameObject testObject;
    public Rigidbody testRigidbody;
    public Animator testAnimator;
    public void Start()
    {
        testAnimator.Play("Ducsd");

        //Debug.Log("This is a log message.");
        //Debug.LogWarning("This is a warning message.");
        //Debug.LogError("This is an error message.");
        // testObject.SetActive(false);
        testRigidbody = GetComponent<Rigidbody>();
        testRigidbody.useGravity = false;

        testObject = GetComponentInChildren<GameObject>();
    
        
    }

    public void Update()
    {
        testObject.transform.Rotate(Vector3.up, 20 * Time.deltaTime);
    }


}
