using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandMovementBehavior : MonoBehaviour {

	private const int MAXFRAMESTORAGE = 300;

	public UnityEvent leftHandFistClose = new UnityEvent ();
	public UnityEvent leftHandFistOpen = new UnityEvent ();

	[SerializeField]
	Transform leftHand;
	[SerializeField]
	Transform rightHand;

	[SerializeField]
	float leftThumbSqueeze;
	[SerializeField]
	float leftIndexSqueeze;
	[SerializeField]
	float leftHandSqueeze;

	[SerializeField]
	float minMovementThreshold = 5f;

	[SerializeField]
	int averagingLength = 3;
	List<Vector3> leftHandHistory = new List<Vector3> ();
	[SerializeField]
	AnimationCurve leftHandHistoryPlot = new AnimationCurve ();

	bool leftFist = false;

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void FixedUpdate () {

		leftThumbSqueeze = Input.GetAxis ("TouchLeftThumbRest");
		leftIndexSqueeze = Input.GetAxis ("Oculus_CrossPlatform_PrimaryIndexTrigger");
		leftHandSqueeze = Input.GetAxis ("Oculus_CrossPlatform_PrimaryHandTrigger");
		//If closed fist
		if (leftThumbSqueeze == -1 && leftIndexSqueeze >= 0.75f && leftHandSqueeze >= 0.75f) {
			if (leftFist) {

			} else {
				leftFist = true;
				leftHandFistOpen.Invoke ();
			}
		}

		leftHandHistory.Add (leftHand.position);
		if (leftHandHistory.Count > MAXFRAMESTORAGE)
			leftHandHistory.RemoveAt (0);

		int averageFrameLength = Mathf.Min (averagingLength, leftHandHistory.Count);
		Vector3 leftHandVel = Vector3.zero;
		for (int i = 0; i < averageFrameLength - 1; i++) {
			leftHandVel += leftHandHistory[leftHandHistory.Count - (1 + i)] - leftHandHistory[leftHandHistory.Count - (2 + i)];
		}
		leftHandVel /= averageFrameLength;
		leftHandHistoryPlot = new AnimationCurve ();
		for (int i = 0; i < leftHandHistory.Count; i++) {
			leftHandHistoryPlot.AddKey (i / leftHandHistory.Count, leftHandHistory[i].magnitude);
			if (leftHandHistory.Count > 5) Debug.Log (leftHandHistory[i].magnitude);
		}
		Debug.Log (leftHandHistoryPlot.keys);
		Debug.Log (leftHandVel.magnitude);
		if (leftHandVel.magnitude > minMovementThreshold) {
			Debug.Log ("Swiish!");
		} else {
			Debug.Log ("Pause");
		}
	}
}