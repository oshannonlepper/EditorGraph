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
		Vector3 FromTangent = new Vector3(0.5f * (From.x + To.x), From.y);
		Vector3 ToTangent = new Vector3(0.5f * (From.x + To.x), To.y);
		Handles.DrawBezier(From, To, FromTangent, ToTangent, InColor, null, 5.0f);
		Handles.EndGUI();
		Repaint();
	}

	private void OnGUI()
	{
		DrawGrid(20, 0.2f, Color.gray);
		DrawGrid(100, 0.4f, Color.gray);

		ProcessEvents(Event.current);

		if (GraphToEdit == null)
		{
			Reset();
			return;
		}

		if (GraphToEdit != null)
		{
			if (RegisteredFunctionDictionary == null)
			{
				Reset();
			}
		}
		
		RenderGraph();
		// draw editors for selected node

		if (GraphToEdit != null && SelectedPin.PinID != -1)
		{
			EditorNode OwnerNode = GraphToEdit.GetNodeFromID(SelectedPin.NodeID);
			Vector2 PinPos = OwnerNode.GetPinRect(SelectedPin.PinID).center;
			Line(PinPos, Event.current.mousePosition, Color.magenta);
		}

		if (GUI.changed)
		{
			Repaint();
		}

	}

	private void OnClick_NewGraph()
	{
		EditorGraph NewGraph = ScriptableObject.CreateInstance<EditorGraph>();
		string AssetDirectory = "Assets/";
		if (AssetDatabase.IsValidFolder("Assets/Graphs"))
		{
			AssetDirectory = "Assets/Graphs/";
		}
		string AssetName = "NewGraph";
		int ID = 1;
		while (AssetDatabase.FindAssets(AssetName).Length > 0)
		{
			AssetName = "NewGraph_" + ID;
			++ID;
		}
		AssetDatabase.CreateAsset(NewGraph, AssetDirectory + AssetName + ".asset");
		AssetDatabase.SaveAssets();
		OnGraphLoaded(NewGraph);
	}

	private void OnClick_LoadGraph()
	{
		controlId = GUIUtility.GetControlID(FocusType.Passive);
		EditorGUIUtility.ShowObjectPicker<EditorGraph>(null, false, "", controlId);
	}

	private void AddFunctionListToContextMenu(GenericMenu menu, Vector2 mousePos)
	{
		if (GraphToEdit == null)
		{
			menu.AddDisabledItem(new GUIContent("No graph available"));
			return;
		}

		foreach (System.Type LibraryType in RegisteredFunctionDictionary.Keys)
		{
			string libraryName = LibraryType.ToString();
			foreach (string MethodName in RegisteredFunctionDictionary[LibraryType])
			{
				menu.AddItem(new GUIContent(libraryName + "/" + MethodName), false, () => OnClick_AddNode(LibraryType, MethodName, mousePos));
			}
		}
	}

	private void OnClick_AddNode(System.Type LibraryType, string MethodName, Vector2 mousePos)
	{
		int NodeID = GraphToEdit.AddNode(EditorNode.CreateFromFunction(LibraryType, MethodName, false, false));
		EditorNode Node = GraphToEdit.GetNodeFromID(NodeID);
		Node.SetNodePosition(mousePos);
	}

	private void ProcessEvents(Event e)
	{
		switch (e.type)
		{
			case EventType.MouseDown:
			{
				OnMouseDown(e.button, e.mousePosition);
				break;
			}
			case EventType.MouseUp:
			{
				OnMouseUp(e.button, e.mousePosition);
				break;
			}
			case EventType.MouseMove:
			{
				OnMouseMove(e.mousePosition);
				break;
			}
			case EventType.MouseDrag:
			{
				OnMouseMove(e.mousePosition);
				break;
			}
			case EventType.KeyUp:
			{
				if (e.keyCode == KeyCode.Delete)
				{
					if (SelectedNode != null && SelectedPin.PinID == -1)
					{
						int NodeID = SelectedNode.ID;
						GraphToEdit.RemoveNode(SelectedNode);
						Deselect();
						foreach (EditorLink Link in LinkList)
						{
							
						}
					}
				}
				break;
			}
		}

		if (e.commandName.Equals("ObjectSelectorClosed"))
		{
			OnGraphLoaded(EditorGUIUtility.GetObjectPickerObject() as EditorGraph);
			controlId = -1;
		}
	}

	private void ProcessContextMenu(Vector2 mousePosition)
	{
		GenericMenu genericMenu = new GenericMenu();
		genericMenu.AddItem(new GUIContent("New Graph"), false, () => OnClick_NewGraph());
		genericMenu.AddItem(new GUIContent("Load Graph"), false, () => OnClick_LoadGraph());
		genericMenu.AddSeparator("");
		AddFunctionListToContextMenu(genericMenu, mousePosition);
		genericMenu.ShowAsContext();
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
				OldGraph.OnGraphChanged -= OnGraphChanged;
			}

			UpdateGraphCache();

			GraphToEdit.OnGraphChanged += OnGraphChanged;
		}
	}

	public void OnGraphChanged()
	{
		UpdateGraphCache();

		SaveGraph();
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
		if (GraphToEdit != null)
		{
			NodeList = GraphToEdit.GetNodeList();
			LinkList = GraphToEdit.GetLinkList();
			Repaint();
		}
	}

	private void RenderNode(EditorNode _Node)
	{
		float selectionBorder = 5.0f;
		Rect NodeRect = _Node.GetNodeRect();
		if (_Node == SelectedNode)
		{
			DrawRect(NodeRect.min - Vector2.one * selectionBorder, NodeRect.max + Vector2.one * selectionBorder, Color.yellow);
		}
		GUI.Box(NodeRect, " ");
	}

	private void RenderNodePins(EditorNode _Node)
	{
		int NumPins = _Node.PinCount;
		for (int PinIndex = 0; PinIndex < NumPins; ++PinIndex)
		{
			Rect PinRect = _Node.GetPinRect(PinIndex);
			GUI.Button(PinRect, " ");
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
						return;
					}
				}

				Rect NodeRect = _Node.GetNodeRect();
				if (HitTestPointToRect(mousePos, NodeRect))
				{
					Deselect();
					SelectNode(_Node.ID);
					return;
				}
			}

			Deselect();
		}
		else if (button == 1)
		{
			ProcessContextMenu(mousePos);
		}
	}

	public void OnMouseUp(int button, Vector2 mousePos)
	{
		if (button == 0)
		{
			foreach (EditorNode _Node in NodeList)
			{
				Rect NodeRect = _Node.GetNodeRect();
				if (HitTestPointToRect(mousePos, NodeRect))
				{
					int NumPins = _Node.PinCount;
					for (int PinIndex = 0; PinIndex < NumPins; ++PinIndex)
					{
						Rect PinRect = _Node.GetPinRect(PinIndex);
						if (HitTestPointToRect(mousePos, PinRect))
						{
							EditorNode OtherNode = GraphToEdit.GetNodeFromID(SelectedPin.NodeID);
							LinkPins(OtherNode.GetPinIdentifier(SelectedPin.PinID), _Node.GetPinIdentifier(PinIndex));
							UpdateGraphCache();
							break;
						}
					}
				}
			}
			if (SelectedPin.PinID != -1)
			{
				Deselect();
			}
		}
	}

	public void OnMouseMove(Vector2 mousePosition)
	{
		if (SelectedNode != null && SelectedPin.PinID == -1)
		{
			MoveNode(SelectedNode, mousePosition);
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

	public bool LinkPins(EditorPinIdentifier pinA, EditorPinIdentifier pinB)
	{
		EditorPin pinAData = GraphToEdit.GetPinFromID(pinA);
		EditorPin pinBData = GraphToEdit.GetPinFromID(pinB);

		if (!pinBData.CanLinkTo(pinAData))
		{
			Debug.LogWarning("Failed to link pins");
			return false;
		}

		GraphToEdit.LinkPins(pinA, pinB);
		return true;
	}

	public void Deselect()
	{
		SelectedNode = null;
		SelectedPin.NodeID = -1;
		SelectedPin.PinID = -1;
	}

	public void MoveNode(EditorNode node, Vector2 newPosition)
	{
		Vector2 delta = newPosition - oldDragPosition;
		Vector2 position = node.GetNodeRect().position;
		node.SetNodePosition(position + delta);
		oldDragPosition = newPosition;
		Repaint();
	}

	private Vector2 drag = new Vector2();

	private void DrawGrid(float cellSize, float opacity, Color colour)
	{
		int widthDivs = Mathf.CeilToInt(position.width / cellSize);
		int heightDivs = Mathf.CeilToInt(position.height / cellSize);

		Handles.BeginGUI();
		Handles.color = new Color(colour.r, colour.g, colour.b, opacity);

		Vector3 newOffset = new Vector3();
		if (GraphToEdit != null)
		{
			GraphToEdit.EditorViewportOffset += drag * 0.5f;
			newOffset = new Vector3(GraphToEdit.EditorViewportOffset.x % cellSize, GraphToEdit.EditorViewportOffset.y % cellSize, 0);
		}

		for (int i = 0; i < widthDivs; ++i)
		{
			Handles.DrawLine(new Vector3(cellSize * i, -cellSize, 0) + newOffset, new Vector3(cellSize * i, position.height, 0) + newOffset);
		}

		for (int j = 0; j < heightDivs; ++j)
		{
			Handles.DrawLine(new Vector3(-cellSize, cellSize * j, 0) + newOffset, new Vector3(position.width, cellSize * j, 0) + newOffset);
		}

		Handles.color = Color.white;
		Handles.EndGUI();
	}
}
