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
	private EditorPinIdentifier CurrentPinFocus;
	private Vector2 OldMousePosition;
	private Vector2 MousePosition;
	private bool bNodeMoved = false;
	private bool bWasJustClicked = true;

	[MenuItem("Window/Graph Editor")]
	public static void ShowGraphEditor()
	{
		GraphEditorWindow window = EditorWindow.GetWindow(typeof(GraphEditorWindow)) as GraphEditorWindow;
		window.Reset();
	}

	public void Reset()
	{
		CurrentNodeFocus = -1;
		CurrentPinFocus.PinID = -1;
		bNodeMoved = false;
		bWasJustClicked = true;
	}

	private bool HitTestPointToRect(Vector2 InPoint, Rect InRect)
	{
		if (InPoint.x >= InRect.min.x && InPoint.x <= InRect.max.x &&
			InPoint.y >= InRect.min.y && InPoint.y <= InRect.max.y)
		{
			return true;
		}
		return false;
	}

	private void Line(Vector2 From, Vector2 To, Color InColor)
	{
		Handles.BeginGUI();
		Handles.color = InColor;
		Handles.DrawLine(From, To);
		Handles.EndGUI();
		Repaint();
	}

	private void OnGUI()
	{
		if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
		{
			OldMousePosition = MousePosition;
			MousePosition = Event.current.mousePosition;
		}

		if (CurrentPinFocus.PinID != -1)
		{
			Vector2 PinPos = GraphToEdit.GetNodeFromID(CurrentPinFocus.NodeID).GetPinRect(CurrentPinFocus.PinID).position;
			Line(PinPos, MousePosition, Color.magenta);
		}

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

			if (Event.current.type == EventType.MouseDown)
			{
				if (bWasJustClicked)
				{
					MousePosition = Event.current.mousePosition;

					foreach (EditorNode _Node in NodeList)
					{
						int NumPins = _Node.PinCount;
						for (int PinIndex = 0; PinIndex < NumPins; ++PinIndex)
						{
							Rect PinRect = _Node.GetPinRect(PinIndex);
							if (HitTestPointToRect(MousePosition, PinRect))
							{
								CurrentPinFocus = _Node.GetPinIdentifier(PinIndex);
								break;
							}
						}

						if (CurrentPinFocus.PinID == -1)
						{
							Rect NodeRect = _Node.GetNodeRect();
							if (HitTestPointToRect(MousePosition, NodeRect))
							{
								CurrentNodeFocus = NodeList.IndexOf(_Node);
								Debug.Log("Picked " + CurrentNodeFocus);
								bNodeMoved = false;
								break;
							}
						}
					}
					bWasJustClicked = false;
				}
			}

			bool bRefresh = false;

			if (CurrentPinFocus.PinID != -1 || CurrentNodeFocus != -1)
			{
				if (Event.current.type == EventType.MouseUp)
				{
					if (bNodeMoved)
					{
						EditorUtility.SetDirty(GraphToEdit);
						SaveGraph();
					}
					bRefresh = true;
				}
				else if (Event.current.type == EventType.MouseDrag)
				{
					if (CurrentNodeFocus != -1)
					{
						Vector2 delta = MousePosition - OldMousePosition;

						NodeList[CurrentNodeFocus].SetNodePosition(NodeList[CurrentNodeFocus].Position + delta);
						NodeList[CurrentNodeFocus].UpdateNodeRect();
						RenderGraph();
						bNodeMoved = true;
					}

					Repaint();
				}
			}

			if (bRefresh)
			{
				CurrentNodeFocus = -1;
				CurrentPinFocus.PinID = -1;
				bWasJustClicked = true;
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

		Line(From, To, Color.green);
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
