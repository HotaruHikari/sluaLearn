using System.Collections;
using System.Collections.Generic;
using SLua;
using UnityEngine;

public class LuaBehaviour : MonoBehaviour {

	private string V_LuaFilePath;

	/// <summary>
	/// 对应的lua脚本对象
	/// </summary>
	public LuaTable self;

	[CustomLuaClass]
	public delegate void UpdateDelegate(object self);

	UpdateDelegate ud;
	UpdateDelegate enabled;
	UpdateDelegate disabled;
	UpdateDelegate destroyd;

	public void SetLuaScript(string path)
	{
		V_LuaFilePath = path;
	}
	
	// Use this for initialization
	void Start ()
	{
		if (!string.IsNullOrEmpty(V_LuaFilePath))
		{
			self = LuaManager.Instance.StartLua(V_LuaFilePath);
		}
		//LuaState.main.doFile(V_LuaFilePath);
		//self = (LuaTable)LuaState.main.run("main");
		
		var update = (LuaFunction)self["update"];
		var destroy = (LuaFunction)self["destroy"];
		var enablef = (LuaFunction)self["enable"];
		var disablef = (LuaFunction)self["disable"];
		if (update != null) ud = update.cast<UpdateDelegate>();
		if (destroy != null) destroyd = destroy.cast<UpdateDelegate>();
		if (enablef != null) enabled = enablef.cast<UpdateDelegate>();
		if (disablef != null) disabled = disablef.cast<UpdateDelegate>();
	}
    
	void OnEnable()
	{
		if (enabled != null) enabled(self);
	}

	void OnDiable()
	{
		if (disabled != null) disabled(self);
	}
	
	void Update () {
		if (ud != null) ud(self);
	}
    
	void OnDestroy()
	{
		if (destroyd != null) destroyd(self);
	}
}

