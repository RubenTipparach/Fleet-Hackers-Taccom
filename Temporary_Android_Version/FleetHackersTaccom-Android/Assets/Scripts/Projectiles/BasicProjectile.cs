﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicProjectile : MonoBehaviour {

    [SerializeField]
    float projectileSpeed;

    [SerializeField]
    float lifeTime;

    float timePassed;

	// Use this for initialization
	void Start()
    {
    }
	
	// Update is called once per frame
	void Update()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * projectileSpeed, Space.Self);

        timePassed += Time.deltaTime;
        
        if(timePassed >= lifeTime)
        {
            Destroy(transform.gameObject);
        }	
	}
}
