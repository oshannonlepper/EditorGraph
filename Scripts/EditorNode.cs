using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

[System.Serializable]
public class EditorNode
{

	public delegate void EditorNodeEvent();
	public event EditorNodeEvent OnNodeChanged;

	[SerializeField] private List<EditorPin> Pins;
//	[SerializeField] private EditorPin InFlowPin = null;
//	[SerializeField] private EditorPin OutFlowPin = null;
	[SerializeField] private string _name;
	[SerializeField] private Vector2 _position;
	private Rect _boundsRect;

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public Vector2 Position
	{
		get
		{
			return _position;
		}
		set
		{
			_position = value;
		}
	}

	public EditorNode()
	{
		InPins = new List<EditorPin>();
		OutPins = new List<EditorPin>();
	}

	public Rect GetNodeRect()
	{
		UpdateNodeRect();
		return _boundsRect;
	}

	public void SetNodePosition(Vector2 InPos)
	{
		Position = InPos;
		UpdateNodeRect();
	}

	public void UpdateNodeRect()
	{
		if (_boundsRect == null)
		{
			_boundsRect = new Rect();
		}
		_boundsRect.position = Position;
		_boundsRect.width = 100.0f;
		_boundsRect.height = 40.0f + (30.0f * Mathf.Max(InPins.Count+1, OutPins.Count+1));
	}

	public void AddInput(EditorPin _Pin)
	{
		InPins.Add(_Pin);
		NotifyGraphChange();
	}

	public void RemoveInput(EditorPin _Pin)
	{
		InPins.Remove(_Pin);
		NotifyGraphChange();
	}

	public EditorPin GetInput(int ID)
	{
		return InPins[ID];
	}

	public int GetNumInputs()
	{
		return InPins.Count;
	}

	public void AddOutput(EditorPin _Pin)
	{
		OutPins.Add(_Pin);
		NotifyGraphChange();
	}

	public bool RemoveOutput(EditorPin _Pin)
	{
		bool bRemoved = OutPins.Remove(_Pin);
		if (bRemoved)
		{
			NotifyGraphChange();
		}
		return bRemoved;
	}

	public EditorPin GetOutput(int ID)
	{
		return OutPins[ID];
	}

	public int GetNumOutputs()
	{
		return OutPins.Count;
	}

	public PinData GetPinData(EditorPin _Pin)
	{
		PinData Data = new PinData();
		Data.Number = -1;

		if (OutPins.Contains(_Pin))
		{
			Data.Number = OutPins.IndexOf(_Pin);
			Data.bIsInput = false;
		}
		else if (InPins.Contains(_Pin))
		{
			Data.Number = InPins.IndexOf(_Pin);
			Data.bIsInput = true;
		}

		return Data;
	}

	public void SetHasFlowInput(bool bHasInput)
	{
		if (!bHasInput)
		{
			InFlowPin = null;
		}
		else if (InFlowPin == null)
		{
			InFlowPin = new EditorPin();
		}
		NotifyGraphChange();
	}

	public void SetHasFlowOutput(bool bHasOutput)
	{
		if (!bHasOutput)
		{
			OutFlowPin = null;
		}
		else if (OutFlowPin == null)
		{
			OutFlowPin = new EditorPin();
		}
		NotifyGraphChange();
	}

	public EditorPin GetInputFlow()
	{
		return InFlowPin;
	}

	public EditorPin GetOutputFlow()
	{
		return OutFlowPin;
	}

	private void NotifyGraphChange()
	{
		if (OnNodeChanged != null)
		{
			OnNodeChanged();
		}
	}

	public static EditorNode CreateFromFunction(System.Type ClassType, string Methodname, bool bHasOutput = true, bool bHasInput = true)
	{
		EditorNode _Node = new EditorNode();
		_Node.Position = new Vector2(Random.Range(0.0f, 300.0f), Random.Range(50.0f, 450.0f));

		if (ClassType != null)
		{
			MethodInfo methodInfo = ClassType.GetMethod(Methodname);

			if (methodInfo != null)
			{
				_Node.Name = SanitizeName(Methodname);
				//Debug.Log("Method name: " + _Node.Name);
				//Debug.Log("Return type: " + methodInfo.ReturnParameter.ParameterType.ToString());
				//Debug.Log("Return name: " + methodInfo.ReturnParameter.Name);

				EditorPin ReturnPin = new EditorPin(methodInfo.ReturnParameter.ParameterType.ToString(), "Output");
				_Node.AddOutput(ReturnPin);

				ParameterInfo[] Parameters = methodInfo.GetParameters();
				foreach (ParameterInfo Parameter in Parameters)
				{
					//Debug.Log("Param type: " + Parameter.ParameterType.ToString());
					//Debug.Log("Param name: " + Parameter.Name);

					EditorPin InputPin = new EditorPin(Parameter.ParameterType.ToString(), Parameter.Name);
					_Node.AddInput(InputPin);
				}

				_Node.SetHasFlowOutput(bHasOutput);
				_Node.SetHasFlowInput(bHasInput);
			}
			else
			{
				Debug.LogError("Function '" + ClassType.ToString() + "." + Methodname + "' not found.");
			}
		}
		else
		{
			Debug.LogError("Tried to create node from function from an unknown class type.");
		}

		_Node.NotifyGraphChange();
		return _Node;
	}

	private static string SanitizeName(string Name)
	{
		string Result = "" + Name[0];

		bool bWasCapital = Name[0] >= 'A' && Name[0] <= 'Z';
		for (int i = 1; i < Name.Length; ++i)
		{
			if (Name[i] >= 'A' && Name[i] <= 'Z')
			{
				if (!bWasCapital)
				{
					Result += " " + Name[i];
				}
				else
				{
					Result += Name[i];
				}
				bWasCapital = true;
			}
			else
			{
				Result += Name[i];
				bWasCapital = false;
			}
		}

		return Result;
	}

}
