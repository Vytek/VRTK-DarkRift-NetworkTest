using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour {

	public bool isControllable;

	//This is a really simple script to move the player around,
	//nothing you can't already do :)
	void Update () {
		if( isControllable ){
			//Move
			transform.Translate(0, 0, Input.GetAxis("Vertical"));
			//Rotation
			transform.Rotate(0, Input.GetAxis("Horizontal"), 0 );
		}
	}
}
