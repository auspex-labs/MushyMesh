using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Softbody))]
public class SoftbodyEditor : Editor {
    private bool generated = false;

    public Softbody softbody;

    public override void OnInspectorGUI () {
        base.OnInspectorGUI ();

        if (GUILayout.Button ("Tense Up")) {
            ((Softbody)target).Tense ();
        }

        if (!generated) {
            if (GUILayout.Button("Generate Softbody")) {
                ((Softbody)target).GenerateSoftbody ();
            }
        }
    }
}
