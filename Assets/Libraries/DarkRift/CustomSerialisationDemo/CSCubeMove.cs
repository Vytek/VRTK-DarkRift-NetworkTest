using System;
using UnityEngine;

//First thing that is needed is to get access to the DarkRift namespace.
using DarkRift;

public class CSCubeMove : MonoBehaviour {

	//This will identify the cube that's being moved.
	public int cubeID;

	Vector3 lastPos = Vector3.zero;

	void Start(){
		DarkRiftAPI.onData += OnDataRecieved;
	}

	void OnDataRecieved(byte tag, ushort subject, object data){

		//Check it's our's
		if( subject == cubeID ){
			DeserialisePos(data);
		}
	}

	void OnMouseDrag(){
		//When draged we need to tell everyone else what's happening so...

		if( DarkRiftAPI.isConnected ){

			//First get it's new position...
			Vector3 pos = Camera.main.ScreenPointToRay(Input.mousePosition).GetPoint(10);

			//Move it to that position on our screen
			transform.position = pos;

			//Then send it to the others if we've moved enough
			if( Vector3.Distance(lastPos, pos) > 0.05f ){
				SerialisePos(pos);
				lastPos = pos;
			}
		}
	}
	
	void SerialisePos(Vector3 pos)
	{
		//Here is where we actually serialise things manually. To do this we need to add
		//any data we want to send to a DarkRiftWriter. and then send this as we would normally.
		//The advantage of custom serialisation is that you have a much smaller overhead than when
		//the default BinaryFormatter is used, typically about 50 bytes.
		using(DarkRiftWriter writer = new DarkRiftWriter())
		{
			//Next we write any data to the writer, as we never change the z pos there's no need to 
			//send it.
			writer.Write(pos.x);
			writer.Write(pos.y);

			DarkRiftAPI.SendMessageToOthers(0, (ushort)cubeID, writer);
		}
	}

	void DeserialisePos(object data)
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
				//Then read!
				transform.position = new Vector3(
					reader.ReadSingle(),
					reader.ReadSingle(),
					0
				);
			}
		}
		else
		{
			Debug.LogError("Should have recieved a DarkRiftReciever but didn't! (Got: " + data.GetType() + ")");
			transform.position = transform.position;
		}
	}
}
