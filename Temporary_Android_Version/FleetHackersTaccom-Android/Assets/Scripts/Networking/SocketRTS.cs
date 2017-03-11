using SocketIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketRTS : MonoBehaviour {


    [SerializeField]
    SocketIOComponent socket;

    // Use this for initialization
    void Start () {
        socket.On("hello", OnConnect);
        socket.Emit("response");
    }
	
    void OnConnect(SocketIOEvent e)
    {
        Debug.Log("client connected to server.");
    }

	// Update is called once per frame
	void Update () {
		
	}
}
