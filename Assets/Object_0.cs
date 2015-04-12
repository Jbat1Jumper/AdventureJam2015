using UnityEngine;
using System.Collections;
using System;

public class Object_0 : MonoBehaviour {

	[HideInInspector]
	public int PosX {
		get {
			return (int)(this.transform.position.x);
		}
		set {
			var p = this.transform.position;
			p.x = value;
			this.transform.position = p;
		}
	}

	private int RealPosY = 0;
	[HideInInspector]
	public int PosY {
		get {
			return RealPosY;
			//return (int)(this.transform.position.y / YPerspectiveRate);
		}
		set {
			RealPosY = value;
			var p = this.transform.position;
			p.y = value * YPerspectiveRate;
			p.z = (p.y - PosHeight) / YZRelation + ZOffset;
			this.transform.position = p;
		}
	}

	private float YPerspectiveRate = 0.6f;
	private float YZRelation = 7.0f;
	private float ZOffset = 3f;

	public float PosHeight = 0.0f;

	protected void ObjectInit() {
		PosY = (int)(this.transform.position.y / YPerspectiveRate);
		try{
			rpSprite = GetComponent<RagePixelSprite>();
		}catch(Exception){
        }
	}
	
	[HideInInspector]
	public RagePixelSprite rpSprite = null;

	// Use this for initialization
	void Start () {
		ObjectInit();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
