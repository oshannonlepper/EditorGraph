using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorLink
{

	[SerializeField] private int FromNodeID;
	[SerializeField] private int FromPinID;
	[SerializeField] private int ToNodeID;
	[SerializeField] private int ToPinID;

	public EditorLink(int _FromNodeID, int _FromPinID, int _ToNodeID, int _ToPinID)
	{
		FromNodeID = _FromNodeID;
		FromPinID = _FromPinID;
		ToNodeID = _ToNodeID;
		ToPinID = _ToPinID;
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
