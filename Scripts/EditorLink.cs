using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorLink
{

	[SerializeField] public EditorPin FromPin { get; set; }
	[SerializeField] public EditorPin ToPin { get; set; }

	public EditorLink(EditorPin LHS, EditorPin RHS)
	{
		FromPin = LHS;
		ToPin = RHS;
	}

}
