using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EditorGraph : ScriptableObject {

	public delegate void EditorGraphEvent();
	public event EditorGraphEvent OnGraphChanged;

	[SerializeField] private List<EditorNode> Nodes;
	[SerializeField] private List<EditorLink> Links;

	public void AddNode(EditorNode _Node)
	{
		if (Nodes == null)
		{
			Nodes = new List<EditorNode>();
		}
		Nodes.Add(_Node);
		_Node.OnNodeChanged += NotifyGraphChange;
		NotifyGraphChange();
	}

	public void RemoveNode(EditorNode _Node)
	{
		if (Nodes == null)
		{
			Nodes = new List<EditorNode>();
		}
		Nodes.Remove(_Node);
		_Node.OnNodeChanged -= NotifyGraphChange;
		NotifyGraphChange();
	}

	public void LinkPins(EditorPin LHS, EditorPin RHS)
	{
		if (Links == null)
		{
			Links = new List<EditorLink>();
		}
		Links.Add(new EditorLink(LHS, RHS));
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
