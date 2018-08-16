using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PinData
{
	public bool bIsInput;
	public int Number;
}

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
	[SerializeField] private int ID;
	[SerializeField] private int OwnerID;
	[SerializeField] private EPinLinkType PinLinkType;

	public EditorPin()
	{
		Type = "null";
		Name = "";
		ID = -1;
		OwnerID = -1;
		PinLinkType = EPinLinkType.None;
	}

	public EditorPin(string _Type, string _Name, int _ID, int _OwnerID, EPinLinkType _PinLinkType)
	{
		Type = _Type;
		Name = _Name;
		ID = _ID;
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

	public int GetID()
	{
		return ID;
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
