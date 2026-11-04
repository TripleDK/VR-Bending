using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Bending : MonoBehaviour {
	public HandInput handInput;
	public delegate void BendingAction ();

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

	public enum BendingSpellHandType {
		SingleHanded,
		TwoHanded,
		EitherOr
	}

	public class BendingSpell {
		public List<BendingPhase> phases = new List<BendingPhase> ();
		public BendingAction bendingAction;
		public BendingSpellHandType handType;
		public HandInput.HandIndex handIndex;
		int index;

		public BendingSpell (BendingSpellHandType handType) {
			this.handType = handType;
		}

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
			phases[index].inBetweenAction ();

			index++;
			Debug.Log ("Next condition! " + index);
			//If all runconditions are met
			if (index >= phases.Count) {
				bendingAction ();
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

	public virtual void Start () {
		handInput = gameObject.GetComponent<HandInput> ();
	}

}