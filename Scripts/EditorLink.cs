using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorLink
{

	[SerializeField] private int FromNodeID;
	[SerializeField] private int FromPinID;
	[SerializeField] private int ToNodeID;
	[SerializeField] private int ToPinID;

	public EditorLink(EditorPinIdentifier LHSPin, EditorPinIdentifier RHSPin)
	{
		FromNodeID = LHSPin.NodeID;
		FromPinID = LHSPin.PinID;
		ToNodeID = RHSPin.NodeID;
		ToPinID = RHSPin.PinID;
	}

	public int NodeID_From
	{
		get
		{
			return FromNodeID;
		}
	}

	public int NodeID_To
	{
		get
		{
			return ToNodeID;
		}
	}

	public int PinID_From
	{
		get
		{
			return ToPinID;
		}
	}

	public int PinID_To
	{
		get
		{
			return ToPinID;
		}
	}


}
