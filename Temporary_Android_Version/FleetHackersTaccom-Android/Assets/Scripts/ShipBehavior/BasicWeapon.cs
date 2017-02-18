using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicWeapon : MonoBehaviour {

    [SerializeField]
    Transform projectile;

    [SerializeField]
    Transform spawnPoint;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
	    if(Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(projectile, spawnPoint.position, spawnPoint.rotation);
        }	
	}
}
