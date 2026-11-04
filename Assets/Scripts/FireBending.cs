using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FireBending : MonoBehaviour {

	public GameObject fireBurstParticle;

	HandInput handInput;

	public delegate void BendingAction ();

	public class BendingSpell {
		public List<HandInput.HandEvent> runConditions = new List<HandInput.HandEvent> ();
		public List<HandInput.HandEvent[]> failConditions = new List<HandInput.HandEvent[]> ();
		public BendingAction bendingAction;
		int index;

		public void Initialize () {
			index = 0;
			runConditions[0].AddListener (NextCondition);
			if (failConditions.Count > 0) {
				foreach (HandInput.HandEvent failCondition in failConditions[0]) {
					failCondition.AddListener (Fail);
				}
			}
		}

		public void NextCondition (HandInput.HandEventArgs args) {
			//Remove previous listener
			runConditions[index].RemoveListener (NextCondition);

			index++;
			//If all runconditions are met
			if (index >= runConditions.Count) {
				bendingAction ();
				//TODO: Handle how to skip the handpose event for consecutive attacks
				Initialize ();
				return;
			}
			//Add new ones
			runConditions[index].AddListener (NextCondition);
		}

		public void Fail (HandInput.HandEventArgs args) {
			runConditions[index].RemoveListener (NextCondition);
			foreach (HandInput.HandEvent failCondition in failConditions[0]) {
				failCondition.RemoveListener (Fail);
			}
			index = 0;
		}
	}

	public BendingSpell FireBall = new BendingSpell ();

	// Use this for initialization
	void Start () {
		handInput = gameObject.GetComponent<HandInput> ();
		FireBall.runConditions.Add (handInput.OnHandFist);
		FireBall.runConditions.Add (handInput.OnHandMove);
		FireBall.runConditions.Add (handInput.OnHandPeakSpeed);
		FireBall.bendingAction = FireBallAction;
		FireBall.Initialize ();
	}

	void FireBallAction () {
		//TODO: Rotate to point towards hand motion
		Vector3 handMovementDirection = handInput.leftHandVelHistory[handInput.leftHandVelHistory.Count - 1];
		Debug.DrawLine (handInput.leftHand.position, handMovementDirection * 3, Color.red);
		Debug.Log ("Fire ball jutsu! " + handMovementDirection.ToString ());
		GameObject fireBall = GameObject.Instantiate (fireBurstParticle, handInput.leftHand.position, Quaternion.identity);
		fireBall.transform.LookAt (fireBall.transform.position + handMovementDirection, transform.up);
	}

}