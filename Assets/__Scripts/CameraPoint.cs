using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPoint : MonoBehaviour {
	public enum eType
	{
		still,
		follow
	}

	public eType		camType;
	[SerializeField]
	private Vector3		_loc;
	[SerializeField]
	private Quaternion 	_rot;
	private Agent		ag;

	// Use this for initialization
	void Start () {
		ag = GetComponentInParent<Agent> (); // ag could be null, but that's fine.

		switch (camType) {
		case eType.still:
			loc = transform.position;
			rot = transform.rotation;
			break;

		case eType.follow:
			loc = transform.localPosition; // This is probably never used
			rot = transform.localRotation; // This is probably never used
			break;
		}

		// Register with CameraController
		CameraController.ADD_CAM_POINT(this);
	}

	void OnDestroy() {
		CameraController.REMOVE_CAM_POINT (this);
	}

	public Vector3 loc {
		get {
			switch (camType) {
			case eType.follow:
				return transform.position;

			case eType.still:
			default:
				return _loc;
			}
		}
		set {
			_loc = value;
		}
	}

	public Quaternion rot {
		get {
			switch (camType) {
			case eType.follow:
				return transform.rotation;

			case eType.still:
			default:
				return _rot;
			}
		}
		set {
			_rot = value;
		}
	}
			
}
