using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class EarthBending : MonoBehaviour {

	public float earthPower = 100f;
	public ParticleSystem targetingArea;
	public float targetingSpeed = 3f;
	public Transform earthFocus;
	public Rigidbody smallRock;
	public GameObject rockPillar;

	bool searchingEarth = false;
	HandInput handInput;
	GameObject heldRock = null;

	public delegate void BendingAction (HandInput.HandEventArgs args);

	public class BendingPhase {
		public HandInput.HandEvent runCondition;
		public HandInput.HandEvent[] failConditions;
		public BendingAction inBetweenAction;
		public BendingPhase (HandInput.HandEvent runCondition, HandInput.HandEvent[] failConditions, BendingAction inBetweenAction) {
			this.runCondition = runCondition;
			this.failConditions = failConditions;
			this.inBetweenAction = inBetweenAction;
		}
	}

	public class BendingSpell {
		public List<BendingPhase> phases = new List<BendingPhase> ();
		public BendingAction bendingAction;
		int index;

		public void Initialize () {
			index = 0;
			phases[0].runCondition.AddListener (NextCondition);
			if (phases[0].failConditions != null && phases[0].failConditions.Length > 0) {
				foreach (HandInput.HandEvent failCondition in phases[0].failConditions) {
					failCondition.AddListener (Fail);
				}
			}
		}

		public void NextCondition (HandInput.HandEventArgs args) {
			//Remove previous listener
			phases[index].runCondition.RemoveListener (NextCondition);
			//TODO: Remove failcondition listeners

			//	Debug.Log ("Inbetween at index " + index + " Action name: " + phases[index].inBetweenAction.Method.Name);
			phases[index].inBetweenAction (args);

			index++;
			Debug.Log ("Next condition! " + index);
			//If all runconditions are met
			if (index >= phases.Count) {
				bendingAction (args);
				Initialize ();
				return;
			}
			//Add new ones
			phases[index].runCondition.AddListener (NextCondition);

		}

		public void Fail (HandInput.HandEventArgs args) {
			phases[index].runCondition.RemoveListener (NextCondition);
			foreach (HandInput.HandEvent failCondition in phases[index].failConditions) {
				failCondition.RemoveListener (Fail);
			}
			index = 0;
		}
	}

	void SearchEarth (HandInput.HandEventArgs args) {
		Debug.Log ("Bout to search for Earth!");
		IEnumerator searchEarthCoroutine = CoSearchEarth ();
		StartCoroutine (searchEarthCoroutine);
		handInput.OnHandMove.AddListener (StopSearchEarth);
	}

	void StopSearchEarth (HandInput.HandEventArgs args) {
		this.searchingEarth = false;
		targetingArea.Stop ();
	}

	private IEnumerator CoSearchEarth () {
		this.searchingEarth = true;
		targetingArea.transform.position = new Vector3 (handInput.transform.position.x, handInput.transform.position.y, handInput.transform.position.z + 2);
		targetingArea.Play ();
		//Set search area in front of player
		//Get middle point between hands / or just one hand
		while (this.searchingEarth) {
			Vector3 leftHandMovement = handInput.leftHandVelHistory[handInput.leftHandVelHistory.Count - 1];
			targetingArea.transform.Translate (leftHandMovement.x * 3, 0, leftHandMovement.z * 3, Space.World);
			yield return new WaitForEndOfFrame ();
		}
	}

	public BendingSpell TargetEarth = new BendingSpell ();
	public BendingSpell PunchRocks = new BendingSpell ();
	public BendingSpell GrowWall = new BendingSpell ();
	// Use this for initialization
	void Start () {
		handInput = gameObject.GetComponent<HandInput> ();
		targetingArea.Stop ();

		TargetEarth.phases.Add (new BendingPhase (handInput.OnHandPalm, null, SearchEarth));
		TargetEarth.phases.Add (new BendingPhase (handInput.OnHandMove, null, new BendingAction (delegate { })));

		PunchRocks.phases.Add (new BendingPhase (handInput.OnHandPeakSpeedPalmUp, null, SpawnRock));
		PunchRocks.phases.Add (new BendingPhase (handInput.OnHandPeakSpeed, null, new BendingAction (delegate { })));
		PunchRocks.bendingAction = PunchRocksAction;

		GrowWall.phases.Add (new BendingPhase (handInput.OnHandPeakSpeedPalmDown, null, SpawnWall));

	}

	void TargetedEarth () {
		PunchRocks.Initialize ();
		GrowWall.Initialize ();
	}

	void PunchRocksAction (HandInput.HandEventArgs args) {
		StopCoroutine ("CoHoldRockInAir");
		Vector3 punchPower = handInput.leftHandVelHistory[handInput.leftHandVelHistory.Count - 1];
		Debug.Log ("Punched a rock! Punch strength: " + punchPower);
		heldRock.GetComponent<Rigidbody> ().AddForce (earthPower * punchPower, ForceMode.Impulse);
	}

	void SpawnRock (HandInput.HandEventArgs args) {
		Debug.Log ("Stone appears!");
		heldRock = GameObject.Instantiate (smallRock.gameObject, targetingArea.transform.position - Vector3.up * smallRock.transform.lossyScale.y, Quaternion.identity);
		StartCoroutine (CoIgnoreTerrainCollision (false));
		Vector3 spawnSpeed = handInput.leftHandVelHistory[handInput.leftHandVelHistory.Count - 1];
		heldRock.GetComponent<Rigidbody> ().AddForce (spawnSpeed * earthPower, ForceMode.Impulse);
		earthFocus.position = targetingArea.transform.position + spawnSpeed * earthPower;
		earthFocus.position = new Vector3 (earthFocus.position.x, handInput.transform.position.y + 3, earthFocus.position.z);
		StartCoroutine ("CoHoldRockInAir");

	}

	private IEnumerator CoHoldRockInAir () {
		Rigidbody rockRigidBody = heldRock.GetComponent<Rigidbody> ();
		while (true) {
			Vector3 focusDir = earthFocus.position - heldRock.transform.position;
			rockRigidBody.AddForce (focusDir * 10, ForceMode.Force);
			rockRigidBody.AddForce (-rockRigidBody.velocity * 5, ForceMode.Force);
			yield return new WaitForFixedUpdate ();
		}
	}

	private IEnumerator CoIgnoreTerrainCollision (bool forever = true) {
		Collider rockCollider = heldRock.GetComponent<Collider> ();
		Collider[] spawnColliders = Physics.OverlapBox (targetingArea.transform.position, rockPillar.transform.lossyScale, Quaternion.identity);
		List<Collider> terrainColliders = (Physics.OverlapBox (targetingArea.transform.position, rockPillar.transform.lossyScale, Quaternion.identity)).ToList ()
			.Where (col => col.gameObject.tag == "Terrain").ToList ();

		foreach (Collider col in terrainColliders) {
			Physics.IgnoreCollision (col, rockCollider, true);
		}

		yield return new WaitForSeconds (1);

		if (!forever) {
			foreach (Collider col in terrainColliders) {
				Physics.IgnoreCollision (col, rockCollider, false);
			}
		}
	}

	void SpawnWall (HandInput.HandEventArgs args) {
		Debug.Log ("Wall appears!");
		heldRock = GameObject.Instantiate (rockPillar, targetingArea.transform.position, Quaternion.identity);
		StartCoroutine (CoIgnoreTerrainCollision (true));
		Vector3 spawnSpeed = args.velocity;
		Rigidbody wallRigid = heldRock.GetComponent<Rigidbody> ();
		wallRigid.AddForce (spawnSpeed * earthPower);
	}

	void OnDrawGizmos () {
		Gizmos.color = Color.blue;
		Gizmos.DrawSphere (earthFocus.transform.position, 1f);
	}
}