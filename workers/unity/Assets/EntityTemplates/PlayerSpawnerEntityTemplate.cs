using Improbable.General;
using Improbable.Math;
using Improbable.Player;
using Improbable.Unity.Core.Acls;
using Improbable.Worker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnerEntityTemplate : MonoBehaviour
{
	// Template definition for a PlayerSpawner entity
	public static SnapshotEntity GeneratePlayerSpawnerSnapshotEntityTemplate()
	{
		// Set name of Unity prefab associated with this entity
		var playerSpawner = new SnapshotEntity { Prefab = "PlayerSpawn" };

		// Define components attached to snapshot entity
		playerSpawner.Add(new WorldTransform.Data(new WorldTransformData(new Coordinates(-5, 10, 0), 0)));
		playerSpawner.Add(new Spawner.Data(new SpawnerData()));

		// Grant FSim (server-side) workers write-access over all of this entity's components, read-access for visual (e.g. client) workers
		var acl = Acl.GenerateServerAuthoritativeAcl(playerSpawner);
		playerSpawner.SetAcl(acl);

		return playerSpawner;
	}
}