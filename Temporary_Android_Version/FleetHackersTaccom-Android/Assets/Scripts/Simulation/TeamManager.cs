using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamManager : MonoBehaviour {

    Dictionary<int, BasicShip> shipBank;

    List<TeamStance> teamDatabase;

    void Awake()
    {
        shipBank = new Dictionary<int, BasicShip>();
        teamDatabase = new List<TeamStance>();
    }

    // Use this for initialization
    void Start ()
    {

    }
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void RegisterShip(BasicShip ship)
    {

    }
}
