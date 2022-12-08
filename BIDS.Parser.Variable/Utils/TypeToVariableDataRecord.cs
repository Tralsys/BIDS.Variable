using System;
using System.Reflection;

namespace BIDS.Parser.Variable;

public static partial class Utils
{
	public static VariableStructure.IDataRecord ToVariableDataRecord(this MemberInfo member)
		=> ToVariableDataRecord(member switch
		{
			FieldInfo v => v.FieldType,
			PropertyInfo v => v.PropertyType,

			_ => throw new ArgumentException("Only Field or Property is supported"),
		}, member.Name);

	public static VariableStructure.IDataRecord ToVariableDataRecord(Type memberType, string name)
	{
		if (memberType.IsArray)
		{
			if (memberType.GetElementType() is not Type elemType)
				throw new Exception("Cannot recognize Array element type");

			VariableDataType elemDataType = elemType.ToVariableDataType();
			if (elemDataType == VariableDataType.Array)
				throw new NotSupportedException("Currently, nested array is not supported.");

			return new VariableStructure.ArrayDataRecord(elemDataType, name, GetSpecifiedTypeArray(elemDataType, 0));
		}

		if (memberType == typeof(string))
		{
			return new VariableStructure.ArrayDataRecord(VariableDataType.UInt8, name, Array.Empty<byte>());
		}

		VariableDataType variableDataType = memberType.ToVariableDataType();
		return new VariableStructure.DataRecord(variableDataType, name, GetDefaultValue(variableDataType));
	}
}
