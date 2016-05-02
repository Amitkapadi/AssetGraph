using System;

using UnityEngine;
using UnityEditor;

namespace AssetBundleGraph {
	[Serializable] public class ConnectionPoint {
		[SerializeField] public string pointId;
		[SerializeField] public string label;
		[SerializeField] public bool isInput;
		[SerializeField] public bool isOutput;
		
		[SerializeField] public Rect buttonRect;
		[SerializeField] public string buttonStyle;

		public ConnectionPoint (string label, bool input, bool output) {
			this.pointId = Guid.NewGuid().ToString();
			this.label = label;
			this.isInput = input;
			this.isOutput = output;
		}

		public virtual void UpdatePos (int index, int max, float width, float height) {}
	}
}