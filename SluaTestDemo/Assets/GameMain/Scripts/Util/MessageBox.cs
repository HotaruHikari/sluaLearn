using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MessageBox {

	public MessageBox(string messageInfo, string firstText, string secondText)
	{
		UnityEngine.Object asset = Resources.Load("Prefab/MessageBox");
		go = UnityEngine.Object.Instantiate(asset) as GameObject;
		go.transform.Find("BG/MessageInfo").GetComponent<Text>().text = messageInfo;

		Transform first = go.transform.Find("BG/First");
		first.Find("Text").GetComponent<Text>().text = firstText;
		first.GetComponent<Button>().onClick.AddListener(() =>
		{
			Result = BoxResult.First;
		});
		
		Transform second = go.transform.Find("BG/Second");
		second.Find("Text").GetComponent<Text>().text = secondText;
		second.GetComponent<Button>().onClick.AddListener(() =>
		{
			Result = BoxResult.Second;
		});
	}

	/// <summary>
	/// 等待用户选择
	/// </summary>
	/// <returns></returns>
	public async Task<BoxResult> GetReplyAsync()
	{
		return await Task.Run<BoxResult>(() =>
		{
			while (true)
			{
				if (Result != BoxResult.None)
				{
					return Result;
				}
			}
		});
	}

	public void Close()
	{
		GameObject.Destroy(go);
	}
	
	public GameObject go;
	
	public BoxResult Result { get; set; }
	
	/// <summary>
	/// 弹窗框用户的选择
	/// </summary>
	public enum BoxResult
	{
		None,
		First,
		Second
	}
}
