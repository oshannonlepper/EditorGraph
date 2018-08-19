using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GraphEditorWindow : EditorWindow, IGraphInputListener, IGraphInputHandler {

	private Dictionary<System.Type, List<string>> RegisteredFunctionDictionary;
	private Dictionary<System.Type, bool> ShowFunctions;
	private List<EditorNode> NodeList = new List<EditorNode>();
	private List<EditorLink> LinkList = new List<EditorLink>();
	private EditorGraph GraphToEdit;
	private int controlId = -1;

	private EditorNode SelectedNode = null;
	private EditorPinIdentifier SelectedPin = new EditorPinIdentifier(-1, -1);
	private Vector2 oldDragPosition = new Vector2();

	[MenuItem("Window/Graph Editor")]
	public static void ShowGraphEditor()
	{
		GraphEditorWindow window = EditorWindow.GetWindow(typeof(GraphEditorWindow)) as GraphEditorWindow;
		window.Reset();
	}

	public void Reset()
	{
		SelectedPin.NodeID = -1;
		SelectedPin.PinID = -1;
		UpdateFunctionMap();
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
		OnGUI_Buttons();

		if (GraphToEdit == null)
		{
			Reset();
			return;
		}

		Update();

		if (GraphToEdit != null)
		{
			if (RegisteredFunctionDictionary == null)
			{
				Reset();
			}
			OnGUI_FunctionList();
		}

		RenderGraph();

		if (GraphToEdit != null && SelectedPin.PinID != -1)
		{
			EditorNode OwnerNode = GraphToEdit.GetNodeFromID(SelectedPin.NodeID);
			Vector2 PinPos = OwnerNode.GetPinRect(SelectedPin.PinID).center;
			Line(PinPos, Event.current.mousePosition, Color.magenta);
		}

	}

	private void OnGUI_Buttons()
	{
		GUILayout.BeginHorizontal();

		if (GUILayout.Button("New Graph"))
		{
			EditorGraph NewGraph = ScriptableObject.CreateInstance<EditorGraph>();
			AssetDatabase.CreateAsset(NewGraph, "Assets/Graphs/NewGraph.asset");
			AssetDatabase.SaveAssets();
			OnGraphLoaded(NewGraph);
		}

		if (GUILayout.Button("Load Graph"))
		{
			controlId = GUIUtility.GetControlID(FocusType.Passive);
			EditorGUIUtility.ShowObjectPicker<EditorGraph>(null, false, "", controlId);
		}

		if (Event.current.commandName.Equals("ObjectSelectorClosed"))
		{
			OnGraphLoaded(EditorGUIUtility.GetObjectPickerObject() as EditorGraph);
			controlId = -1;
		}

		GUILayout.EndHorizontal();
	}

	private void OnGUI_FunctionList()
	{
		GUILayout.BeginVertical(GUILayout.Width(100.0f));

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

	private void UpdateFunctionMap()
	{
		UpdateGraphCache();

		RegisteredFunctionDictionary = new Dictionary<System.Type, List<string>>();
		ShowFunctions = new Dictionary<System.Type, bool>();

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

			foreach (EditorNode _Node in NodeList)
			{
				RenderNodePins(_Node);
			}

			foreach (var _Link in LinkList)
			{
				RenderLink(_Link);
			}

			foreach (EditorNode _Node in NodeList)
			{
				RenderNodeText(_Node);
			}
		}
	}

	public void OnGraphLoaded(EditorGraph graph)
	{
		SetGraph(graph);
		UpdateFunctionMap();
	}

	private void UpdateGraphCache()
	{
		NodeList = GraphToEdit.GetNodeList();
		LinkList = GraphToEdit.GetLinkList();
	}

	private void RenderNode(EditorNode _Node)
	{
		float selectionBorder = 5.0f;
		Rect NodeRect = _Node.GetNodeRect();
		if (_Node == SelectedNode)
		{
			DrawRect(NodeRect.min - Vector2.one * selectionBorder, NodeRect.max + Vector2.one * selectionBorder, Color.yellow);
		}
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
			if (FromNode != null && ToNode != null)
			{
				break;
			}
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

		Vector2 From = FromNode.GetPinRect(_Link.PinID_From).center;
		Vector2 To = ToNode.GetPinRect(_Link.PinID_To).center;

		Line(From, To, Color.black);
	}

	private void DrawRect(Vector2 TopLeft, Vector2 BottomRight, Color Fill)
	{
		EditorGUI.DrawRect(new Rect(TopLeft, BottomRight - TopLeft), Fill);
	}

	public void OnMouseDown(int button, Vector2 mousePos)
	{
		if (button == 0)
		{
			foreach (EditorNode _Node in NodeList)
			{
				bool bPinSelected = false;
				int NumPins = _Node.PinCount;
				for (int PinIndex = 0; PinIndex < NumPins; ++PinIndex)
				{
					Rect PinRect = _Node.GetPinRect(PinIndex);
					if (HitTestPointToRect(mousePos, PinRect))
					{
						SelectPin(_Node.GetPinIdentifier(PinIndex));
						bPinSelected = true;
						break;
					}
				}

				if (bPinSelected)
				{
					break;
				}

				Rect NodeRect = _Node.GetNodeRect();
				if (HitTestPointToRect(mousePos, NodeRect))
				{
					SelectNode(_Node.ID);
					break;
				}
			}
		}
	}

	public void OnMouseUp(int button, Vector2 mousePos)
	{
		if (button == 0)
		{
			foreach (EditorNode _Node in NodeList)
			{
				int NumPins = _Node.PinCount;
				for (int PinIndex = 0; PinIndex < NumPins; ++PinIndex)
				{
					Rect PinRect = _Node.GetPinRect(PinIndex);
					if (HitTestPointToRect(mousePos, PinRect))
					{
						EditorNode OtherNode = GraphToEdit.GetNodeFromID(SelectedPin.NodeID);
						LinkPins(OtherNode.GetPinIdentifier(SelectedPin.PinID), _Node.GetPinIdentifier(PinIndex));
						break;
					}
				}
			}
			Deselect();
		}
	}

	public void OnMouseMove(Vector2 mousePosition)
	{
		if (SelectedNode != null)
		{
			MoveNode(SelectedNode, mousePosition);
		}
	}

	public void Update()
	{
		if (Event.current != null)
		{
			if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
			{
				OnMouseMove(Event.current.mousePosition);
			}
			else if (Event.current.type == EventType.MouseDown)
			{
				OnMouseDown(Event.current.button, Event.current.mousePosition);
			}
			else if (Event.current.type == EventType.MouseUp)
			{
				OnMouseUp(Event.current.button, Event.current.mousePosition);
			}
		}
	}

	public void SelectNode(int nodeID)
	{
		oldDragPosition = Event.current.mousePosition;
		SelectedNode = GraphToEdit.GetNodeFromID(nodeID);
		Repaint();
	}

	public void SelectPin(EditorPinIdentifier pinID)
	{
		SelectedPin = pinID;
		Repaint();
	}

	public void LinkPins(EditorPinIdentifier pinA, EditorPinIdentifier pinB)
	{
		GraphToEdit.LinkPins(pinA, pinB);
	}

	public void Deselect()
	{
		SelectedNode = null;
		SelectedPin.NodeID = -1;
		SelectedPin.PinID = -1;
		Repaint();
		EditorUtility.SetDirty(GraphToEdit);
		SaveGraph();
	}

	public void MoveNode(EditorNode node, Vector2 newPosition)
	{
		Vector2 delta = newPosition - oldDragPosition;
		Vector2 position = node.GetNodeRect().position;
		node.SetNodePosition(position + delta);
		oldDragPosition = newPosition;
		Repaint();
	}
}
