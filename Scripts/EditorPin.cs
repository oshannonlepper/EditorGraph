using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EPinLinkType
{
	None,
	Input,
	Output,
}

[System.Serializable]
public class EditorPin
{

	[SerializeField] private string Type;
	[SerializeField] private string Name;
	[SerializeField] private int OwnerID;
	[SerializeField] private EPinLinkType PinLinkType;

	public EditorPin()
	{
		Type = "null";
		Name = "";
		OwnerID = -1;
		PinLinkType = EPinLinkType.None;
	}

	public EditorPin(string _Type, string _Name, int _OwnerID, EPinLinkType _PinLinkType)
	{
		Type = _Type;
		Name = _Name;
		OwnerID = _OwnerID;
		PinLinkType = _PinLinkType;
	}

	public override string ToString()
	{
		return Type + " " + Name;
	}

	public string GetPinName()
	{
		return Name;
	}

	public int GetOwnerID()
	{
		return OwnerID;
	}

	public EPinLinkType GetPinLinkType()
	{
		return PinLinkType;
	}

	public System.Type GetPinType()
	{
		return System.Type.GetType(Type);
	}

}
