using UnityEngine;
using System.Collections.Generic;

using DarkRift;

public class ObjectSpawner : MonoBehaviour
{
	//The tags we will data with
	const byte SPAWN_TAG = 0;
	const byte DESPAWN_TAG = 1;

	/// <summary>
	///		The character prefab to spawn.
	/// </summary>
	[SerializeField]
	GameObject syncCharacterPrefab;

	/// <summary>
	/// 	The characters that we have spawned addressed by their ID.
	/// </summary>
	Dictionary<ushort, SyncCharacter> characters = new Dictionary<ushort, SyncCharacter>();

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

	/// <summary>
	/// 	Handles messages received from DarkRift.
	/// </summary>
	/// <param name="tag">The tag.</param>
	/// <param name="subject">The subject.</param>
	/// <param name="data">The data.</param>
	void HandleOnData (byte tag, ushort subject, object data)
	{
		//If the message was a spawn message
		if (tag == SPAWN_TAG)
		{
			//The data is a reader (may want an added safety check here)
			using (DarkRiftReader reader = data as DarkRiftReader)
			{
				//Get the number of characters we will be spawning
				ushort count = reader.ReadUInt16();

				for (int i = 0; i < count; i++)
				{
					//Read the position of the character from the reader and spawn it
					Vector3 pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
					GameObject clone = (GameObject)Instantiate(syncCharacterPrefab, pos, Quaternion.identity);

					//Setup ID and owner of the SyncCharacter
					SyncCharacter character = clone.GetComponent<SyncCharacter>();
					character.ID = reader.ReadUInt16();
					character.owner = reader.ReadUInt16();

					//Add to the list of characters
					characters.Add(character.ID, character);
				}
			}
		}

		//If the message is a despawn tag
		else if (tag == DESPAWN_TAG)
		{
			//The data is a reader (may want an added safety check here)
			using (DarkRiftReader reader = data as DarkRiftReader)
			{
				//Count the number to despawn
				ushort count = reader.ReadUInt16();

				//Despawn the characters passed to us
				for (int i = 0; i < count; i++)
				{
					Destroy(characters[reader.ReadUInt16()].gameObject);
				}
			}
		}
	}

	/// <summary>
	/// 	Indexer for the characters dictionary.
	/// </summary>
	/// <param name="id">The character's ID.</param>
	public SyncCharacter this[ushort id]
	{
		get
		{
			return characters[id];
		}
	}

	/// <summary>
	/// 	Gets the characters that belong to this client.
	/// </summary>
	/// <returns>The SyncCharacters we own.</returns>
	public SyncCharacter[] GetOurs()
	{
		List<SyncCharacter> ours = new List<SyncCharacter>();

		foreach (SyncCharacter character in characters.Values)
		{
			if (character.owner == HSNetworkManager.Connection.id)
				ours.Add(character);
		}

		return ours.ToArray();
	}
}
