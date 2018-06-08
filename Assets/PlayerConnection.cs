using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerConnection : NetworkBehaviour 
{
	public GameObject playerObject;
	
	private void Start ()
	{
		if (isLocalPlayer)
		{
			CmdspawnPlayer();
		}
	}

	[Command]
	public void CmdspawnPlayer()
	{
		GameObject aPlayer = Instantiate(playerObject);
		NetworkServer.SpawnWithClientAuthority(aPlayer, connectionToClient);
	}
	
	void Update ()
	{
		if (!hasAuthority) return;
		
	}
}
 