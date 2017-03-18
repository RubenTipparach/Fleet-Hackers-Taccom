using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
using System;

public class BasicNetworkShip : MonoBehaviour {


    [SerializeField]
    public SocketIOComponent socket;

    public string shipId;
    // Use this for initialization
    void Start () {
		socket.Emit("move-ship", SIMessage.ToJSO(new NetworkShipPosition()));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}


[Serializable]
public class NetworkShipPosition
{
    public string shipId;
}