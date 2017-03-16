using UnityEngine;
using System.Collections;

public class SyncCharacter : MonoBehaviour
{
	/// <summary>
	/// 	The position we're moving to.
	/// </summary>
	public Vector3 movePosition;

	/// <summary>
	/// 	The speed the unit will move at.
	/// </summary>
	[SerializeField]
	float speed = 5f;

	/// <summary>
	/// 	The ID of the character.
	/// </summary>
	public ushort ID;

	/// <summary>
	///		The ID of the owner of this character.
	/// </summary>
	public ushort owner;

	void Awake ()
	{
		//Make sure move position isn't null on first update
		movePosition = transform.position;
	}

	void Update ()
	{
		//Move towards the destination
		transform.position = Vector3.MoveTowards (transform.position, movePosition, Time.deltaTime * speed);

		//If we own this character and are by the destination then get a new destination
		if (owner == HSNetworkManager.Connection.id && Vector3.SqrMagnitude(transform.position - movePosition) < 1f)
			movePosition = new Vector3(Random.Range(0f, 100f), 0, Random.Range(0f, 100f));
	}
}
