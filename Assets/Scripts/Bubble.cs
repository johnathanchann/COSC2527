using Globals;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public float expiry = 2f;

    void Start()
    {
        Destroy(gameObject, expiry);

    }

    void OnCollisionEnter2D(Collision2D collision)
    {

        if (!collision.gameObject.CompareTag("Frog") && !collision.gameObject.CompareTag("Bubble"))
        {
            Debug.Log("Ball hit " + collision.gameObject.name);
            Destroy(gameObject);
        }
    }


}