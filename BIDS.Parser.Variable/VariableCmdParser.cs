using System;
using System.Collections.Generic;
using System.IO;

namespace BIDS.Parser.Variable;

public interface IVariableCmdResult
{
	int DataTypeId { get; }
}

public record VariableCmdKeyNotFound(int DataTypeId) : IVariableCmdResult;

public record ValiableCmdError();

public class VariableCmdParser
{
	public Dictionary<int, VariableStructure> DataTypeDict { get; } = new();

	public VariableCmdParser()
	{
	}

	public IVariableCmdResult From(ReadOnlySpan<byte> gotData)
	{
		if (gotData.Length < 4)
			throw new InvalidDataException("Not a Valiable Command (length < 4)");

		int dataId = Utils.GetInt32AndMove(ref gotData);

		VariableStructure? structure;
		switch (dataId)
		{
			case 0:
				structure = ParseDataTypeRegisterCommand(gotData);
				DataTypeDict.Add(structure.DataTypeId, structure);
				return structure;

			default:
				return DataTypeDict.TryGetValue(dataId, out structure)
					? structure.With(gotData)
					: new VariableCmdKeyNotFound(dataId);
		}
	}

	public static VariableStructure ParseDataTypeRegisterCommand(ReadOnlySpan<byte> bytes)
	{
		if (bytes.Length < 9)
			throw new ArgumentException("`bytes` length must be more than 9 (dataType{4} _ elemDataType{4} _ elemName{1..})");

		// 前から順々にデータ構造を記録
		List<VariableStructure.IDataRecord> records = new();

		// 初期段階では、カスタムデータを構造に含めることはサポートしない

		int cmdDataType = Utils.GetInt32AndMove(ref bytes);


		string structureName = Utils.GetStringAndMove(ref bytes);

		while (bytes.Length >= 5)
		{
			// 各フィールドのデータ型番号を取得する
			VariableDataType dataType = (VariableDataType)Utils.GetInt32AndMove(ref bytes);

			// 配列の場合は、配列の型に関する情報が次に記録されている。
			VariableDataType? arrayElemDataType = dataType == VariableDataType.Array
				? arrayElemDataType = (VariableDataType)Utils.GetInt32AndMove(ref bytes)
				: null;

			// 変数名(フィールド名/データ名)を取得する。
			// NULL文字に到達するか、あるいはSpanを全て読み切ったら終了
			string dataName = Utils.GetStringAndMove(ref bytes);

			if (arrayElemDataType is VariableDataType elemDataType)
				records.Add(new VariableStructure.ArrayDataRecord(elemDataType, dataName, Utils.GetSpecifiedTypeArray(elemDataType, 0)));
			else
				records.Add(new VariableStructure.DataRecord(dataType, dataName, Utils.GetDefaultValue(dataType)));
		}

		return new VariableStructure(cmdDataType, structureName, records);
	}
}
