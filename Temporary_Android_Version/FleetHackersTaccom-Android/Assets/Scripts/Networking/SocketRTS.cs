using SocketIO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocketRTS : MonoBehaviour {


    [SerializeField]
    SocketIOComponent socket;

    [SerializeField]
    Transform testSpawn;

    [SerializeField]
    List<BasicShip> trackedShips;

    // Use this for initialization
    void Start () {
        socket.On("intialize", OnConnect);

        socket.On("spawn-ship", SpawnShip);

        StartCoroutine(DelayedEmit());
    }

    private void SpawnShip(SocketIOEvent se)
    {
        Debug.Log("ship spawn ordered " + se.data.Print());

        //obj.data
        var spawnData = SIMessage.FromJSO<SpawnData>(se.data.Print());
        Debug.Log("ship spawned " + spawnData.ToString());

        Instantiate(testSpawn, 
            new Vector3(spawnData.position.x, 0, spawnData.position.y), Quaternion.identity);
    }

    void OnConnect(SocketIOEvent se)
    {
        Debug.Log("client connected to server." + se.data.str);
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

[Serializable]
public class SpawnData
{
    public position position;

    public string shipId;

    public override string ToString()
    {
        return "pos x: " + position.x + " y: " + position.y;
    }
}

[Serializable]
public class position
{
    public float x;
    public float y;
}