using UnityEngine;
using Random = UnityEngine.Random;

public class TestRot : MonoBehaviour
{
    private Vector3 spin;

    private void Start()
    {
        spin = new Vector3(
            Random.Range(-50, 50),
            Random.Range(-50, 50),
            Random.Range(-50, 50)
        );
    }

    void Update()
    {
        transform.Rotate(spin * 1f/60f);
    }
}