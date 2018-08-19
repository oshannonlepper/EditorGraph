using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGraphInputHandler
{

	void OnGraphLoaded(EditorGraph graph);

	void SelectNode(int nodeID);
	void MoveNode(EditorNode node, Vector2 newPosition);
	void SelectPin(EditorPinIdentifier pinID);
	void Deselect();
	void LinkPins(EditorPinIdentifier pinA, EditorPinIdentifier pinB);

}
