using UnityEngine;
using System.Collections;
using System;

public class TVScreen_0 : MonoBehaviour {

	private Player_0 player;
	public int LimitLeft = -100;
	public int LimitRight = 100;

	// Use this for initialization
	void Start () {
		player = GameObject.Find("Player").GetComponent<Player_0>();
	}
	
	// Update is called once per frame
	void Update () {
		var p = this.transform.position;
		p.x = Mathf.Clamp(player.PosX, LimitLeft, LimitRight);
		this.transform.position = p;
	}
}
