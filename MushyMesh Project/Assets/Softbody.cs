using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Softbody : MonoBehaviour {

    //Physics info
    [SerializeField] int layers;
    [SerializeField] float stiffness = 2f;
    [SerializeField] float mass = 1f;
    [SerializeField] float width, height;
    [SerializeField] bool dynamicShape = true;
	[SerializeField] bool pregenerated = false;
    private List<GameObject> children;
    private List<CircleCollider2D> colliders;
    private List<SpringJoint2D> springJoints;
    private SpriteRenderer spriteRenderer;
    private Sprite sprite;
    private Rigidbody2D rootRB;

    public bool ignoreChildren = true;


    //Mesh info
    Mesh mesh;
    MeshRenderer mr;
    MeshFilter mf;
    List<Vector3> vertices;
    List<int> triangles;
    List<Vector2> uvs;
    Transform[,] points;
    Material mat;

    private void Start () {
        if (null == GetComponent<Rigidbody2D> ()) {
            rootRB = gameObject.AddComponent<Rigidbody2D> ();
        } else {
            rootRB = gameObject.GetComponent<Rigidbody2D> ();
            mass = rootRB.mass;
        }
        rootRB.mass = mass / (layers * layers);
		//rootRB.constraints = RigidbodyConstraints2D.FreezeRotation;

        List<Transform> children = new List<Transform>();
        if (ignoreChildren) {
            foreach (Transform t in transform) {
                children.Add(t);
                t.parent = null;
            }
        }

		if (null != (spriteRenderer = GetComponent<SpriteRenderer>())) {
			sprite = spriteRenderer.sprite;
		}

		if (false == pregenerated) {
			if (false == GenerateSoftbody()) {
				return;
			}
		}

		CreateMaterial();

		StartCoroutine (InitializeMesh ());

        if (ignoreChildren) {
            foreach (Transform t in children) {
                t.parent = transform;
            }
        }
    }

    public bool GenerateSoftbody () {
        children = new List<GameObject> ();
        colliders = new List<CircleCollider2D> ();
        springJoints = new List<SpringJoint2D> ();
        Vector3 origin = transform.position;
        stiffness *= layers;

        if (null == GetComponent<SpriteRenderer> ()) {
            Debug.LogError ("Could not generate softbody on " + gameObject.name
            + ". Make sure it has a Sprite Renderer attached.");
            return false;
        }

        if (null == sprite) {
            Debug.LogError ("Could not generate softbody on " + gameObject.name 
            + ". Make sure it has a non-null Sprite in the Sprite Renderer.");
            return false;
        }

        width = sprite.texture.width / sprite.pixelsPerUnit;
        height = sprite.texture.height / sprite.pixelsPerUnit;
        
        spriteRenderer.enabled = false;

        for (int h = 0; h < layers; h++) {
            for (int w = 0; w < layers; w++) {
                float xi = w - layers / 2f + 0.5f;
                float yi = h - layers / 2f + 0.5f;

                GameObject child = new GameObject ("Piece " + w.ToString () + ", " + h.ToString ());
                child.transform.SetParent (transform);
                children.Add (child);

                Rigidbody2D rb = child.AddComponent<Rigidbody2D> ();
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.mass = mass / (layers * layers);
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                //rb.gravityScale = 0f;

                CircleCollider2D col = child.AddComponent<CircleCollider2D> ();
                colliders.Add (col);
                col.radius = Mathf.Abs(Mathf.Max(width, height) * Mathf.Min (transform.localScale.x, transform.localScale.y) / (2 * layers));
                //col.radius = width * Mathf.Abs (Mathf.Min (transform.localScale.x, transform.localScale.y) / (2 * layers));

                child.transform.position = origin +
                    transform.right * transform.localScale.x * xi * width / (layers) +
                    transform.up * transform.localScale.y * yi * height / (layers);

                SpriteRenderer thisSr = child.AddComponent<SpriteRenderer> ();

                //SpringJoint2D connectionToParent = child.AddComponent<SpringJoint2D> ();
                //connectionToParent.frequency = 0.1f;
                //connectionToParent.connectedBody = rootRB;

                if (w > 0 && h > 0) {
                    SpringJoint2D diagonal = child.AddComponent<SpringJoint2D> ();
                    SpringJoint2D left = child.AddComponent<SpringJoint2D> ();
                    SpringJoint2D up = child.AddComponent<SpringJoint2D> ();

                    springJoints.Add (diagonal);
                    springJoints.Add (left);
                    springJoints.Add (up);

                    diagonal.frequency = stiffness;
                    left.frequency = stiffness * 2f;
                    up.frequency = stiffness;

                    diagonal.connectedBody = children[(h - 1) * layers + w - 1].GetComponent<Rigidbody2D> ();
                    left.connectedBody = children[(h) * layers + w - 1].GetComponent<Rigidbody2D> ();
                    up.connectedBody = children[(h - 1) * layers + w].GetComponent<Rigidbody2D> ();

                } else if (w > 0 && h == 0) {
                    SpringJoint2D left = child.AddComponent<SpringJoint2D> ();

                    springJoints.Add (left);

                    left.frequency = stiffness * 2f;
                    left.connectedBody = children[h * layers + w - 1].GetComponent<Rigidbody2D> ();

                } else if (h > 0 && w == 0) {
                    SpringJoint2D up = child.AddComponent<SpringJoint2D> ();

                    springJoints.Add (up);

                    up.frequency = stiffness;
                    up.connectedBody = children[(h - 1) * layers + w].GetComponent<Rigidbody2D> ();
                }

                int x = Mathf.RoundToInt((float)w / (layers - 1) * width * sprite.pixelsPerUnit);
                int y = Mathf.RoundToInt ((float)h / (layers - 1) * height * sprite.pixelsPerUnit);

                if (false == dynamicShape) continue;

                if (false == sprite.texture.isReadable) {
                    Debug.LogError ("Could not generate softbody on " + gameObject.name
                    + ". Make sure the Sprite Texture is read enabled.");
                    for (int i = 1; i <= layers * layers; i++) {
                        Destroy (transform.GetChild (transform.childCount - i).gameObject);
                    }
                }

                if (!CheckColorInRadius (x, y, 128)) {
                    col.enabled = false;
                    rb.gravityScale = 0;
                    rb.mass = 0.001f;
                }

                //if (sprite.texture.GetPixel (x, y).a <= float.Epsilon) {
                //    col.enabled = false;
                //    rb.gravityScale = 0;
                //    rb.mass = 0.001f;
                //}
            }
        }

        //Root object
        rootRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        //CircleCollider2D rootCol = gameObject.AddComponent<CircleCollider2D> ();
        //rootCol.radius = width * Mathf.Min (transform.localScale.x, transform.localScale.y) / (2 * layers);

        SpringJoint2D u = gameObject.AddComponent<SpringJoint2D> ();
        SpringJoint2D d = gameObject.AddComponent<SpringJoint2D> ();
        SpringJoint2D l = gameObject.AddComponent<SpringJoint2D> ();
        SpringJoint2D r = gameObject.AddComponent<SpringJoint2D> ();

        u.frequency = stiffness;
        d.frequency = stiffness;
        l.frequency = stiffness;
        r.frequency = stiffness;

        int mid = layers / 2;

        u.connectedBody = children[(mid + 1) * layers + mid].GetComponent<Rigidbody2D> ();
        d.connectedBody = children[(mid - 1) * layers + mid].GetComponent<Rigidbody2D> ();
        l.connectedBody = children[mid * layers + mid - 1].GetComponent<Rigidbody2D> ();
        r.connectedBody = children[mid * layers + mid + 1].GetComponent<Rigidbody2D> ();

		return true;
	}

    private bool CheckColorInRadius (int x, int y, int r) {
        int lowX = Mathf.Max (0, x - r);
        int highX = Mathf.Min (sprite.texture.width - 1, x + r);
        int lowY = Mathf.Max (0, y - r);
        int highY = Mathf.Min (sprite.texture.height - 1, y + r);

        for (int i = lowX; i <= highX; i++) {
            for (int j = lowY; j <= highY; j++) {
                //if ((i - x) * (i - x) + (j - x) * (j - x) <= r * r) {
                    if (sprite.texture.GetPixel (i, j).a > 0f) {
                        return true;
                    }
                //}
            }
        }
        return false;
    }

    public void CreateMaterial () {
        mat = new Material (Shader.Find("Sprites/Default"));
		if (null != sprite) {
			mat.mainTexture = sprite.texture;
			mat.name = sprite.texture.name;
		}
	}

    //Mesh functions
    /*************************************************************************/
    IEnumerator InitializeMesh () {
        layers = GetComponent<Softbody> ().layers;
        if (GetComponent<SpriteRenderer> ()) {
            Destroy (GetComponent<SpriteRenderer> ());
        }
        yield return new WaitForEndOfFrame ();

		if (null == (mf = GetComponent<MeshFilter>())) {
			mf = gameObject.AddComponent<MeshFilter>();
		}

		if (null == (mr = GetComponent<MeshRenderer>())) {
			mr = gameObject.AddComponent<MeshRenderer>();
		}

        mesh = new Mesh ();
        mesh.name = gameObject.name;
        mf.mesh = mesh;
        mr.materials[0] = mat;
        mr.sharedMaterial = mat;

        vertices = new List<Vector3> ();
        triangles = new List<int> ();
        points = new Transform[layers, layers];
        uvs = new List<Vector2> ();

        for (int i = 0; i < layers; i++) {
            for (int j = 0; j < layers; j++) {
                points[i, j] = (transform.GetChild (i * layers + j));
                uvs.Add (new Vector2 ((float)j / (layers - 1), (float)i / (layers - 1)));
            }
        }


        StartCoroutine (FragmentShader ());
    }

    //TODO: Replace with GPU Compute Shader
    IEnumerator FragmentShader () {
        while (true) {
            vertices.Clear ();
            foreach (var point in points) {
                vertices.Add (point.localPosition);
            }

            triangles.Clear ();
            for (int i = 0; i < vertices.Count; i++) {
                if (i % layers >= layers - 1 || i / layers >= layers - 1)
                    continue;

                triangles.Add (i);
                triangles.Add (i + layers);
                triangles.Add (i + 1);

                triangles.Add (i + 1);
                triangles.Add (i + layers);
                triangles.Add (i + layers + 1);
            }

            mesh.vertices = vertices.ToArray ();
            mesh.uv = uvs.ToArray ();
            mesh.triangles = triangles.ToArray ();
            mesh.RecalculateNormals ();
            yield return new WaitForEndOfFrame ();
        }
    }

    //Runtime functions
    /*************************************************************************/
    public void Tense () {
        StartCoroutine (TenseCR ());
    }

    IEnumerator TenseCR () {
        foreach (SpringJoint2D joint in springJoints) {
            joint.frequency *= 50f;
        }

        yield return new WaitForSeconds (1f);

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
