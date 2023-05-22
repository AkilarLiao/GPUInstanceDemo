/// <summary>
/// Author: AkilarLiao
/// Date: 2023/05/22
/// Desc:
/// </summary>
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GPUInstanceTest), true)]
public class GPUInstanceTestEditor : Editor
{	
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (GUILayout.Button("SwitchCamera"))
			((GPUInstanceTest)target).SwitchCameraTransform();
	}
}