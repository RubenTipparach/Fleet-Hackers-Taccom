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
    void Start()
    {
        StartCoroutine(BroadcastShipPosition());
    }

    private IEnumerator BroadcastShipPosition()
    {

        while (true)
        {
            yield return new WaitForSeconds(.2f);

            socket.Emit("move-ship", SIMessage.ToJSO(
                        new NetworkShipPosition
                        {
                            shipId = this.shipId,
                            x = transform.position.x,
                            y = transform.position.z

                        }));
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}


[Serializable]
public class NetworkShipPosition
{
    public string shipId;

    public float x;

    public float y;
}