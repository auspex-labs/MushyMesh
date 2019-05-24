using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MushyMesh {
	[CustomEditor(typeof(Softbody2D))]
	public class Softbody2DEditor : Editor {
		private bool generated = false;

		public Softbody2D softbody;

		public override void OnInspectorGUI () {
			base.OnInspectorGUI();

			if (GUILayout.Button("Tense Up")) {
				((Softbody2D)target).Tense();
			}

			//if (!generated) {
			//	if (GUILayout.Button("Generate Softbody")) {
			//		((Softbody)target).GenerateSoftbody();
			//	}
			//}
		}
	}
}