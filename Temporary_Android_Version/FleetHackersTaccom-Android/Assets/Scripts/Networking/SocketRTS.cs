using SocketIO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketRTS : MonoBehaviour {


    [SerializeField]
    SocketIOComponent socket;


    // Use this for initialization
    void Start () {
        socket.On("intialize", OnConnect);

        socket.On("spawn-ship", SpawnShip);

        StartCoroutine(DelayedEmit());
    }

    private void SpawnShip(SocketIOEvent se)
    {
        //obj.data
    }

    void OnConnect(SocketIOEvent se)
    {
        Debug.Log("client connected to server.");
    }


    public IEnumerator DelayedEmit()
    {
        yield return new WaitForSeconds(1);

        socket.Emit("register-unity-server",
            SIMessage.ToJSO(
                new RTSServer { serverId = "rubens-srever" }));
    }

    // Update is called once per frame
    void Update () {

	}
}

public static class SIMessage
{
    public static JSONObject ToJSO<T>(T input)
    {
        return JSONObject.Create(JsonUtility.ToJson(input));
    }

    public static T FromJSO<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }
}

[Serializable]
public class RTSServer
{
    public string serverId;
}

