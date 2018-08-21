using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public struct EditorNodeRenderData
{
	public Rect NodeRect;
	public float PinVerticalOffset;
	public float InputPinHorizontalOffset;
	public float OutputPinHorizontalOffset;
	public float PinVerticalSpacing;
	public float PinSize;
}

public struct EditorPinIdentifier
{
	public EditorPinIdentifier(int _NodeID, int _PinID)
	{
		NodeID = _NodeID;
		PinID = _PinID;
	}

	public int NodeID;
	public int PinID;

	public override string ToString()
	{
		return NodeID + "." + PinID;
	}

}

[System.Serializable]
public class EditorNode
{

	public delegate void EditorNodeEvent();
	public event EditorNodeEvent OnNodeChanged;

	[SerializeField] private List<EditorPin> Pins;
	[SerializeField] private string _name;
	[SerializeField] private Vector2 _position;
	[SerializeField] private int _ID;
	private EditorNodeRenderData _renderData;
	
	private static int _nodeIDCounter = -1;

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

	public int ID
	{
		get
		{
			return _ID;
		}
	}

	public int PinCount
	{
		get
		{
			return Pins.Count;
		}
	}

	public EditorNode()
	{
		_ID = ++_nodeIDCounter;
		Pins = new List<EditorPin>();
		UpdateNodeRect();
	}

	public EditorPin GetPin(int Index)
	{
		if (Index < 0 || Index >= Pins.Count)
		{
			Debug.LogError("Attempted to get pin of invalid index " + Index + ", (max = " + Pins.Count + ")");
		}
		return Pins[Index];
	}

	public Rect GetNodeRect()
	{
		UpdateNodeRect();
		return _renderData.NodeRect;
	}

	public Rect GetPinRect(int ID)
	{
		EditorPin Pin = Pins[ID];
		EPinLinkType LinkType = Pin.GetPinLinkType();

		int TypeIndex = 1;
		for (int Index = 0; Index < ID; ++Index)
		{
			if (Pins[Index].GetPinLinkType() == LinkType)
			{
				++TypeIndex;
			}
		}

		Rect ReturnRect = new Rect();
		Vector2 RectPosition = new Vector2();
		ReturnRect.width = _renderData.PinSize;
		ReturnRect.height = _renderData.PinSize;
		RectPosition.y = _renderData.NodeRect.position.y + _renderData.PinVerticalOffset + TypeIndex * (ReturnRect.height + _renderData.PinVerticalSpacing);
		if (LinkType == EPinLinkType.Input)
		{
			RectPosition.x = _renderData.NodeRect.position.x + _renderData.InputPinHorizontalOffset;
		}
		else
		{
			RectPosition.x = _renderData.NodeRect.position.x + _renderData.OutputPinHorizontalOffset - _renderData.PinSize;
		}
		ReturnRect.position = RectPosition;

		return ReturnRect;
	}

	public Rect GetPinTextRect(int ID)
	{
		float EstimatedCharacterWidth = 8.0f;
		float EstimatedCharacterHeight = 16.0f;

		Rect PinRect = GetPinRect(ID);
		PinRect.width = (Pins[ID].GetPinName().Length+1) * EstimatedCharacterWidth;
		PinRect.height = EstimatedCharacterHeight;
		Vector2 RectPos = PinRect.position;
		if (Pins[ID].GetPinLinkType() == EPinLinkType.Input)
		{
			RectPos.x += _renderData.PinSize;
		}
		else
		{
			RectPos.x -= PinRect.width - _renderData.PinSize;
		}
		PinRect.position = RectPos;
		return PinRect;
	}

	public string GetPinName(int ID)
	{
		return Pins[ID].GetPinName();
	}

	public EditorPinIdentifier GetPinIdentifier(int ID)
	{
		EditorPinIdentifier Identifier = new EditorPinIdentifier();
		Identifier.NodeID = _ID;
		Identifier.PinID = ID;
		return Identifier;
	}

	public void SetNodePosition(Vector2 InPos)
	{
		Position = InPos;
		UpdateNodeRect();
	}

	public void UpdateNodeRect()
	{
		_renderData.NodeRect.position = Position;
		_renderData.NodeRect.width = 100.0f;
		_renderData.NodeRect.height = 16.0f + (30.0f * Mathf.Max(GetNumPins(EPinLinkType.Input), GetNumPins(EPinLinkType.Output)));
		_renderData.PinSize = 10.0f;
		_renderData.InputPinHorizontalOffset = _renderData.PinSize;
		_renderData.OutputPinHorizontalOffset = _renderData.NodeRect.width - _renderData.PinSize;
		_renderData.PinVerticalOffset = 16.0f;
		_renderData.PinVerticalSpacing = 10.0f;
	}

	private int AddPin(EPinLinkType _LinkType, System.Type _Type, string _Name)
	{
		EditorPin NewPin = new EditorPin((_Type == null) ? "null" : _Type.ToString(), _Name, ID, _LinkType);
		Pins.Add(NewPin);
		return Pins.Count-1;
	}

	private void ClearPins()
	{
		Pins.Clear();
	}

	private bool RemovePin(EditorPin _Pin)
	{
		return Pins.Remove(_Pin);
	}

	private int GetNumPins(EPinLinkType PinLinkType)
	{
		int OutNumPins = 0;
		for (int PinIndex = 0; PinIndex < PinCount; ++PinIndex)
		{
			EditorPin Pin = Pins[PinIndex];
			if (Pin.GetPinLinkType() == PinLinkType)
			{
				++OutNumPins;
			}
		}
		return OutNumPins;
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
				
				if (bHasOutput)
				{
					_Node.AddPin(EPinLinkType.Output, null, "");
				}
				if (bHasInput)
				{
					_Node.AddPin(EPinLinkType.Input, null, "");
				}

				_Node.AddPin(EPinLinkType.Output, methodInfo.ReturnParameter.ParameterType, "Output");

				ParameterInfo[] Parameters = methodInfo.GetParameters();
				foreach (ParameterInfo Parameter in Parameters)
				{
					//Debug.Log("Param type: " + Parameter.ParameterType.ToString());
					//Debug.Log("Param name: " + Parameter.Name);

					_Node.AddPin(EPinLinkType.Input, Parameter.ParameterType, Parameter.Name);
				}
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
