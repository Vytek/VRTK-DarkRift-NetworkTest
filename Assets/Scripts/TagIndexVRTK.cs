using UnityEngine;
using System.Collections;

// We reccomend that for each project you keep tags and subjects in a class to make
// your life simpler. Here's how we usually do it:

public class TagIndexVRTK {
	public const int Controller = 0;		//For controller ralated things (think creating new players etc)
	public const int PlayerUpdate = 1;		//For player changing things like change of animation/position/rotation etc
	public const int ObjectUpdate = 2;		//For Object update /position/rotation

	// By the way, there's no problem with using enums for the following,
	// it's just casting can be confusing so we dont.

	public class ControllerSubjects{
		public const int JoinMessage = 0;			//Tells everyone we've joined and need to know who's there.
		public const int SpawnPlayer = 1;			//Tell people to spawn a new player for us.
	}

	public class PlayerUpdateSubjects{
		public const int Position = 0;		//Move the player to (Vector3)Data
		public const int Rotation = 1;		//Rotate the player to (Quaternion)Data
		public const int Scale = 2;			//Scale the player to (Vector3)Data
	}

	public class ObjectUpdateSubjects{
		public const int Position = 0;		//Move the object to (Vector3)Data
		public const int Rotation = 1;		//Rotate the object to (Quaternion)Data
		public const int Scale = 2;			//Scale the object to (Vector3)Data
		public const int PosRot = 3;		//Scale the object to (Vector3)Data/(Quaternion)Data
	}
}