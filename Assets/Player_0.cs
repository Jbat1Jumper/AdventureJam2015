using UnityEngine;
using System.Collections;

public class Player_0 : Object_0 {

	// Use this for initialization
	void Start () {
		PosInit();

	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKey(KeyCode.A))
			PosX -= 1;
		if(Input.GetKey(KeyCode.S))
			PosY -= 1;
		if(Input.GetKey(KeyCode.D))
			PosX += 1;
		if(Input.GetKey(KeyCode.W))
			PosY += 1;
	}
}
