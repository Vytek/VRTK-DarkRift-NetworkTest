using UnityEngine;
using System.Collections;
//Access the DarkRift namespace
using DarkRift;

public class NetworkManagerVRTK : MonoBehaviour
{

	/// <summary>
	/// 	The IP to connect to.
	/// </summary>
	public string serverIP = "127.0.0.1";

	/// <summary>
	/// 	The port to conenct to.
	/// </summary>
	public int serverPort = 4296;

	//The player that we will instantiate when someone joins.
	public GameObject playerObject;

	//A reference to our player
	Transform player;

	void Start ()
	{
		//Enable Background Running
		Application.runInBackground = true;

		//Enable custom log
		CustomLogger.LogIt("Start Logging");
		//Application.dataPath
		Debug.Log(Application.dataPath);

		//Connect to the DarkRift Server using the Ip specified (will hang until connected or timeout)
		DarkRiftAPI.Connect (serverIP, serverPort);
		//Setup a receiver so we can create players when told to.
		DarkRiftAPI.onDataDetailed += ReceiveData;

		//Tell others that we've entered the game and to instantiate a player object for us.
		if (DarkRiftAPI.isConnected)
		{
			Debug.Log("Connected to the Server!");

			//Get everyone else to tell us to spawn them a player (this doesn't need the data field so just put whatever)
			DarkRiftAPI.SendMessageToOthers (TagIndex.Controller, TagIndex.ControllerSubjects.JoinMessage, "hi");
			//Then tell them to spawn us a player! (this time the data is the spawn position)
			DarkRiftAPI.SendMessageToAll (TagIndex.Controller, TagIndex.ControllerSubjects.SpawnPlayer, new Vector3(0f,0f,0f));
		}
		else
		{
			Debug.LogError ("Failed to connect to DarkRift Server!");
		}
	}

	void OnApplicationQuit ()
	{
		//You will want this here otherwise the server wont notice until someone else sends data to this
		//client.
		DarkRiftAPI.Disconnect ();
	}

	void ReceiveData (ushort senderID, byte tag, ushort subject, object data){
		//When any data is received it will be passed here, 
		//we then need to process it if it's got a tag of 0 and, if 
		//so, create an object. This is where you'd handle most admin 
		//stuff like that.

		//Ok, if data has a Controller tag then it's for us
		if (tag == TagIndex.Controller)
		{
			//If a player has joined tell them to give us a player
			if (subject == TagIndex.ControllerSubjects.JoinMessage)	
			{
				//Basically reply to them.
				DarkRiftAPI.SendMessageToID (senderID, TagIndex.Controller, TagIndex.ControllerSubjects.SpawnPlayer, player.position);
				Debug.Log("Replay to ID: "+senderID.ToString()+" "+player.position.ToString());
			}

			//Then if it has a spawn subject we need to spawn a player
			if (subject == TagIndex.ControllerSubjects.SpawnPlayer)
			{
				//Instantiate the player
				GameObject clone = (GameObject)Instantiate (playerObject, (Vector3)data, Quaternion.identity);
				//Tell the network player who owns it so it tunes into the right updates.
				//clone.GetComponent<PlayerAvatarVR>().ObjectID = senderID;
				//clone.GetComponentInChildren<NetworkPlayerVTRK>().ObjectID = (ushort)(senderID*100);
				clone.transform.Find("Head").gameObject.GetComponent<NetworkPlayerVTRK>().ObjectID = (ushort)(senderID*100);
				clone.transform.Find("RightHand").gameObject.GetComponent<NetworkPlayerVTRK>().ObjectID = (ushort)(senderID*100 + 1);
				clone.transform.Find("LeftHand").gameObject.GetComponent<NetworkPlayerVTRK>().ObjectID = (ushort)(senderID*100 + 2);
				//How to ObjectID for Head, RightHand, LeftHand?

				//If it's our player being created allow control and set the reference
				if (senderID == DarkRiftAPI.id)
				{
					player = clone.transform;
					player.GetComponent<AvatarCameraRigSync> ().enabled = true;
					Debug.Log("SpawnPlayer ObjectID: "+clone.GetComponentInChildren<NetworkPlayerVTRK>().ObjectID.ToString()+" in position "+data.ToString());
				}
			}
		}
	}
}
