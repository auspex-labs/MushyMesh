using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickAndDrag : MonoBehaviour {
	public bool holding;
	public Rigidbody2D target;
	public Vector2 difference;

	private void Start () {
		holding = false;
	}

	private void Update () {
		if (Input.GetMouseButtonDown(0)) {
			holding = true;
			print("Click");
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit2D hit;

			if (hit = Physics2D.Raycast(ray.origin, ray.direction)) {
				print("Hit " + hit.collider.name);
				if (hit.collider.GetComponent<Rigidbody2D>()) {
					print("Thing had rigidbody!");
					target = hit.collider.GetComponent<Rigidbody2D>();
				}
			}

		} else if (Input.GetMouseButtonUp(0)) {
			print("Unclick");
			holding = false;
			target = null;
		}

		if (target) {
			//difference = (Vector2)Vector3.ProjectOnPlane(Camera.main.ScreenPointToRay(Input.mousePosition).direction, Vector3.forward) - target.position;
			difference = (Input.mousePosition - Camera.main.WorldToScreenPoint(target.position)) / Mathf.Max(Screen.width, Screen.height);
			print("Applying force");
			target.AddForce(difference * 100);
		}
	}
}
