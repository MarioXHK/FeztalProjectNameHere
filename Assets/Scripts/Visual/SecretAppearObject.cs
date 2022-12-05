using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretAppearObject : MonoBehaviour
{
    public GameObject linkedObject;
	
    public float animTime;
	
	public bool KillSelf = true;

    MeshRenderer render;

    float elapsedTime = 0;
    bool active = false;
    bool activatedLinkedObject = false;

    void Start()
    {
        render = GetComponent<MeshRenderer>();
    }


    void Update()
    {
        if (!active) return;
        elapsedTime = Mathf.Min(elapsedTime + Time.unscaledDeltaTime, animTime);

        if(elapsedTime >= animTime * 0.9 && !activatedLinkedObject) {
            linkedObject.SetActive(true);
            activatedLinkedObject = true;
        }

        foreach(Material m in render.materials) {
            m.SetFloat("Progress", elapsedTime / animTime);
        }
    }

    public void Activate() {
        active = true;
        if (KillSelf) Destroy(gameObject, animTime);
		else StartCoroutine(Vanish());
    }
	IEnumerator Vanish() {
		yield return new WaitForSeconds(animTime);
		elapsedTime = 0;
		active = false;
		activatedLinkedObject = false;
	}
}
