using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MushyMesh {
	public class Softbody2D : MonoBehaviour {
		[System.Serializable]
		private enum Quality { off, low, medium, high, ultra, };

		[Header("Softbody Info")]
		//Physics info
		[Tooltip("Sample Resolution. Higher means more accurate softbody, but more intensive. Avoid using Ultra with a high volume of softbodies.")]
		[SerializeField] private Quality quality;
		private int layers;
		[Tooltip("Percent rigid. At values close to 1, can get bouncy. Values from 0.25-0.75 look the best.")]
		[Range(0.1f, 1f)] [SerializeField] private float stiffness = 0.5f;
		private float adjustedStiffness;
		private bool dynamicShape = true; //If true, conforms to sprite shape. Otherwise, a square.


		[Header("Rigidbody Info")]
		[Tooltip("The type of rigidbody.")]
		[SerializeField] private RigidbodyType2D bodyType = RigidbodyType2D.Dynamic;
		[Tooltip("The physics material used by all attached colliders. Overrides any global physics material.")]
		[SerializeField] private PhysicsMaterial2D material;
		[Tooltip("Is the body simulated? If not, it is effectively hidden from the simulation.")]
		[SerializeField] private bool simulated = true;
		[Tooltip("Whether to calculate the mass from the collider(s) density and area.")]
		[SerializeField] private bool useAutoMass = false;
		[Tooltip("The mass of the body. [ 0.0001, 1000000 ].")]
		[SerializeField] private float mass = 1f;
		[Tooltip("The linear drag coefficient. 0 means no damping. [ 0, 1000000 ].")]
		[SerializeField] private float linearDrag = 0;
		[Tooltip("The angular drag coefficient. 0 means no damping. [ 0, 1000000 ].")]
		[SerializeField] private float angularDrag = 0.05f;
		[Tooltip("How much gravity affects this body.  [ -1000000, 1000000 ].")]
		[SerializeField] private float gravityScale = 1f;
		[Tooltip("The collision detection mode for the body.")]
		[SerializeField] private CollisionDetectionMode2D collisionDetection = CollisionDetectionMode2D.Continuous;
		[Tooltip("The sleeping mode for the body.")]
		[SerializeField] private RigidbodySleepMode2D sleepingMode = RigidbodySleepMode2D.StartAwake;
		[Tooltip("The per-frame update mode for the body.")]
		[SerializeField] private RigidbodyInterpolation2D interpolation = RigidbodyInterpolation2D.None;

		private float width, height;
		private List<GameObject> children;
		private List<CircleCollider2D> colliders;
		private List<SpringJoint2D> springJoints;
		private SpriteRenderer spriteRenderer;
		private Sprite sprite;
		private Rigidbody2D rootRB;


		//Mesh info
		Mesh mesh;
		MeshRenderer mr;
		MeshFilter mf;
		List<Vector3> vertices;
		List<int> triangles;
		List<Vector2> uvs;
		Transform[,] points;
		Material mat;
		bool start;

		private void Start () {
			SetQuality();

			if (layers == 0) return; //Don't create softbody if quality = off

			if (null == GetComponent<Rigidbody2D>()) {
				rootRB = gameObject.AddComponent<Rigidbody2D>();
			} else {
				rootRB = gameObject.GetComponent<Rigidbody2D>();
				mass = rootRB.mass;
			}
			rootRB.mass = mass / (layers * layers);
			//rootRB.constraints = RigidbodyConstraints2D.FreezeRotation;

			GenerateSoftbody();
			StartCoroutine(InitializeMesh());
		}

		private void SetQuality () {
			switch (quality) {
				case Quality.off:
					layers = 0;
					break;
				case Quality.low:
					layers = 5;
					break;
				case Quality.medium:
					layers = 8;
					break;
				case Quality.high:
					layers = 12;
					break;
				case Quality.ultra:
					layers = 16;
					break;
				default:
					break;
			}
		}

		public void SetRigidbodyInfo (Rigidbody2D rb) {
			rb.bodyType = bodyType;
			rb.sharedMaterial = material;
			rb.simulated = simulated;
			rb.useAutoMass = useAutoMass;
			rb.mass = mass / (layers * layers);
			rb.drag = linearDrag;
			rb.angularDrag = angularDrag;
			rb.gravityScale = gravityScale;
			rb.collisionDetectionMode = collisionDetection;
			rb.sleepMode = sleepingMode;
			rb.interpolation = interpolation;
			rb.constraints = RigidbodyConstraints2D.FreezeRotation;
		}

		public void GenerateSoftbody () {
			children = new List<GameObject>();
			colliders = new List<CircleCollider2D>();
			springJoints = new List<SpringJoint2D>();
			Vector3 origin = transform.position;
			adjustedStiffness = stiffness * Mathf.Log(layers, 2f) * 8;

			if (null == GetComponent<SpriteRenderer>()) {
				Debug.LogError("Could not generate softbody on " + gameObject.name
				+ ". Make sure it has a Sprite Renderer attached.");
				return;
			}

			spriteRenderer = GetComponent<SpriteRenderer>();
			sprite = spriteRenderer.sprite;

			if (null == sprite) {
				Debug.LogError("Could not generate softbody on " + gameObject.name
				+ ". Make sure it has a non-null Sprite in the Sprite Renderer.");
				return;
			}

			width = sprite.texture.width / sprite.pixelsPerUnit;
			height = sprite.texture.height / sprite.pixelsPerUnit;

			CreateMaterial();
			spriteRenderer.enabled = false;

			for (int h = 0; h < layers; h++) {
				for (int w = 0; w < layers; w++) {
					float xi = w - layers / 2f + 0.5f;
					float yi = h - layers / 2f + 0.5f;

					GameObject child = new GameObject("Piece " + w.ToString() + ", " + h.ToString());
					child.transform.SetParent(transform);
					children.Add(child);

					Rigidbody2D rb = child.AddComponent<Rigidbody2D>();
					SetRigidbodyInfo(rb);

					CircleCollider2D col = child.AddComponent<CircleCollider2D>();
					colliders.Add(col);
					col.radius = width * Mathf.Min(transform.localScale.x, transform.localScale.y) / (2 * layers);

					child.transform.position = origin +
						transform.right * transform.localScale.x * xi * width / (layers) +
						transform.up * transform.localScale.y * yi * height / (layers);

					SpriteRenderer thisSr = child.AddComponent<SpriteRenderer>();

					//SpringJoint2D connectionToParent = child.AddComponent<SpringJoint2D> ();
					//connectionToParent.frequency = 0.1f;
					//connectionToParent.connectedBody = rootRB;

					if (w > 0 && h > 0) {
						SpringJoint2D diagonal = child.AddComponent<SpringJoint2D>();
						SpringJoint2D left = child.AddComponent<SpringJoint2D>();
						SpringJoint2D up = child.AddComponent<SpringJoint2D>();

						springJoints.Add(diagonal);
						springJoints.Add(left);
						springJoints.Add(up);

						diagonal.frequency = adjustedStiffness;
						left.frequency = adjustedStiffness;
						up.frequency = adjustedStiffness;

						diagonal.connectedBody = children[(h - 1) * layers + w - 1].GetComponent<Rigidbody2D>();
						left.connectedBody = children[(h) * layers + w - 1].GetComponent<Rigidbody2D>();
						up.connectedBody = children[(h - 1) * layers + w].GetComponent<Rigidbody2D>();

					} else if (w > 0 && h == 0) {
						SpringJoint2D left = child.AddComponent<SpringJoint2D>();

						springJoints.Add(left);

						left.frequency = adjustedStiffness;
						left.connectedBody = children[h * layers + w - 1].GetComponent<Rigidbody2D>();

					} else if (h > 0 && w == 0) {
						SpringJoint2D up = child.AddComponent<SpringJoint2D>();

						springJoints.Add(up);

						up.frequency = adjustedStiffness;
						up.connectedBody = children[(h - 1) * layers + w].GetComponent<Rigidbody2D>();
					}

					int x = Mathf.RoundToInt((float)w / (layers - 1) * width * sprite.pixelsPerUnit);
					int y = Mathf.RoundToInt((float)h / (layers - 1) * height * sprite.pixelsPerUnit);

					if (false == dynamicShape) continue;

					if (false == sprite.texture.isReadable) {
						Debug.LogError("Could not generate softbody on " + gameObject.name
						+ ". Make sure the Sprite Texture is read enabled.");
						for (int i = 1; i <= layers * layers; i++) {
							Destroy(transform.GetChild(transform.childCount - i).gameObject);
						}
					}

					if (sprite.texture.GetPixel(x, y).a <= float.Epsilon) {
						col.enabled = false;
						rb.gravityScale = 0;
						rb.mass = 0.001f;
					}
				}
			}

			//Root object
			rootRB.constraints = RigidbodyConstraints2D.FreezeRotation;
			//CircleCollider2D rootCol = gameObject.AddComponent<CircleCollider2D> ();
			//rootCol.radius = width * Mathf.Min (transform.localScale.x, transform.localScale.y) / (2 * layers);

			SpringJoint2D u = gameObject.AddComponent<SpringJoint2D>();
			SpringJoint2D d = gameObject.AddComponent<SpringJoint2D>();
			SpringJoint2D l = gameObject.AddComponent<SpringJoint2D>();
			SpringJoint2D r = gameObject.AddComponent<SpringJoint2D>();

			u.frequency = adjustedStiffness;
			d.frequency = adjustedStiffness;
			l.frequency = adjustedStiffness;
			r.frequency = adjustedStiffness;

			int mid = layers / 2;

			u.connectedBody = children[(mid + 1) * layers + mid].GetComponent<Rigidbody2D>();
			d.connectedBody = children[(mid - 1) * layers + mid].GetComponent<Rigidbody2D>();
			l.connectedBody = children[mid * layers + mid - 1].GetComponent<Rigidbody2D>();
			r.connectedBody = children[mid * layers + mid + 1].GetComponent<Rigidbody2D>();
		}

		public void CreateMaterial () {
			mat = new Material(Shader.Find("Sprites/Default"));
			mat.mainTexture = sprite.texture;
			mat.name = sprite.texture.name;
		}

		//Mesh functions
		/*************************************************************************/
		IEnumerator InitializeMesh () {
			start = false;
			layers = GetComponent<Softbody2D>().layers;
			if (GetComponent<SpriteRenderer>()) {
				Destroy(GetComponent<SpriteRenderer>());
			}
			yield return new WaitForEndOfFrame();
			start = true;
			mf = gameObject.AddComponent<MeshFilter>();
			mr = gameObject.AddComponent<MeshRenderer>();
			mesh = new Mesh();
			mesh.name = gameObject.name;
			mf.mesh = mesh;
			mr.materials[0] = mat;
			mr.sharedMaterial = mat;

			vertices = new List<Vector3>();
			triangles = new List<int>();
			points = new Transform[layers, layers];
			uvs = new List<Vector2>();

			for (int i = 0; i < layers; i++) {
				for (int j = 0; j < layers; j++) {
					points[i, j] = (transform.GetChild(i * layers + j));
					uvs.Add(new Vector2((float)j / (layers - 1), (float)i / (layers - 1)));
				}
			}

			StartCoroutine(FragmentShader());
		}

		//TODO: Replace with GPU Compute Shader
		IEnumerator FragmentShader () {
			while (true) {
				vertices.Clear();
				foreach (var point in points) {
					vertices.Add(point.localPosition);
				}

				triangles.Clear();
				for (int i = 0; i < vertices.Count; i++) {
					if (i % layers >= layers - 1 || i / layers >= layers - 1)
						continue;

					triangles.Add(i);
					triangles.Add(i + layers);
					triangles.Add(i + 1);

					triangles.Add(i + 1);
					triangles.Add(i + layers);
					triangles.Add(i + layers + 1);
				}

				mesh.vertices = vertices.ToArray();
				mesh.uv = uvs.ToArray();
				mesh.triangles = triangles.ToArray();
				mesh.RecalculateNormals();
				yield return new WaitForEndOfFrame();
			}
		}

		//Runtime functions
		/*************************************************************************/
		public void Tense () {
			StartCoroutine(TenseCR());
		}

		IEnumerator TenseCR () {
			foreach (SpringJoint2D joint in springJoints) {
				joint.frequency *= 50f;
			}

			yield return new WaitForSeconds(1f);

			foreach (SpringJoint2D joint in springJoints) {
				joint.frequency /= 50f;
			}

			//List<float> frequencies = new List<float> ();
			//foreach (SpringJoint2D joint in springJoints) {
			//    frequencies.Add(joint.frequency);
			//}

			//for (int i = 0; i < 100; i++) {
			//    for (int f = 0; f < springJoints.Count; f++) {
			//        springJoints[f].frequency = Mathf.Lerp (frequencies[f], frequencies[f] * 50f, (float)i / 100);
			//        yield return new WaitForEndOfFrame ();
			//    }
			//}

			//for (int i = 0; i < 100; i++) {
			//    for (int f = 0; f < springJoints.Count; f++) {
			//        springJoints[f].frequency = Mathf.Lerp (frequencies[f] * 50f, frequencies[f], (float)i / 100);
			//        yield return new WaitForEndOfFrame ();
			//    }
			//}
		}
	}
}