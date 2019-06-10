using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MushyMesh {
	[CanEditMultipleObjects]
	[CustomEditor(typeof(Softbody2D))]
	public class Softbody2DEditor : Editor {
		private bool generated = false;
		public Softbody2D softbody;
		TextureImporter importer;

		public override void OnInspectorGUI () {
			base.OnInspectorGUI();

			if (!softbody) {
				softbody = (Softbody2D)target;
			}

			if (EditorApplication.isPlaying) return;

			Sprite sprite = softbody.gameObject.GetComponent<SpriteRenderer>().sprite;
			if (sprite) {
				importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
				if (!importer.isReadable) {
					importer.isReadable = true;
					importer.SaveAndReimport();
				}
			}

			//if (GUILayout.Button("Tense Up")) {
			//	((Softbody2D)target).Tense();
			//}

			//if (!generated) {
			//	if (GUILayout.Button("Generate Softbody")) {
			//		((Softbody)target).GenerateSoftbody();
			//	}
			//}
		}
	}
}