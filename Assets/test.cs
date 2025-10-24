using UnityEngine;

public class test : MonoBehaviour
{
    public GameObject testObject;
    public void Start()
    {
        //Debug.Log("This is a log message.");
        //Debug.LogWarning("This is a warning message.");
        //Debug.LogError("This is an error message.");
        testObject.SetActive(false);
    }
}
