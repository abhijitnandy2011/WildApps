// Workbook processing related utils

using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using OfficeOpenXml;
using RAppsAPI.Data;
using RAppsAPI.Models.MPM;
using static RAppsAPI.Data.DBConstants;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Text.Json;
using RAppsAPI.Models;
using OfficeOpenXml.Style;
using System.Drawing;

namespace RAppsAPI.ExcelUtils
{
    public class WBTools
    {
        public WBTools() { }

        public (int, string) BuildWorkbookFromDB(
            MPMEditRequestDTO req, 
            RDBContext dbContext,
            out ExcelPackage ep)
        {
            var listSkip = new List<string>()
            {
                ""
            };
            //ep = new ExcelPackage();
            const string outputPath = "E:\\Code\\RApps\\output\\test.xlsx";
            ep = new ExcelPackage(outputPath);
            try
            {                
                var fileId = req.FileId;
                var sheets = dbContext.Sheets.Where(s => s.VfileId == fileId).OrderBy(s => s.SheetNum).ToList();
                foreach (var sheet in sheets)
                {
                    if (listSkip.Contains(sheet.Name))
                    {
                        Console.WriteLine($"Skipping {sheet.Name}");
                        continue;
                    }
                    Console.WriteLine($"Creating sheet {sheet.Name}...");
                    // Create sheet in output file
                    var outSheet = ep.Workbook.Worksheets.Add(sheet.Name);
                    var destCells = outSheet.Cells;
                    // Create the cells of the sheet
                    var vFileIdParam = new SqlParameter("vFileIdParam", fileId);
                    var activeStatusParam = new SqlParameter("activeStatusParam", RStatus.Active);
                    var sheetId = sheet.SheetId;
                    var sheetIdParam = new SqlParameter("sheetIdParam", sheetId);
                    var listCellsResult = dbContext.Database.SqlQuery<Cell>(
                            @$"SELECT * FROM mpm.Cells WHERE 
                            SheetId={sheetIdParam} AND VFileID={vFileIdParam} AND RStatus={activeStatusParam}
                            ORDER BY RowNum, ColNum").ToList();
                    foreach (var dbCell in listCellsResult)
                    {
                        var r = dbCell.RowNum;
                        var c = dbCell.ColNum;
                        var cellDest = destCells[r, c];
                        cellDest.Formula = dbCell.Formula;
                        cellDest.Value = dbCell.Value;
                        cellDest.Style.Numberformat.Format = dbCell.Format;
                        cellDest.AddComment(dbCell.Comment);
                        SetCellStyle(cellDest, dbCell.Style);
                        
                    }
                    // Create all the tables
                    var listMTables = dbContext.Tables.Where(t => t.VfileId == fileId && t.SheetId == sheet.SheetId).ToList();
                    foreach (var mTable in listMTables)
                    {

                        var startRow = mTable.StartRowNum;
                        var startCol = mTable.StartColNum;
                        if (mTable.HeaderRow)
                            startRow++;
                        var endRow = mTable.EndRowNum;
                        var endCol = mTable.EndColNum;
                        var dstRange = outSheet.Cells[startRow, startCol, endRow, endCol];
                        Console.WriteLine($"mTable.Name:{mTable.Name}, sr:{startRow}, sc:{startCol}, er:{endRow}, ec:{endCol}");
                        var dstTable = outSheet.Tables.Add(dstRange, mTable.Name);
                        dstTable.ShowHeader = mTable.HeaderRow;
                        //dstRange.Calculate();  // TODO: chk if needed
                        dstRange.AutoFitColumns();
                    }

                } // foreach sheet
                
                // Test code: save the file, TODO: remove this later
                ep.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }

            //Console.WriteLine("BuildWorkbookFromDB: {0}", sheet1.Cells[1, 2].Value);
            return (0, "");
        }


        //--------------------- Cell Style -----------------------
        private bool SetCellStyle(ExcelRange cell, string style)
        {
            var cellStyle = JsonSerializer.Deserialize<MPMCellStyle>(style);
            if (cellStyle == null) 
            {
                return false;
            }
            var color = System.Drawing.ColorTranslator.FromHtml("#" + cellStyle.bg);
            cell.Style.Fill.BackgroundColor.SetColor(color);
            color = System.Drawing.ColorTranslator.FromHtml("#" + cellStyle.font.c);
            cell.Style.Font.Color.SetColor(color);
            cell.Style.Font.Name = cellStyle.font.n;
            cell.Style.Font.Bold = (cellStyle.font.b == "1");
            cell.Style.Font.Italic = (cellStyle.font.i == "1");
            cell.Style.Font.UnderLine = (cellStyle.font.u == "1");
            cell.Style.Font.Strike = (cellStyle.font.s == "1");
            return true;
        }


        // Condition for defining an 'empty' cell in a EPPlus sheet is below:
        // TODO: Style and formatting needs to be checked too
        public bool IsCellEmpty(
            string cellValue,
            string cellFormula,
            string cellComment,
            string cellFormat,
            MPMCellStyle cellStyle)
        {
            return cellValue == "" &&
                cellFormula == "" &&
                cellComment == "" &&
                cellFormat == "" &&
                cellStyle.bg == "";
        }

        public void GetCellProperties(
            ExcelRange cell,
            out string cellValue,
            out string cellFormula,
            out string cellComment,
            out string cellFormat,
            out MPMCellStyle cellStyle)
        {
            cellValue = cell.Value?.ToString() ?? "";
            cellFormula = cell.Formula;
            cellComment = cell.Comment?.ToString() ?? "";
            cellFormat = cell.Style.Numberformat.Format;
            // Compose the style
            // TODO: Lot more details of the style has to be captured
            cellStyle = GetBriefCellStyle(cell);
            // Store defaults as empty strings, default values are known
            // Format default
            if (cellFormat == "General")
            {
                cellFormat = "";
            }
        }

        public MPMCellStyle GetBriefCellStyle(ExcelRange cell)
        {
            MPMCellStyle cellStyle = new()
            {
                bg = cell.Style.Fill.BackgroundColor.Rgb ?? "",
                font = new()
                {
                    c = cell.Style.Font.Color.Rgb,
                    n = cell.Style.Font.Name,
                    b = cell.Style.Font.Bold ? "1" : "",
                    i = cell.Style.Font.Italic ? "1" : "",
                    u = cell.Style.Font.UnderLine ? "1" : "",
                    s = cell.Style.Font.Strike ? "1" : "",
                }
            };
            if (cellStyle.font.n == "Calibri")
            {
                cellStyle.font.n = "";
            }
            return cellStyle;
        }

        // Apply rules to shorten style json here
        // {"bg":"","font":{"c":"","n":"","b":"","i":"","u":"","s":""}}
        // TODO: Detect if all properties are the default values and shorten the json or make this empty string            
        public string GetCellStyleAsJSONString(MPMCellStyle style)
        {
            var font = style.font;
            if (style.bg == "" &&
                font.c == "" &&
                font.n == "" &&
                font.i == "" &&
                font.u == "" &&
                font.s == "")
            {
                if (font.b == "")
                    return "";
                else if (font.b == "1")
                    return "{\"font\":{\"b\":\"1\"}";

            }
            return JsonSerializer.Serialize(style);
        }


        // Copy all sheets to new ExcelPackage
        // TODO: Check if tables got copied in all the sheets
        public (int, string) CloneExcelPackage(ExcelPackage ep, out ExcelPackage epCopy)
        {
            epCopy = new ExcelPackage();
            try
            {                
                var sheets = ep.Workbook.Worksheets;
                foreach (var sheet in sheets)
                {
                    epCopy.Workbook.Worksheets.Add(sheet.Name, sheet);
                    // TODO: confirm if tables are present in copied sheet
                    var tables = epCopy.Workbook.Worksheets[sheet.Name].Tables;
                    foreach (var table in tables)
                    {
                        Console.WriteLine($"CloneExcelPackage: Table: {table.Name} range:{table.Range} found in epCopy");                        
                    }
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
            return (0, "");
        }


        // Compare 2 workbooks & prduce list of sheet names and table names that changed
        public (int, string) CompareWorkbooks(
            ExcelPackage ep1,
            ExcelPackage ep2,
            out HashSet<string> diffSheets)
        {
            diffSheets = new();
            // Grab the sheets
            var wbSheets1 = ep1.Workbook.Worksheets;
            var wbSheets2 = ep2.Workbook.Worksheets;
            // Process each sheet in ep1
            foreach (var sheet1 in wbSheets1)
            {
                var sheet2 = wbSheets2[sheet1.Name];
                if (sheet2 == null)
                {
                    diffSheets.Add(sheet1.Name);
                    Console.WriteLine($"CompareWorkbooks: {sheet1.Name} is absent from ep2");
                    continue;
                }
                int startRow1 = sheet1.Dimension.Start.Row;
                int startRow2 = sheet2.Dimension.Start.Row;
                int endRow1 = sheet1.Dimension.End.Row;
                int endRow2 = sheet2.Dimension.End.Row;
                int startCol1 = sheet1.Dimension.Start.Column;
                int startCol2 = sheet2.Dimension.Start.Column;
                int endCol1 = sheet1.Dimension.End.Column;
                int endCol2 = sheet2.Dimension.End.Column;
                if (startRow1 != startRow2 || endRow1 != endRow2 || startCol1 != startCol2 || endCol1 != endCol2)
                {
                    diffSheets.Add(sheet1.Name);
                    Console.WriteLine($"CompareWorkbooks: {sheet1.Name} has difference in dims:({startRow1},{endRow1},{startCol1},{endCol1}) vs ({startRow2},{endRow2},{startCol2},{endCol2})");
                    continue;
                }               
                if (endCol1 > Constants.MAX_COLS_READ_IN_SHEET)
                {
                    Console.WriteLine($"CompareWorkbooks: ERROR Really big number of columns{endCol1}, rounding to {Constants.MAX_COLS_READ_IN_SHEET} columns");
                    endCol1 = endCol2 = Constants.MAX_COLS_READ_IN_SHEET;
                }
                // Cells
                var Cells1 = sheet1.Cells;
                var Cells2 = sheet2.Cells;
                bool sheetIsDifferent = false;
                for (int r = startRow1; r <= endRow1; ++r)
                {
                    for (int c = startCol1; c <= endCol1; ++c)
                    {
                        var val1 = Cells1[r, c].Value?.ToString() ?? "";
                        var val2 = Cells2[r, c].Value?.ToString() ?? "";
                        if (val1 != val2)
                        {
                            sheetIsDifferent = true;
                            Console.WriteLine($"CompareWorkbooks: Cell value mismatch in sheet:{sheet1.Name}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                            break;
                        }
                    }
                    if (sheetIsDifferent)
                    {                        
                        break;
                    }
                }
                if (sheetIsDifferent)
                {
                    diffSheets.Add(sheet1.Name);
                    continue;
                }
                // Now compare tables
                var tables1 = sheet1.Tables;
                var tables2 = sheet2.Tables;
                foreach (var table1 in tables1)
                {
                    // Get name directly from table object as names will have been given correctly by now
                    var tableName1 = table1.Name;
                    var addr1 = table1.Address;
                    var strAddr1 = addr1.ToString();
                    // Find table1 in tables2
                    var wasTableFound = false;
                    foreach (var table2 in tables2)
                    {
                        var tableName2 = table2.Name;
                        if (tableName2 == tableName1)
                        {
                            // Table found
                            wasTableFound = true;
                            var addr2 = table2.Address;                            
                            var strAddr2 = addr2.ToString();
                            if (strAddr1 != strAddr2)
                            {
                                Console.WriteLine($"CompareWorkbooks: Table address mismatch in sheet:{sheet1.Name}, Table:{tableName1}, Addr1:{strAddr1}, Addr2:{strAddr2}");
                                sheetIsDifferent = true;
                                break;
                            }

                        }
                    }
                    if (!wasTableFound || sheetIsDifferent)
                    {
                        // Safer, if a table mismatches for a sheet, the entire sheet to be considered diff.
                        // TODO: Maybe we could just consider the table diff & not the whole sheet?
                        Console.WriteLine($"CompareWorkbooks: Table mismatch in sheet:{sheet1.Name}, Table:{tableName1}, Addr:{strAddr1}");
                        break;
                    }
                }
                if (sheetIsDifferent)
                {
                    diffSheets.Add(sheet1.Name);
                    continue;
                }
            }  // foreach (var sheet1
            return (0, "");
        }

        
    }



}
