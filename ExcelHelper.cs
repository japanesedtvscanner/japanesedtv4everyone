using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinelScanner4JPTV
{
	public static class ExcelHelper
	{
		public static DataSet Excel2DataSet(string fileName)
		{
			if (!File.Exists(fileName))
			{
				return null;
			}

			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
			{
				DataSet dataSet = new DataSet();
				IWorkbook workbook = new XSSFWorkbook(fs);
				for (int i = 0; i < workbook.NumberOfSheets; ++i)
				{
					ISheet sheet = workbook.GetSheetAt(i);
					DataTable dataTable = new DataTable(sheet.SheetName);

					// Create columns in DataTable based on Excel headers
					IRow headerRow = sheet.GetRow(0);
					foreach (ICell headerCell in headerRow)
					{
						dataTable.Columns.Add(headerCell.ToString());
					}

					// Populate DataTable with Excel data
					for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
					{
						IRow dataRow = sheet.GetRow(rowIndex);
						DataRow newRow = dataTable.NewRow();

						for (int cellIndex = 0; cellIndex < dataRow.LastCellNum; cellIndex++)
						{
							newRow[cellIndex] = dataRow.GetCell(cellIndex)?.ToString();
						}

						dataTable.Rows.Add(newRow);
					}
					dataSet.Tables.Add(dataTable);
				}
				return dataSet;
			}
		}

		public static void SaveDataSet2Excel(DataSet dataSet, string fileName)
		{
			if (!Directory.Exists(Path.GetDirectoryName(fileName)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(fileName));
			}

			IWorkbook workbook = new XSSFWorkbook();

			foreach (DataTable dataTable in dataSet.Tables)
			{
				string tableName;
				if (string.IsNullOrWhiteSpace(dataTable.TableName))
				{
					tableName = "Sheet" + (workbook.NumberOfSheets + 1).ToString();
				}
				else
				{
					tableName = dataTable.TableName;
				}
				ISheet sheet = workbook.CreateSheet(tableName);

				// Write column headers
				IRow headerRow = sheet.CreateRow(0);
				for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
				{
					ICell cell = headerRow.CreateCell(colIndex);
					cell.SetCellValue(dataTable.Columns[colIndex].ColumnName);
				}

				// Write data rows
				for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
				{
					IRow dataRow = sheet.CreateRow(rowIndex + 1);
					for (int colIndex = 0; colIndex < dataTable.Columns.Count; colIndex++)
					{
						ICell cell = dataRow.CreateCell(colIndex);
						cell.SetCellValue(dataTable.Rows[rowIndex][colIndex].ToString());
					}
				}
			}

			using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
			{
				workbook.Write(fs);
			}
		}

		public static void SaveDataTable2Excel(DataTable dataTable, string fileName)
		{
			DataSet dataSet = new DataSet();
			dataSet.Tables.Add(dataTable);
			SaveDataSet2Excel(dataSet, fileName);
		}
	}
}
