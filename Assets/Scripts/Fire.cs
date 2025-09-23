using Globals;
using UnityEngine;

public class Fire : MonoBehaviour
{
    public float expiry = 2f;

    void Start()
    {
        Destroy(gameObject, expiry);

    }

    void OnCollisionEnter2D(Collision2D collision)
    {

            Debug.Log("Fire hit " + collision.gameObject.name);
            Destroy(gameObject);
        
    }


}