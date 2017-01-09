using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour {

	public BasicShip ship;

	Slider sliderVal;

	void Awake()
	{
		sliderVal = GetComponent<Slider>();
	}

	// Use this for initialization
	void Start () {
    }
	
	// Update is called once per frame
	void Update () {
		sliderVal.value = ship.currentHealth;
	}


	public void SetInitVal(float val)
	{
		sliderVal.value = val;
	}
}
