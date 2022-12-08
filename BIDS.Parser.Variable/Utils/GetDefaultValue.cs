namespace BIDS.Parser.Variable;

public static partial class Utils
{
	public static object? GetDefaultValue(VariableStructure.IDataRecord dataRecord)
		=> dataRecord is VariableStructure.IArrayDataRecord v
			? GetSpecifiedTypeArray(v.ElemType, 0)
			: GetDefaultValue(dataRecord.Type);

	public static object? GetDefaultValue(VariableDataType type)
		=> type switch
		{
			VariableDataType.Boolean => default(bool),

			VariableDataType.Int8 => default(sbyte),
			VariableDataType.Int16 => default(short),
			VariableDataType.Int32 => default(int),
			VariableDataType.Int64 => default(long),

			VariableDataType.UInt8 => default(byte),
			VariableDataType.UInt16 => default(ushort),
			VariableDataType.UInt32 => default(uint),
			VariableDataType.UInt64 => default(ulong),

#if NET5_0_OR_GREATER
			VariableDataType.Float16 => default(System.Half),
#endif

			VariableDataType.Float32 => default(float),
			VariableDataType.Float64 => default(double),

			_ => null,
		};
}
