using UnityEngine;

public class TestScriptOne : MonoBehaviour
{
    private Rigidbody testRb;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        TestFunction();
    }

    public void TestFunction()
    {
        testRb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
    }
}
