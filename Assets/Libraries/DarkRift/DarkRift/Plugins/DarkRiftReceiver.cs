using UnityEngine;
using System.Collections;

using DarkRift;

//This script will recieve data from the Update routine, that's all
public class DarkRiftReceiver : MonoBehaviour {
	
	void Update(){
		if (DarkRiftAPI.isConnected)
			DarkRiftAPI.Receive ();
	}
}
