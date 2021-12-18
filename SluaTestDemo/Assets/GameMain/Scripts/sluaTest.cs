using System.Collections;
using System.Collections.Generic;
using SLua;
using UnityEngine;

public class sluaTest : MonoBehaviour {

	private  static LuaState lua_state;
	void Start () {
		lua_state = new LuaState();

	}
	
	void Update () {
		
	}

}
