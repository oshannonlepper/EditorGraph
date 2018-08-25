﻿using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[System.Serializable]
public class EditorGraph : ScriptableObject {

	public delegate void EditorGraphEvent();
	public event EditorGraphEvent OnGraphChanged;

	[SerializeField] private List<EditorNode> Nodes;
	[SerializeField] private List<EditorLink> Links;
	[SerializeField] public Vector2 EditorViewportOffset = new Vector2();

	public int AddNode(EditorNode _Node)
	{
		if (Nodes == null)
		{
			Nodes = new List<EditorNode>();
		}
		Nodes.Add(_Node);
		_Node.OnNodeChanged += NotifyGraphChange;
		NotifyGraphChange();
		return _Node.ID;
	}

	public bool RemoveNode(EditorNode _Node)
	{
		if (Nodes == null)
		{
			return false;
		}

		int NodeID = _Node.ID;
		bool bSuccess = Nodes.Remove(_Node);

		if (bSuccess)
		{
			// remove all associated links
			for (int Index = Links.Count - 1; Index >= 0; --Index)
			{
				EditorLink Link = Links[Index];
				if (Link.NodeID_From == NodeID || Link.NodeID_To == NodeID)
				{
					Links.RemoveAt(Index);
				}
			}

			_Node.OnNodeChanged -= NotifyGraphChange;
			NotifyGraphChange();
			return true;
		}
		else
		{
			return false;
		}
	}

	public EditorNode GetNodeFromID(int ID)
	{
		foreach (EditorNode _Node in Nodes)
		{
			if (_Node.ID == ID)
			{
				return _Node;
			}
		}

		Debug.LogError("Trying to get Node with invalid ID " + ID + ".");
		return null;
	}

	public EditorPin GetPinFromID(EditorPinIdentifier PinIdentifier)
	{
		EditorNode _Node = GetNodeFromID(PinIdentifier.NodeID);
		if (_Node != null)
		{
			return _Node.GetPin(PinIdentifier.PinID);
		}
		return null;
	}

	public EditorNode CreateFromFunction(System.Type ClassType, string Methodname, bool bHasOutput = false, bool bHasInput = false)
	{
		return EditorNode.CreateFromFunction(ClassType, Methodname, bHasInput, bHasOutput);
	}

	public void LinkPins(EditorPinIdentifier LHSPin, EditorPinIdentifier RHSPin)
	{
		if (Links == null)
		{
			Links = new List<EditorLink>();
		}

		EditorLink NewLink = new EditorLink(LHSPin, RHSPin);
		Links.Add(NewLink);
		NotifyGraphChange();
	}

	public List<EditorNode> GetNodeList()
	{
		if (Nodes == null)
		{
			Nodes = new List<EditorNode>();
		}
		return Nodes;
	}

	public List<EditorLink> GetLinkList()
	{
		if (Links == null)
		{
			Links = new List<EditorLink>();
		}
		return Links;
	}

	public void NotifyGraphChange()
	{
		if (OnGraphChanged != null)
		{
			OnGraphChanged();
		}
	}

}
