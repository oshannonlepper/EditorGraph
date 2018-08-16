using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GraphEditorWindow : EditorWindow {

	private Dictionary<EditorPin, EditorPin> LinkDictionary;
	private Dictionary<EditorPin, EditorNode> PinOwnerDictionary;
	private Dictionary<System.Type, List<string>> RegisteredFunctionDictionary;
	private Dictionary<System.Type, bool> ShowFunctions;
	private List<EditorNode> NodeList = new List<EditorNode>();
	private List<EditorLink> LinkList = new List<EditorLink>();
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
							int NodeID = GraphToEdit.AddNode(EditorNode.CreateFromFunction(LibraryType, MethodName));
							EditorNode Node = GraphToEdit.GetNodeFromID(NodeID);
							Node.SetNodePosition(new Vector2(Random.Range(0, 200), Random.Range(0, 200)));
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

			foreach (var _Link in LinkList)
			{
				RenderLink(_Link);
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
		NodeList = GraphToEdit.GetNodeList();
	}

	private void RenderNode(EditorNode _Node)
	{
		Rect NodeRect = _Node.GetNodeRect();
		Debug.Log("NodeRect = " + NodeRect);
		DrawRect(NodeRect.min, NodeRect.max, Color.gray);
	}

	private void RenderNodePins(EditorNode _Node)
	{
		int NumPins = _Node.PinCount;
		for (int PinIndex = 0; PinIndex < NumPins; ++PinIndex)
		{
			Rect PinRect = _Node.GetPinRect(PinIndex);
			DrawRect(PinRect.min, PinRect.max, Color.green);
		}
	}

	private void RenderNodeText(EditorNode _Node)
	{
		Rect NodeRect = _Node.GetNodeRect();
		NodeRect.height = 16.0f;
		EditorGUI.LabelField(NodeRect, _Node.Name);

		int NumPins = _Node.PinCount;
		for (int PinIndex = 0; PinIndex < NumPins;  ++PinIndex)
		{
			EditorGUI.LabelField(_Node.GetPinTextRect(PinIndex), _Node.GetPinName(PinIndex));
		}
	}

	private void RenderLink(EditorLink _Link)
	{
		EditorNode FromNode = null;
		EditorNode ToNode = null;

		int NumNodes = NodeList.Count;
		for (int NodeIndex = 0; NodeIndex < NumNodes; ++NodeIndex)
		{
			if (NodeList[NodeIndex].ID == _Link.NodeID_From)
			{
				FromNode = NodeList[NodeIndex];
				continue;
			}
			if (NodeList[NodeIndex].ID == _Link.NodeID_To)
			{
				ToNode = NodeList[NodeIndex];
				continue;
			}
		}

		Vector2 From = FromNode.GetPinRect(_Link.PinID_From).position;
		Vector2 To = ToNode.GetPinRect(_Link.PinID_To).position;
		
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
