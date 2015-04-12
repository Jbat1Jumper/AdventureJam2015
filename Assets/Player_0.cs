using UnityEngine;
using System.Collections;

public class Player_0 : Object_0 {

	// Use this for initialization
	void Start () {
		ObjectInit();

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Walk();
	}

	private float speed = 2f;
	private bool CanWalkNow = false;
	void Walk ()
	{
		CanWalkNow = ! CanWalkNow;
		if(!CanWalkNow)
			return;
		var dir = new Vector2(0, 0);
		if(Input.GetKey(KeyCode.A))
			dir.x -= 1;
		if(Input.GetKey(KeyCode.S))
			dir.y -= 1;
		if(Input.GetKey(KeyCode.D))
			dir.x += 1;
        if(Input.GetKey(KeyCode.W))
			dir.y += 1;

		dir = dir * speed;

		dir = AvailableDirections(dir);


		PosX += (int)dir.x;
		PosY += (int)dir.y;
		Animate(dir);
    }

	Vector2 AvailableDirections (Vector2 dir)
	{
		var finaldir = new Vector2(dir.x, dir.y);

		var objects = GameObject.FindObjectsOfType<Object_0>();
		var my_col = GetComponent<BoxCollider2D>();
		foreach(var obj in objects) {
			if(obj == this)
				continue;
			if(obj.GetComponent("BoxCollider2D") == null)
				continue;
			BoxCollider2D[] obj_cols;
			try{
				obj_cols = obj.GetComponents<BoxCollider2D>();
			}catch(UnityException) {
				continue;
			}

			foreach(var obj_col in obj_cols) {
				if(obj_col.isTrigger)
					continue;

				var o = new Rect(obj_col.bounds.min.x, 
				                 obj_col.bounds.min.y, 
				                 obj_col.bounds.size.x, 
				                 obj_col.bounds.size.y);
				var mx = new Rect(my_col.bounds.min.x + dir.x, 
				                 my_col.bounds.min.y, 
				                 my_col.bounds.size.x, 
				                  my_col.bounds.size.y);
				var my = new Rect(my_col.bounds.min.x, 
				                  my_col.bounds.min.y + dir.y, 
				                  my_col.bounds.size.x, 
				                  my_col.bounds.size.y);
				if(o.Overlaps(mx))
					finaldir.x = 0;
				if(o.Overlaps(my))
					finaldir.y = 0;
			}
		}
		return finaldir;
	}

	private Vector2 lastdir = new Vector2(0, 0);
	void Animate (Vector2 dir)
	{
		if(dir.x == lastdir.x)
			return;
		if(dir.x > 0) {
			rpSprite.flipHorizontal = false;
			rpSprite.PlayNamedAnimation("Walk", false);
		} else
		if(dir.x < 0) {
			rpSprite.flipHorizontal = true;
			rpSprite.PlayNamedAnimation("Walk", false);
		} else 
		{
			rpSprite.PlayNamedAnimation("Stand", false);
		}
		lastdir = dir;
	}
}
