using UnityEngine;
using System.Collections;

using DarkRift;

public class HSNetworkManager : MonoBehaviour
{
	/// <summary>
	/// 	The connection to the server.
	/// </summary>
	public static DarkRiftConnection Connection = new DarkRiftConnection();

	/// <summary>
	/// 	The IP to connect to.
	/// </summary>
	[SerializeField]
	string IP = "127.0.0.1";

	/// <summary>
	/// 	The port to conenct to.
	/// </summary>
	[SerializeField]
	int port = 4296;

	void Start ()
	{
		//Connect to the server
		Connection.Connect(IP, port);
	}

	void Update()
	{
		//Every frame we must call receive to invoke the events from the main thread
		Connection.Receive();
	}

	void OnDisable()
	{
		//Disconnect on disable
		Connection.Disconnect();
	}
}
