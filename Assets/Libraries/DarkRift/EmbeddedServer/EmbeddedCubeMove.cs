using System;
using UnityEngine;

//First thing that is needed is to get access to the DarkRift namespace.
using DarkRift;

//The only thing that's different to the standard script is that we send to the server rather than others.
public class EmbeddedCubeMove : MonoBehaviour
{
	//This will identify the cube that's being moved.
	public int cubeID;

	Vector3 lastPos = Vector3.zero;

	void Start()
	{
		DarkRiftAPI.onData += OnDataReceived;
	}

	void OnDataReceived(byte tag, ushort subject, object data)
	{
		//Check it's our's
		if( subject == cubeID ){
			transform.position = (Vector3)data;
		}
	}

	void OnMouseDrag()
	{
		//When draged we need to tell everyone else what's happening so...
		if( DarkRiftAPI.isConnected )
		{
			//First get it's new position...
			Vector3 pos = Camera.main.ScreenPointToRay(Input.mousePosition).GetPoint(10);

			//Move it to that position on our screen
			transform.position = new Vector3(pos.x, pos.y, 0f);

			//Then send it to the others if we've moved enough
			if( Vector3.Distance(lastPos, pos) > 0.05f )
			{
				DarkRiftAPI.SendMessageToServer(0, (ushort)cubeID, new Vector3(pos.x, pos.y, 0f));
				lastPos = pos;
			}
		}
	}
}
