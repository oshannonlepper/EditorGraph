using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GraphEditorWindow : EditorWindow {

	private Dictionary<EditorPin, EditorPin> LinkDictionary;
	private Dictionary<EditorPin, EditorNode> PinOwnerDictionary;
	private Dictionary<System.Type, List<string>> RegisteredFunctionDictionary;
	private Dictionary<System.Type, bool> ShowFunctions;
	private List<EditorNode> NodeList;
	private EditorGraph GraphToEdit;
	private int controlId = -1;
	private Material mat;
	private int CurrentNodeFocus = -1;
	private Vector2 OldMousePosition;
	private Vector2 MousePosition;
	private bool bNodeMoved = false;

	[MenuItem("Window/Graph Editor")]
	public static void ShowGraphEditor()
	{
		EditorWindow.GetWindow(typeof(GraphEditorWindow));
	}

	private void OnGUI()
	{
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("New Graph"))
		{
			EditorGraph NewGraph = ScriptableObject.CreateInstance<EditorGraph>();

			AssetDatabase.CreateAsset(NewGraph, "Assets/Graphs/NewGraph.asset");
			AssetDatabase.SaveAssets();

			SetGraph(NewGraph);
		}

		if (GUILayout.Button("Load Graph"))
		{
			controlId = GUIUtility.GetControlID(FocusType.Passive);
			EditorGUIUtility.ShowObjectPicker<EditorGraph>(null, false, "", controlId);
		}

		GUILayout.EndHorizontal();

		if (GraphToEdit != null)
		{
			GUILayout.BeginVertical(GUILayout.Width(100.0f));

			UpdateFunctionMap();
			if (ShowFunctions == null)
			{
				ShowFunctions = new Dictionary<System.Type, bool>();
			}

			foreach (System.Type LibraryType in RegisteredFunctionDictionary.Keys)
			{
				if (GUILayout.Button(LibraryType.ToString()))
				{
					if (!ShowFunctions.ContainsKey(LibraryType))
					{
						ShowFunctions[LibraryType] = true;
					}
					else
					{
						ShowFunctions[LibraryType] = !ShowFunctions[LibraryType];
					}
				}

				if (ShowFunctions.ContainsKey(LibraryType) && ShowFunctions[LibraryType])
				{
					foreach (string MethodName in RegisteredFunctionDictionary[LibraryType])
					{
						if (GUILayout.Button(MethodName))
						{
							GraphToEdit.AddNode(EditorNode.CreateFromFunction(LibraryType, MethodName));
						}
					}
				}
			}

			GUILayout.EndVertical();
		}

		if (Event.current.commandName.Equals("ObjectSelectorUpdated") && EditorGUIUtility.GetObjectPickerControlID() == controlId)
		{
			SetGraph(EditorGUIUtility.GetObjectPickerObject() as EditorGraph);
			controlId = -1;
		}

		if (GraphToEdit != null)
		{
			UpdateGraphCache();

			if (CurrentNodeFocus == -1)
			{
				if (Event.current.type == EventType.MouseDown)
				{
					MousePosition = Event.current.mousePosition;

					foreach (EditorNode _Node in NodeList)
					{
						Rect NodeRect = _Node.GetNodeRect();
						if (MousePosition.x >= NodeRect.min.x && MousePosition.x <= NodeRect.max.x &&
							MousePosition.y >= NodeRect.min.y && MousePosition.y <= NodeRect.max.y)
						{
							CurrentNodeFocus = NodeList.IndexOf(_Node);
							bNodeMoved = false;
							break;
						}
					}
				}
			}
			else
			{
				if (Event.current.type == EventType.MouseUp)
				{
					if (bNodeMoved)
					{
						SaveGraph();
					}

					CurrentNodeFocus = -1;
				}
				else if (Event.current.type == EventType.MouseDrag)
				{
					OldMousePosition = MousePosition;
					MousePosition = Event.current.mousePosition;
					Vector2 delta = MousePosition - OldMousePosition;

					NodeList[CurrentNodeFocus].SetNodePosition(NodeList[CurrentNodeFocus].Position + delta);
					NodeList[CurrentNodeFocus].UpdateNodeRect();
					EditorUtility.SetDirty(GraphToEdit);
					RenderGraph();
					Repaint();
					bNodeMoved = true;
				}
			}

			RenderGraph();
		}
	}

	private void UpdateFunctionMap()
	{
		RegisteredFunctionDictionary = new Dictionary<System.Type, List<string>>();

		List<System.Type> SubclassList = new List<System.Type>();
		TypeUtilities.GetAllSubclasses(typeof(FunctionLibrary), SubclassList);

		foreach (System.Type Subclass in SubclassList)
		{
			RegisteredFunctionDictionary[Subclass] = new List<string>();
			System.Reflection.MethodInfo[] methodInfos = Subclass.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			foreach (System.Reflection.MethodInfo methodInfo in methodInfos)
			{
				RegisteredFunctionDictionary[Subclass].Add(methodInfo.Name);
			}
		}
	}

	public void SetGraph(EditorGraph _Graph)
	{
		EditorGraph OldGraph = GraphToEdit;
		GraphToEdit = _Graph;
		if (GraphToEdit != null)
		{
			if (OldGraph != null && OldGraph != GraphToEdit)
			{
				OldGraph.OnGraphChanged -= RenderGraph;
			}

			UpdateGraphCache();

			GraphToEdit.OnGraphChanged += RenderGraph;
		}
	}

	public void OnGraphChanged()
	{
		UpdateGraphCache();

		SaveGraph();

		RenderGraph();
	}

	private void SaveGraph()
	{
		if (GraphToEdit != null)
		{
			EditorUtility.SetDirty(GraphToEdit);
			AssetDatabase.SaveAssets();
		}
	}

	public void RenderGraph()
	{
		if (GraphToEdit != null)
		{
			foreach (EditorNode _Node in NodeList)
			{
				RenderNode(_Node);
			}

			foreach (var PinPair in LinkDictionary)
			{
				RenderLink(PinPair.Key, PinPair.Value);
			}

			foreach (EditorNode _Node in NodeList)
			{
				RenderNodePins(_Node);
			}

			foreach (EditorNode _Node in NodeList)
			{
				RenderNodeText(_Node);
			}
		}
	}

	private void UpdateGraphCache()
	{
		PinOwnerDictionary = new Dictionary<EditorPin, EditorNode>();
		NodeList = GraphToEdit.GetNodeList();
		foreach (EditorNode _Node in NodeList)
		{
			EditorPin InFlowPin = _Node.GetInputFlow();
			EditorPin OutFlowPin = _Node.GetOutputFlow();
			if (InFlowPin != null)
			{
				PinOwnerDictionary.Add(InFlowPin, _Node);
			}
			if (OutFlowPin != null)
			{
				PinOwnerDictionary.Add(OutFlowPin, _Node);
			}

			int NumInputs = _Node.GetNumInputs();
			int NumOutputs = _Node.GetNumOutputs();

			for (int Input = 0; Input < NumInputs; ++Input)
			{
				EditorPin InputPin = _Node.GetInput(Input);
				PinOwnerDictionary.Add(InputPin, _Node);
			}

			for (int Output = 0; Output < NumOutputs; ++Output)
			{
				EditorPin OutputPin = _Node.GetOutput(Output);
				PinOwnerDictionary.Add(OutputPin, _Node);
			}
		}

		LinkDictionary = new Dictionary<EditorPin, EditorPin>();
		List<EditorLink> LinkList = GraphToEdit.GetLinkList();
		foreach (EditorLink _Link in LinkList)
		{
			LinkDictionary.Add(_Link.FromPin, _Link.ToPin);
		}
	}

	private void RenderNode(EditorNode _Node)
	{
		Rect NodeRect = _Node.GetNodeRect();
		DrawRect(NodeRect.min, NodeRect.max, Color.gray);
	}

	private void RenderNodePins(EditorNode _Node)
	{
		int NumInputs = _Node.GetNumInputs();
		int NumOutputs = _Node.GetNumOutputs();
		RenderPin(_Node.GetInputFlow(), true, -1, Color.white);
		RenderPin(_Node.GetOutputFlow(), false, -1, Color.white);

		for (int Input = 0; Input < NumInputs; ++Input)
		{
			RenderPin(_Node.GetInput(Input), true, Input, Color.cyan);
		}
		for (int Output = 0; Output < NumOutputs; ++Output)
		{
			RenderPin(_Node.GetOutput(Output), false, Output, Color.red);
		}
	}

	private void RenderNodeText(EditorNode _Node)
	{
		int NumInputs = _Node.GetNumInputs();
		int NumOutputs = _Node.GetNumOutputs();
		Rect NodeRect = _Node.GetNodeRect();

		EditorGUI.LabelField(new Rect(NodeRect.min.x + NodeRect.width * 0.5f, NodeRect.min.y - 20.0f, NodeRect.height, 20.0f), _Node.Name);

		for (int Input = 0; Input < NumInputs; ++Input)
		{
			RenderPinText(_Node.GetInput(Input), true, Input, Color.cyan);
		}
		for (int Output = 0; Output < NumOutputs; ++Output)
		{
			RenderPinText(_Node.GetOutput(Output), false, Output, Color.red);
		}
	}

	private void RenderPin(EditorPin _Pin, bool bIsInput, int Number, Color InColor)
	{
		if (_Pin == null)
		{
			return;
		}

		Vector2 PinTopLeft = GetPinPosition(_Pin, bIsInput, Number);
		Vector2 PinBottomRight = PinTopLeft + Vector2.one * 10.0f;

		DrawRect(PinTopLeft, PinBottomRight, InColor);
	}

	private void RenderPinText(EditorPin _Pin, bool bIsInput, int Number, Color InColor)
	{
		if (_Pin == null)
		{
			return;
		}

		Vector2 PinTopLeft = GetPinPosition(_Pin, bIsInput, Number) + Vector2.right * 20.0f;
		Vector2 PinBottomRight = PinTopLeft + Vector2.one * 10.0f;

		GUIStyle style = new GUIStyle();
		style.normal.textColor = InColor;
		EditorGUI.LabelField(new Rect(PinTopLeft.x, PinTopLeft.y, 80.0f, 20.0f), _Pin.GetPinName(), style);
	}

	private void RenderLink(EditorPin FromPin, EditorPin ToPin)
	{
		PinData LHSData = GetOwner(FromPin).GetPinData(FromPin);
		PinData RHSData = GetOwner(ToPin).GetPinData(ToPin);

		Vector2 From = GetPinPosition(FromPin, LHSData.bIsInput, LHSData.Number);
		Vector2 To = GetPinPosition(ToPin, RHSData.bIsInput, RHSData.Number);
		
		GL.Color(Color.green);
		GL.Begin(GL.LINES);
		GL.Vertex3(From.x, From.y, 0.0f);
		GL.Vertex3(To.x, To.y, 0.0f);
		GL.End();
	}

	private void DrawRect(Vector2 TopLeft, Vector2 BottomRight, Color Fill)
	{
		EditorGUI.DrawRect(new Rect(TopLeft, BottomRight - TopLeft), Fill);
	}

	private Vector2 GetPinPosition(EditorPin _Pin, bool bIsInput, int Number)
	{
		if (_Pin == null)
		{
			return Vector2.zero;
		}

		EditorNode Owner = GetOwner(_Pin);
		Vector2 PinTopLeft = new Vector2();
		if (Owner != null)
		{
			PinTopLeft = Owner.Position;
		}

		PinTopLeft.y += 20.0f + (30.0f * Number);
		PinTopLeft.x += bIsInput ? 10.0f : 70.0f;

		return PinTopLeft;
	}

	private EditorNode GetOwner(EditorPin _Pin)
	{
		if (_Pin == null)
		{
			return null;
		}
		if (!PinOwnerDictionary.ContainsKey(_Pin))
		{
			Debug.LogError("No owner for pin found.");
			return null;
		}
		return PinOwnerDictionary[_Pin];
	}

}
