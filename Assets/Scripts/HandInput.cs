using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandInput : MonoBehaviour {

	private const int MAXFRAMESTORAGE = 100;

	public enum HandIndex { LeftHand, RightHand, Both };

	public class HandEvent : UnityEvent<HandEventArgs> { }
	public struct HandEventArgs {
		public HandIndex handIndex;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 velocity;
		public Vector3 acceleration;
	};

	public HandEvent OnHandFist = new HandEvent ();
	public HandEvent OnHandOpen = new HandEvent ();
	public HandEvent OnHandPalm = new HandEvent ();
	public HandEvent OnHandWater = new HandEvent ();
	public HandEvent OnHandMove = new HandEvent ();
	public HandEvent OnHandPeakSpeed = new HandEvent ();
	public HandEvent OnHandPeakSpeedPalmUp = new HandEvent ();
	public HandEvent OnHandPeakSpeedPalmDown = new HandEvent ();
	public HandEvent OnHandStop = new HandEvent ();

	public Transform leftHand;
	public Transform rightHand;

	[SerializeField]
	float leftThumbSqueeze, rightThumbSqueeze;
	[SerializeField]
	float leftIndexSqueeze, rightIndexSqueeze;
	[SerializeField]
	float leftHandSqueeze, rightHandSqueeze;

	[SerializeField]
	float minMovementThreshold = 5f;

	[SerializeField]
	int averagingLength = 3;

	public List<Vector3> leftHandPosHistory = new List<Vector3> ();
	[SerializeField]
	AnimationCurve leftHandPosHistoryPlot = new AnimationCurve ();
	public List<Vector3> leftHandVelHistory = new List<Vector3> ();
	[SerializeField]
	AnimationCurve leftHandVelHistoryPlot = new AnimationCurve ();
	public List<float> leftHandAccHistory = new List<float> ();
	[SerializeField]
	AnimationCurve leftHandAccHistoryPlot = new AnimationCurve ();
	public List<Vector3> leftHandAccDirHistory = new List<Vector3> ();
	public List<Vector3> rightHandPosHistory = new List<Vector3> ();
	[SerializeField]
	AnimationCurve rightHandPosHistoryPlot = new AnimationCurve ();
	public List<Vector3> rightHandVelHistory = new List<Vector3> ();
	[SerializeField]
	AnimationCurve rightHandVelHistoryPlot = new AnimationCurve ();
	public List<float> rightHandAccHistory = new List<float> ();
	[SerializeField]
	AnimationCurve rightHandAccHistoryPlot = new AnimationCurve ();
	public List<Vector3> rightHandAccDirHistory = new List<Vector3> ();

	bool leftHandMoving = false;
	bool leftHandSpeedpeaked = false;
	bool leftFist = false;
	bool leftOpenPalm = false;
	bool leftWaterHand = false;
	bool rightHandMoving = false;
	bool rightHandSpeedpeaked = false;
	bool rightFist = false;
	bool rightOpenPalm = false;
	bool rightWaterHand = false;

	HandEventArgs currentLeftHandArgs, currentRightHandArgs;

	// Use this for initialization
	void Start () {
		StartCoroutine ("DelayedStart");
	}

	IEnumerator DelayedStart () {
		FireBending fireBending = GetComponent<FireBending> ();
		fireBending.enabled = false;
		EarthBending earthBending = GetComponent<EarthBending> ();
		earthBending.enabled = false;
		yield return new WaitForSeconds (2);
		fireBending.enabled = true;
		earthBending.enabled = true;
	}

	// Update is called once per frame
	void FixedUpdate () {
		CalculateHandMovement ();

		currentLeftHandArgs = new HandEventArgs () {
			handIndex = HandIndex.LeftHand,
				position = leftHandPosHistory[leftHandPosHistory.Count - 1],
				velocity = leftHandVelHistory[leftHandVelHistory.Count - 1],
				rotation = leftHand.rotation
		};
		currentRightHandArgs = new HandEventArgs () {
			handIndex = HandIndex.RightHand,
				position = leftHandPosHistory[leftHandPosHistory.Count - 1],
				velocity = leftHandVelHistory[leftHandVelHistory.Count - 1],
				rotation = leftHand.rotation
		};

		CheckMovementEvents ();
	}

	void CalculateHandMovement () {
		//Get button input
		leftThumbSqueeze = Input.GetAxis ("TouchLeftThumbRest");
		leftIndexSqueeze = Input.GetAxis ("Oculus_CrossPlatform_PrimaryIndexTrigger");
		leftHandSqueeze = Input.GetAxis ("Oculus_CrossPlatform_PrimaryHandTrigger");

		//Pos
		leftHandPosHistory.Add (leftHand.position);
		if (leftHandPosHistory.Count > MAXFRAMESTORAGE)
			leftHandPosHistory.RemoveAt (0);
		rightHandPosHistory.Add (rightHand.position);
		if (rightHandPosHistory.Count > MAXFRAMESTORAGE)
			rightHandPosHistory.RemoveAt (0);

		//Calculate average velocity over last few frames
		int averageFrameLength = Mathf.Min (averagingLength, leftHandPosHistory.Count);
		Vector3 leftHandVel = Vector3.zero;
		for (int i = 0; i < averageFrameLength - 1; i++) {
			leftHandVel += leftHandPosHistory[leftHandPosHistory.Count - (1 + i)] - leftHandPosHistory[leftHandPosHistory.Count - (2 + i)];
		}
		leftHandVel /= averageFrameLength;
		leftHandVelHistory.Add (leftHandVel);
		if (leftHandVelHistory.Count > MAXFRAMESTORAGE)
			leftHandVelHistory.RemoveAt (0);
		Vector3 rightHandVel = Vector3.zero;
		for (int i = 0; i < averageFrameLength - 1; i++) {
			rightHandVel += rightHandPosHistory[rightHandPosHistory.Count - (1 + i)] - rightHandPosHistory[rightHandPosHistory.Count - (2 + i)];
		}
		rightHandVel /= averageFrameLength;
		rightHandVelHistory.Add (rightHandVel);
		if (rightHandVelHistory.Count > MAXFRAMESTORAGE)
			rightHandVelHistory.RemoveAt (0);

		//Graph velocity history
		leftHandVelHistoryPlot = new AnimationCurve ();
		for (int i = 1; i < leftHandVelHistory.Count; i++) {
			leftHandVelHistoryPlot.AddKey ((float) i / leftHandVelHistory.Count, leftHandVelHistory[i].magnitude);
		}
		rightHandVelHistoryPlot = new AnimationCurve ();
		for (int i = 1; i < rightHandVelHistory.Count; i++) {
			rightHandVelHistoryPlot.AddKey ((float) i / rightHandVelHistory.Count, rightHandVelHistory[i].magnitude);
		}

		//Calculate acceleration
		float leftHandAcc = 0;
		if (leftHandVelHistory.Count > 2) leftHandAcc = leftHandVelHistory[leftHandVelHistory.Count - 1].magnitude - leftHandVelHistory[leftHandVelHistory.Count - 2].magnitude;
		leftHandAccHistory.Add (leftHandAcc);
		if (leftHandAccHistory.Count > MAXFRAMESTORAGE)
			leftHandAccHistory.RemoveAt (0);
		float rightHandAcc = 0;
		if (rightHandVelHistory.Count > 2) rightHandAcc = rightHandVelHistory[rightHandVelHistory.Count - 1].magnitude - rightHandVelHistory[rightHandVelHistory.Count - 2].magnitude;
		rightHandAccHistory.Add (rightHandAcc);
		if (rightHandAccHistory.Count > MAXFRAMESTORAGE)
			rightHandAccHistory.RemoveAt (0);

		//Graph acceleration history
		leftHandAccHistoryPlot = new AnimationCurve ();
		for (int i = 1; i < leftHandAccHistory.Count; i++) {
			leftHandAccHistoryPlot.AddKey ((float) i / leftHandAccHistory.Count, leftHandAccHistory[i]);
		}
		rightHandAccHistoryPlot = new AnimationCurve ();
		for (int i = 1; i < rightHandAccHistory.Count; i++) {
			rightHandAccHistoryPlot.AddKey ((float) i / rightHandAccHistory.Count, rightHandAccHistory[i]);
		}
	}

	void CheckMovementEvents () {
		//Check for starts and stops in movements
		if (leftHandVelHistory[leftHandVelHistory.Count - 1].magnitude > minMovementThreshold) {
			if (!leftHandMoving) {
				leftHandMoving = true;
				OnHandMove.Invoke (currentLeftHandArgs);
			}
		} else {
			if (leftHandMoving) {
				leftHandMoving = false;
				leftHandSpeedpeaked = false;
				OnHandStop.Invoke (currentLeftHandArgs);
			}
		}
		if (rightHandVelHistory[rightHandVelHistory.Count - 1].magnitude > minMovementThreshold) {
			if (!rightHandMoving) {
				rightHandMoving = true;
				OnHandMove.Invoke (currentRightHandArgs);
			}
		} else {
			if (rightHandMoving) {
				rightHandMoving = false;
				rightHandSpeedpeaked = false;
				OnHandStop.Invoke (currentRightHandArgs);
			}
		}
		//Check for velocity peaks
		if (leftHandMoving) {
			if (leftHandAccHistory[leftHandAccHistory.Count - 1] < 0 && leftHandSpeedpeaked == false) {
				leftHandSpeedpeaked = true;
				OnHandPeakSpeed.Invoke (currentLeftHandArgs);
				if (currentLeftHandArgs.rotation.eulerAngles.z > 90 && currentLeftHandArgs.rotation.eulerAngles.z < 270) {
					OnHandPeakSpeedPalmUp.Invoke (currentLeftHandArgs);
				} else {
					OnHandPeakSpeedPalmDown.Invoke (currentLeftHandArgs);
				}
			}
		}
		if (rightHandMoving) {
			if (rightHandAccHistory[rightHandAccHistory.Count - 1] < 0 && rightHandSpeedpeaked == false) {
				rightHandSpeedpeaked = true;
				OnHandPeakSpeed.Invoke (currentRightHandArgs);
				if (currentRightHandArgs.rotation.eulerAngles.z > 90 && currentRightHandArgs.rotation.eulerAngles.z < 270) {
					OnHandPeakSpeedPalmUp.Invoke (currentRightHandArgs);
				} else {
					OnHandPeakSpeedPalmDown.Invoke (currentRightHandArgs);
				}
			}
		}

		///Check for hand stance
		//Closed fist
		if (leftThumbSqueeze == -1 && leftIndexSqueeze >= 0.75f && leftHandSqueeze >= 0.75f) {
			if (leftFist) {

			} else {
				leftFist = true;
				OnHandFist.Invoke (currentLeftHandArgs);
			}
			//Open palm	
		} else if (leftThumbSqueeze == -1 && leftIndexSqueeze <= 0.01f && leftHandSqueeze <= 0.01f) {
			if (leftOpenPalm) {

			} else {
				leftOpenPalm = true;
				OnHandPalm.Invoke (currentLeftHandArgs);
			}
			//Water fist
		} else if (leftThumbSqueeze == 0 && leftHandSqueeze <= 0.01f && leftIndexSqueeze > 0.3f && leftIndexSqueeze < 0.8f) {
			if (leftWaterHand) {

			} else {
				leftWaterHand = true;
				OnHandWater.Invoke (currentLeftHandArgs);
			}
		} else {
			if (leftFist || leftOpenPalm || leftWaterHand) {
				leftFist = false;
				leftOpenPalm = false;
				leftWaterHand = false;
				OnHandOpen.Invoke (currentLeftHandArgs);
			}
		}
		//Closed fist
		if (rightThumbSqueeze == -1 && rightIndexSqueeze >= 0.75f && rightHandSqueeze >= 0.75f) {
			if (rightFist) {

			} else {
				rightFist = true;
				OnHandFist.Invoke (currentRightHandArgs);
			}
			//Open palm	
		} else if (rightThumbSqueeze == -1 && rightIndexSqueeze <= 0.01f && rightHandSqueeze <= 0.01f) {
			if (rightOpenPalm) {

			} else {
				rightOpenPalm = true;
				OnHandPalm.Invoke (currentRightHandArgs);
			}
			//Water fist
		} else if (rightThumbSqueeze == 0 && rightHandSqueeze <= 0.01f && rightIndexSqueeze > 0.3f && rightIndexSqueeze < 0.8f) {
			if (rightWaterHand) {

			} else {
				rightWaterHand = true;
				OnHandWater.Invoke (currentRightHandArgs);
			}
		} else {
			if (rightFist || rightOpenPalm || rightWaterHand) {
				rightFist = false;
				rightOpenPalm = false;
				rightWaterHand = false;
				OnHandOpen.Invoke (currentRightHandArgs);
			}
		}
	}
}