using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallStopper : MonoBehaviour {
	private void OnTriggerEnter (Collider other) {
		if (other.gameObject.tag == "Terrain") {
			StartCoroutine (SlowStopping ());
		}
	}

	private IEnumerator SlowStopping () {
		Rigidbody parentRigid = transform.parent.gameObject.GetComponent<Rigidbody> ();
		while (parentRigid.velocity.magnitude > 0.01f) {
			parentRigid.velocity /= 2;
			yield return new WaitForFixedUpdate ();
		}
	}
}