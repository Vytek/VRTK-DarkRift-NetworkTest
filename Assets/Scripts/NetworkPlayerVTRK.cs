using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;

public class NetworkPlayerVTRK : MonoBehaviour {

	//The ID of the ObjectGame
	public ushort ObjectID;
	public bool DEBUG = true;
	public float InterRate = 9;
	public float ThresholdPosition = 0.05f;
	public float ThresholdRotation = 0.3f;

	Vector3 lastPosition = Vector3.zero;
	Quaternion lastRotation = Quaternion.identity;
	Vector3 lastScale;

	void Start()
	{
		//Tell the network to pass data to our RecieveData function so we can process it.
		DarkRiftAPI.onDataDetailed += RecieveData;

		//Also, make sure we're told if a player disconnects.
		DarkRiftAPI.onPlayerDisconnected += PlayerDisconnected;

		//Initialize
		lastPosition = transform.position;
		lastRotation = transform.rotation;
	}

	void Update()
	{
		//Only send data if we're connected
		if( DarkRiftAPI.isConnected )
		{
			if ((Vector3.Distance (transform.position, lastPosition) > ThresholdPosition) || (Quaternion.Angle (transform.rotation, lastRotation) > ThresholdRotation))
			{
				if (DEBUG) {
					float dist = Vector3.Distance (transform.position, lastPosition);
					float difa = Quaternion.Angle (transform.rotation, lastRotation);
					Debug.Log ("Distance: " + dist + " Angle: " + difa);
				}

				SerialisePosRot (Vector3.Lerp (transform.position, lastPosition, Time.deltaTime * InterRate), Quaternion.Lerp (transform.rotation, lastRotation, Time.deltaTime * InterRate), ObjectID);
				//SerialisePosRot (transform.position, transform.rotation, ObjectID, isKinematic);

				if (DEBUG) {
					Debug.Log ("ObjectID: " + ObjectID.ToString ());
					Debug.Log ("DarkRiftID: " + DarkRiftAPI.id.ToString ());
				}

				//Update stuff
				lastPosition = transform.position;
				lastRotation = transform.rotation;

			}
		}
	}
		
	void RecieveData(ushort senderID, byte tag, ushort subject, object data)
	{
		//Right then. When data is recieved it will be passed here, 
		//we then need to process it if it's got a tag of 3
		//(the tags for position and rotation), check it's for us 
		//and update ourself.

		//The catch is we need to do this quite quickly because data
		//is going to be comming in thick and fast and it'll create 
		//lag if we spend time here.

		//If it has a ObjectUpdate tag then...
		if( tag == TagIndexVRTK.PlayerUpdate ){

			//...update our position and rotation
			if( subject == ObjectID){

				DeserialisePosRot (data);
			}
		}
	}

	void SerialisePosRot(Vector3 pos, Quaternion rot, ushort ID)
	{
		//Here is where we actually serialise things manually. To do this we need to add
		//any data we want to send to a DarkRiftWriter. and then send this as we would normally.
		//The advantage of custom serialisation is that you have a much smaller overhead than when
		//the default BinaryFormatter is used, typically about 50 bytes.
		using(DarkRiftWriter writer = new DarkRiftWriter())
		{
			//Next we write any data to the writer
			writer.Write(pos.x);
			writer.Write(pos.y);
			writer.Write(pos.z);
			writer.Write(rot.x);
			writer.Write(rot.y);
			writer.Write(rot.z);
			writer.Write(rot.w);

			DarkRiftAPI.SendMessageToOthers(TagIndexVRTK.PlayerUpdate, ID, writer);
			if (DEBUG) {
				Debug.Log ("Data sent: " + pos.ToString ("F4") + " " + rot.ToString ("F6"));
			}
		}
	}

	void DeserialisePosRot(object data)
	{
		//Here we decode the stream, the data will arrive as a DarkRiftReader so we need to cast to it
		//and then read the data off in EXACTLY the same order we wrote it.
		if( data is DarkRiftReader )
		{
			//Cast in a using statement because we are using streams and therefore it 
			//is important that the memory is deallocated afterwards, you wont be able
			//to use this more than once though.
			using(DarkRiftReader reader = (DarkRiftReader)data)
			{
				//Then read and
				//update is for this object
				try
				{
					transform.position = new Vector3 (
						reader.ReadSingle (),
						reader.ReadSingle (),
						reader.ReadSingle ()
					);

					if (DEBUG) {
						Debug.Log ("Position Readed");
					}

					transform.rotation = new Quaternion (
						reader.ReadSingle (),
						reader.ReadSingle (),
						reader.ReadSingle (),
						reader.ReadSingle ()
					);

					if (DEBUG) {
						Debug.Log ("ObjectID: "+ObjectID.ToString());
						Debug.Log ("Data recieved:" + transform.position.ToString ("F4") + " " + transform.rotation.ToString ("F6"));
					}
				}
				catch (KeyNotFoundException)
				{
					//Probably not aware of them yet!
				}
			}
		}
		else
		{
			Debug.LogError("Should have recieved a DarkRiftReciever but didn't! (Got: " + data.GetType() + ")");
			transform.position = transform.position;
			transform.rotation = transform.rotation;
		}
	}

	void PlayerDisconnected(ushort ID){
		// This will be called when a player disconnects, if it's the client we represent
		// we need to get rid of this object.

		if( ID == ObjectID ){
			Destroy(gameObject);
		}
	}
}
