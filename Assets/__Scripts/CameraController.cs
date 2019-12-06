using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
	static public List<CameraPoint>	CAM_POINTS = new List<CameraPoint>();

	static public void ADD_CAM_POINT(CameraPoint cp) {
		if (!CAM_POINTS.Contains (cp)) {
			CAM_POINTS.Add (cp);
		}
	}

	static public void REMOVE_CAM_POINT(CameraPoint cp) {
		CAM_POINTS.Remove (cp);
	}

	[Header("Inscribed")]
	public bool			switchViews = true;
	public float		easing = 0.25f;
	public float 		camSwitchDelay = 2;

	[Header("Dynamic")]
	public CameraPoint	currPoint;

	// Use this for initialization
	void Start () {
		currPoint = GetComponent<CameraPoint> ();
		StartCoroutine (SwitchCamCoroutine());
	}
	
	// Update is called once per frame
	void Update () {
		if (!CAM_POINTS.Contains (currPoint)) {
			// The currPoint has been destroyed, so don't move
			currPoint = null;
		}

		if (currPoint != null) {
			transform.position = Vector3.Lerp (transform.position, currPoint.loc, easing);
			transform.rotation = Quaternion.Lerp (transform.rotation, currPoint.rot, easing);
		}
	}

	IEnumerator SwitchCamCoroutine() {
		while (true) {
			yield return new WaitForSeconds (camSwitchDelay);
			// choose a new camera
			if (switchViews && CAM_POINTS.Count > 1) {
				int i;
				do {
					i = Random.Range (0, CAM_POINTS.Count);
				} while (CAM_POINTS [i] == currPoint);
				currPoint = CAM_POINTS [i];
			}
		}
	}
}
