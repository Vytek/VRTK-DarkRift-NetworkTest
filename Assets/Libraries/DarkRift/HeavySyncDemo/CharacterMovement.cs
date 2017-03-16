using UnityEngine;
using System.Collections.Generic;

using DarkRift;

[RequireComponent(typeof(ObjectSpawner))]
public class CharacterMovement : MonoBehaviour
{
	/// <summary>
	/// 	The tag for synchronisation data.
	/// </summary>
	const byte SYNC_TAG = 2;

	/// <summary>
	/// 	The time at which we will next send sync data.
	/// </summary>
	float nextSync = 0f;

	/// <summary>
	/// 	The object spawner on this GameObject.
	/// </summary>
	ObjectSpawner objectSpawner;

	//Subscribe to events in OnEnable
	void OnEnable()
	{
		HSNetworkManager.Connection.onData += HandleOnData;
	}
	
	//Unsubscribe in OnDisable for safety
	void OnDisable()
	{
		HSNetworkManager.Connection.onData -= HandleOnData;
	}

	void Awake()
	{
		objectSpawner = GetComponent<ObjectSpawner>();
	}

	/// <summary>
	/// 	Called when data is received by this character.
	/// </summary>
	/// <param name="tag">The tag.</param>
	/// <param name="subject">The subject.</param>
	/// <param name="data">The data.</param>
	void HandleOnData (byte tag, ushort subject, object data)
	{
		//If it's a Sync tag we need to sync!
		if (tag == SYNC_TAG)
		{
			using (DarkRiftReader reader = data as DarkRiftReader)
			{
				//Get the number of characters this sync information has
				ushort count = reader.ReadUInt16();

				//Update the move position with the received position for each character
				for (int i = 0; i < count; i++)
				{
					//Read the character's ID
					ushort id = reader.ReadUInt16();

					try
					{
						//Set move position
						objectSpawner[id].movePosition = 
							new Vector3(
								reader.ReadSingle(),
								reader.ReadSingle(),
								reader.ReadSingle()
								);
					}
					catch (KeyNotFoundException)
					{
						//Probably not aware of them yet!
					}
				}
			}
		}
	}

	void Update ()
	{
		//Every 0.1 seconds send sync data
		if (Time.time >= nextSync)
		{
			//Create a writer
			using (DarkRiftWriter writer = new DarkRiftWriter())
			{
				//Get the characters we own
				SyncCharacter[] ourCharacters = objectSpawner.GetOurs();

				writer.Write((ushort)ourCharacters.Length);

				foreach (SyncCharacter character in ourCharacters)
				{
					writer.Write(character.ID);
					writer.Write(character.transform.position.x);
					writer.Write(character.transform.position.y);
					writer.Write(character.transform.position.z);
				}

				//Send message
				HSNetworkManager.Connection.SendMessageToOthers(SYNC_TAG, 0, writer);
			}

			nextSync = Time.time + 0.1f;
		}
	}
}
