using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBending : Bending {
	public Transform waterFocus;
	public float bendingRange = 10f;

	BendingSpell waterThrow = new BendingSpell (Bending.BendingSpellHandType.EitherOr);

	public override void Start () {
		base.Start ();
		waterThrow.phases.Add (new BendingPhase (handInput.OnHandWater, null, null));
	}

}