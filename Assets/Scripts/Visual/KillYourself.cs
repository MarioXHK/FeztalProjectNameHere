using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillYourself : MonoBehaviour
{
    public bool DEBUG = false;

    Vector3 startPos;
    public void Execute()
    {
        if (DEBUG) Debug.Log("Destroying Self.");
		Destroy(gameObject);
    }
}
