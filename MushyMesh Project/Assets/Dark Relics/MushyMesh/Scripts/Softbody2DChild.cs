using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushyMesh {
	public class Softbody2DChild : MonoBehaviour {
		public Softbody2D parent;

		//Collisions sent to parent. Ignores collision with other softbody nodes on the same object

		public void OnTriggerExit2D (Collider2D collision) {
			if (parent && (!collision.gameObject.GetComponent<Softbody2DChild>() || 
			collision.gameObject.GetComponent<Softbody2DChild>().parent != parent)) {
				parent.OnTriggerExit2D(collision);
			}
		}

		public void OnTriggerStay2D (Collider2D collision) {
			if (parent && (!collision.gameObject.GetComponent<Softbody2DChild>() ||
			collision.gameObject.GetComponent<Softbody2DChild>().parent != parent)) {
				parent.OnTriggerStay2D(collision);
			}
		}

		public void OnTriggerEnter2D (Collider2D collision) {
			if (parent && (!collision.gameObject.GetComponent<Softbody2DChild>() ||
			collision.gameObject.GetComponent<Softbody2DChild>().parent != parent)) {
				parent.OnTriggerEnter2D(collision);
			}
		}

		public void OnCollisionExit2D (Collision2D collision) {
			if (parent && (!collision.gameObject.GetComponent<Softbody2DChild>() ||
			collision.gameObject.GetComponent<Softbody2DChild>().parent != parent)) {
				parent.OnCollisionExit2D(collision);
			}
		}

		public void OnCollisionStay2D (Collision2D collision) {
			if (parent && (!collision.gameObject.GetComponent<Softbody2DChild>() ||
			collision.gameObject.GetComponent<Softbody2DChild>().parent != parent)) {
				parent.OnCollisionStay2D(collision);
			}
		}

		public void OnCollisionEnter2D (Collision2D collision) {
			if (parent) {
				parent.OnCollisionEnter2D(collision);
			}
		}
	}
}