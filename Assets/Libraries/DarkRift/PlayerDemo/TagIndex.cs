using UnityEngine;
using System.Collections;

// We reccomend that for each project you keep tags and subjects in a class to make
// your life simpler. Here's how we usually do it:

public class TagIndex {
	public const int Controller = 0;		//For controller ralated things (think creating new players etc)
	public const int PlayerUpdate = 1;		//For player changing things like change of animation/position/rotation etc

	// By the way, there's no problem with using enums for the following,
	// it's just casting can be confusing so we dont.

	public class ControllerSubjects{
		public const int JoinMessage = 0;			//Tells everyone we've joined and need to know who's there.
		public const int SpawnPlayer = 1;			//Tell people to spawn a new player for us.
	}

	public class PlayerUpdateSubjects{
		public const int Position = 0;		//Move the player to (Vector3)Data
		public const int Rotation = 1;		//Rotate the player to (Quaternion)Data
	}
}