using UnityEngine;
using System.Collections;

using DarkRift;

public class NetworkPlayer : MonoBehaviour {

	//The ID of the client that owns this player (so we can check if it's us updating)
	public ushort networkID;

	Vector3 lastPosition;
	Quaternion lastRotation;

	void Start(){
		//Tell the network to pass data to our RecieveData function so we can process it.
		DarkRiftAPI.onDataDetailed += RecieveData;

		//Also, make sure we're told if a player disconnects.
		DarkRiftAPI.onPlayerDisconnected += PlayerDisconnected;
	}

	void Update(){
		//Only send data if we're connected and we own this player
		if( DarkRiftAPI.isConnected && DarkRiftAPI.id == networkID ){
			//We're going to use a tag of 1 for movement messages
			//If we're conencted and have moved send our position with subject 0.
			if( transform.position != lastPosition )
				DarkRiftAPI.SendMessageToOthers(TagIndex.PlayerUpdate, TagIndex.PlayerUpdateSubjects.Position, transform.position);
			//Then send our rotation with subject 1 if we've rotated.
			if( transform.rotation != lastRotation )
				DarkRiftAPI.SendMessageToOthers(TagIndex.PlayerUpdate, TagIndex.PlayerUpdateSubjects.Rotation, transform.rotation);

			//Update stuff
			lastPosition = transform.position;
			lastRotation = transform.rotation;
		}
	}

	void RecieveData(ushort senderID, byte tag, ushort subject, object data){
		//Right then. When data is recieved it will be passed here, 
		//we then need to process it if it's got a tag of 1 or 2 
		//(the tags for position and rotation), check it's for us 
		//and update ourself.

		//The catch is we need to do this quite quickly because data
		//is going to be comming in thick and fast and it'll create 
		//lag if we spend time here.

		//If the data is about us, process it.
		if( senderID == networkID ){

			//If it has a PlayerUpdate tag then...
			if( tag == TagIndex.PlayerUpdate ){

				//...update our position
				if( subject == TagIndex.PlayerUpdateSubjects.Position ){
					transform.position = (Vector3)data;
				}

				//...update our rotation
				if( subject == TagIndex.PlayerUpdateSubjects.Rotation ){
					transform.rotation = (Quaternion)data;
				}
			}
		}
	}

	void PlayerDisconnected(ushort ID){
		// This will be called when a player disconnects, if it's the client we represent
		// we need to get rid of this object.

		if( ID == networkID ){
			Destroy(gameObject);
		}
	}
}
