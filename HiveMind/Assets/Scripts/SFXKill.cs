﻿using UnityEngine;
using System.Collections;

public class SFXKill : MonoBehaviour {

	public float lifeTime = 1.0f;

	// Use this for initialization
	void Start () {
		Destroy (this.gameObject, lifeTime);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
