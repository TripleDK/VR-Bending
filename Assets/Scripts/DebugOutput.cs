using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;

public class DebugOutput : MonoBehaviour {

	Text textField;

	// Use this for initialization
	void Start () {
		textField = GetComponent<Text> ();
		textField.text = "";
		Application.logMessageReceived += AppendToTextField;
	}

	void AppendToTextField (string logString, string stackTrace, LogType type) {
		textField.text += logString + "\n";
		string[] textLines = textField.text.Split ('\n');
		if (textLines.Length > 12) {
			textField.text = String.Join ("\n", textLines.Skip (textLines.Length - 12).ToArray ());
		}
	}

}