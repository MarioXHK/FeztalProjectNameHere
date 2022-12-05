using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnYourself : MonoBehaviour
{
    public GameObject linkedObject;

    Vector3 startPos;
    public void Respawn()
    {
        gameObject.transform.position = linkedObject.transform.position;
		gameObject.SetActive(false);
		gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
    }
}
