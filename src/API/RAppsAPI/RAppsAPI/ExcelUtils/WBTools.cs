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
using OfficeOpenXml.Table;

namespace RAppsAPI.ExcelUtils
{
    public class WBTools
    {
        public WBTools() { }

        public (int, string) BuildWorkbookFromDB(
            MPMEditRequestDTO req,
            int userId,
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
                    // Create each cell in wb. MUST apply all properties as they will be
                    // to overwrite the DB after clearing existing data.
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
                    // Must apply all the propertiesas they will be
                    // to overwrite the tables in DB after clearing existing data.
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
                        dstTable.ShowTotal = mTable.TotalRow;
                        dstTable.ShowRowStripes = mTable.BandedRows;
                        dstTable.ShowColumnStripes = mTable.BandedColumns;
                        dstTable.ShowFilter = mTable.FilterButton;
                        //dstRange.Calculate();  // TODO: chk if needed
                        //dstRange.AutoFitColumns(); // Table is never seen, so not needed
                    }

                } // foreach sheet
                // We will do this to ensure all formulas and values are up to date and ready
                ep.Workbook.Calculate();

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

        public bool AreCellStylesDifferent(ExcelRange cell1, ExcelRange cell2, string sheetName, int r, int c)
        {
            // Background color
            var val1 = cell1.Style.Fill.BackgroundColor.Rgb ?? "";
            var val2 = cell2.Style.Fill.BackgroundColor.Rgb ?? "";
            if (val1 != val2) 
            {
                Console.WriteLine($"CompareWorkbooks: Cell background color diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true; 
            }
            // Font Color
            val1 = cell1.Style.Font.Color.Rgb;
            val2 = cell2.Style.Font.Color.Rgb;
            if (val1 != val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell font color diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Font name
            val1 = cell1.Style.Font.Name;
            val2 = cell2.Style.Font.Name;
            if (val1 != val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell font name diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Font bold
            val1 = cell1.Style.Font.Bold ? "1" : "";
            val2 = cell2.Style.Font.Bold ? "1" : "";
            if (val1 != val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell font bold diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Font italic
            val1 = cell1.Style.Font.Italic ? "1" : "";
            val2 = cell2.Style.Font.Italic ? "1" : "";
            if (val1 != val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell font italic diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Font underline
            val1 = cell1.Style.Font.UnderLine ? "1" : "";
            val2 = cell2.Style.Font.UnderLine ? "1" : "";
            if (val1 != val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell font underline diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Font strike
            val1 = cell1.Style.Font.Strike ? "1" : "";
            val2 = cell2.Style.Font.Strike ? "1" : "";
            if (val1 != val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell font strike diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            return false;
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


        // Compare 2 workbooks & produce list of sheet names and table names that changed.
        // Compares all cell properties to find diff.
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

                        var cell1 = Cells1[r, c];
                        var cell2 = Cells2[r, c];                        ;
                        if (AreCellsDifferent(cell1, cell2, sheet1.Name, r, c))
                        {
                            sheetIsDifferent = true;                            
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
                // TODO: Only compares table range currently. Other properties should be 
                // compared too.
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
                            // Table found, compare the table range(addr) to check if it changed
                            // TODO: Check if this works
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


        // TODO: Currently only diffs value. Should diff all cell properties.
        private bool AreCellsDifferent(ExcelRange cell1, ExcelRange cell2, string sheetName, int r, int c)
        {
            // Value
            var val1 = cell1.Value?.ToString() ?? "";
            var val2 = cell2.Value?.ToString() ?? "";
            if (val1 == val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell value diff in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Formula
            val1 = cell1.Formula;
            val2 = cell2.Formula;
            if (val1 == val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell formula mismatch in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Format
            val1 = cell1.Style.Numberformat.Format;
            val2 = cell2.Style.Numberformat.Format;
            if (val1 == val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell number format mismatch in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Comment
            val1 = cell1.Comment?.ToString() ?? "";
            val2 = cell2.Comment?.ToString() ?? "";
            if (val1 == val2)
            {
                Console.WriteLine($"CompareWorkbooks: Cell comment mismatch in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            // Cell style
            if (AreCellStylesDifferent(cell1, cell2, sheetName, r,c))
            {
                Console.WriteLine($"CompareWorkbooks: Cell style mismatch in sheet:{sheetName}, row:{r}, col:{c}, val1:{val1}, val2:{val2}");
                return true;
            }
            return false;
        }




        //-------------------------------------------------------------------------------------------------

        internal class MRangeWithRow
        {
            public MRange DBRange { get; set; }
            // Range header table's row position, used to decide this range's RangeNum
            // & assign multiple series to it.
            public int Row { get; set; }
            // Got from the Range header
            // After a series is confirmed by row distance, check here & remove the series name
            // TODO: If any are left after all series are done, its an error
            public HashSet<string> setSeriesNames { get; set; } = new();
        }

        // Used for assigning RangeID to a MSeries. List<MSeriesWithRow> will be maintained.
        // Also to add MSeries to db later.
        internal class MSeriesWithRow
        {
            public MSeries DBSeries { get; set; }
            // Series header's row position - used for row distance calc to find the MRange for above
            // MSeries entry. The Series Header MTable is already updated with the SeriesId, so not 
            // needed here.
            public int Row { get; set; }
        }

        // Used for locating the range and then assigning the correct SeriesId within that range
        // to Series Detail MTable(looks up dictSeriesNameVsID in class MRangeWithRow).
        // List<MTableWithRow> will be maintained.
        internal class MTableWithRow
        {
            public MTable DBTable;
            // After a series is confirmed by row distance, check here & remove the series name
            public int Row;
        }

        public enum TableTypes
        {
            UNKNOWN = -1,
            MASTER = 1,
            RANGE_HEADER = 2,
            RANGE_SERIES_HEADER = 3,
            RANGE_SERIES_DETAIL_MINMAX = 100
        };

        enum ParseRetCode
        {
            SUCCESS = 0,
            INVALID_TABLE_DIMS,
            INVALID_RANGE_HEADER_DIMS,
            INVALID_RANGE_HEADER_TABLE_NAME,
            INVALID_RANGE_SERIES_HEADER_DIMS,
            INVALID_PRODUCT,
            INVALID_PRODUCT_TYPE_OR_RANGE,
            INVALID_SERIES_HEADER_TABLE_NAME,
            INVALID_SERIES_HEADER_SERIES_NAME,
            INVALID_SERIES_HEADER_DIMS,
            INVALID_SERIES_DETAIL_TABLE_NAME,
            INVALID_SERIES_DETAIL_SERIES_NAME,
            INVALID_SERIES_DETAIL_DIMS,
            // Create file for download, return codes
            RANGE_HEADER_MTABLES_NOT_FOUND,
            MTABLE_RANGE_ID_MISMATCH,
            MTABLE_TYPE_MISMATCH,
            MTABLE_LESS_CELLS,
            MTABLE_CELL_INVALID_ROW,
            MTABLE_CELL_INVALID_COL,
            // File comparison
            COMPARE_SHEET_ABSENT,
            COMPARE_TABLE_ROW_COL_MISMATCH,
            COMPARE_TABLE_NOT_FOUND,
            COMPARE_CELL_VALUE_MISMATCH
        };

        const string RANGE_HEADER_TABLE = "RangeHeaderTable";
        const string RANGE_SERIES_TABLE = "RangeSeriesTable";
        const string RANGE_SERIES_DETAIL_TABLE = "RangeSeriesDetailTable";


        // Write ExcelPackage into DB.
        // Keeping things simple for now.
        // Existing data will be cleared for now to avoid difficult to debug bugs. 
        // Backups can be used to compare if needed.
        // 'internal' so accessible only within same assembly.
        internal void UpdateDBFromWorkbook(
            int fileId, ExcelPackage ep, HashSet<string> diffSheets, RDBContext dbContext)
        {
            ExcelWorksheets worksheets = ep.Workbook.Worksheets;
            try
            {
                // Across sheets
                int productID = 1;
                int productTypeID = 1;
                // For locating by path for ID updates
                Dictionary<string, Product> dictProducts = new();
                // Path format: Little Car > Hatchback
                Dictionary<string, ProductType> dictProductTypes = new();
                // Scoped to workbook, not sheet
                int rangeID = 1;
                int seriesID = 1;
                int tableID = 1;

                foreach (var sheet in worksheets)
                {
                    // For current sheet
                    List<MRangeWithRow> mRangesWithRow = new();
                    List<MSeriesWithRow> mSeriesWithHeaderTableRow = new();
                    List<MTableWithRow> seriesDetailMTablesWithRow = new();
                    Dictionary<int, MTable> dictSeriesIdVsSeriesHdrMTable = new();

                    // Entities
                    List<Cell> efCells = new();
                    List<MTable> efMTables = new();

                    Console.WriteLine(sheet.Name);
                    if (sheet.Dimension == null)
                    {
                        Console.WriteLine($"ERROR: {sheet.Name} has NULL dimension");
                        break;
                    }

                    /*if(sheet.Name.Trim() == "")
                    {
                        Console.WriteLine(sheet.Name);
                    }*/

                    // Create the DB sheet
                    int sheetStartRow = sheet.Dimension.Start.Row;
                    int sheetEndRow = sheet.Dimension.End.Row;
                    int sheetStartCol = sheet.Dimension.Start.Column;
                    int sheetEndCol = sheet.Dimension.End.Column;
                    if (sheetEndCol > 500)
                    {
                        Console.WriteLine("Really big COLS:" + sheetEndCol);
                        sheetEndCol = 500;
                    }
                    var efSheet = new Sheet
                    {
                        VfileId = fileId,
                        SheetId = sheet.Index + 1,  // dont want this 0 based
                        Name = sheet.Name,
                        SheetNum = (short)(sheet.Index + 1),
                        Style = "",
                        StartRowNum = sheetStartRow,
                        StartColNum = sheetStartCol,
                        EndRowNum = sheetEndRow,
                        EndColNum = sheetEndCol,
                        CreatedBy = 1,
                        CreatedDate = DateTime.Today,
                        Rstatus = (byte)RStatus.Active
                    };
                    dbContext.Sheets.Add(efSheet);
                    // Add the cells of the sheet  
                    addSheetCells(
                        efSheet.SheetId,
                        efCells,
                        fileId,
                        sheetStartRow,
                        sheetEndRow,
                        sheetStartCol,
                        sheetEndCol,
                        sheet.Cells);

                    // Tables
                    var tables = sheet.Tables;
                    foreach (var table in tables)
                    {
                        Console.WriteLine($"Table: {table.Name} range:{table.Range}");
                        var addr = table.Address;
                        var numRows = addr.End.Row - addr.Start.Row + 1;
                        var numCols = addr.End.Column - addr.Start.Column + 1;
                        var efMTable = new MTable
                        {
                            VfileId = fileId,
                            TableId = tableID++,
                            Name = table.Name,  // TODO: Tablename is not unique, we need to check for this by putting the names in a set
                            NumRows = numRows,   // Will probably not be needed anymore
                            NumCols = numCols,
                            StartRowNum = addr.Start.Row,
                            StartColNum = addr.Start.Column,
                            EndRowNum = addr.End.Row,
                            EndColNum = addr.End.Column,
                            SheetId = efSheet.SheetId,   // RangeId, SeriesId updated later if not master table
                            TableType = -1,     // unknown at the moment, updated later when table parsed
                            Style = table.TableStyle.ToString(),
                            HeaderRow = table.ShowHeader,
                            TotalRow = table.ShowTotal,
                            BandedRows = table.ShowRowStripes,
                            BandedColumns = table.ShowColumnStripes,
                            FilterButton = table.ShowFilter,
                            CreatedBy = 1,
                            CreatedDate = DateTime.Today,
                            Rstatus = (byte)RStatus.Active
                        };
                        // Process the table data
                        processTable(
                            fileId,
                            table,
                            efMTable,
                            efSheet,
                            ref productID,   // passed by ref as this is incremented by this function, so next call can use a different ID
                            ref productTypeID,
                            dictProducts,
                            dictProductTypes,
                            ref rangeID,
                            ref seriesID,
                            mRangesWithRow,
                            mSeriesWithHeaderTableRow,
                            seriesDetailMTablesWithRow,
                            dictSeriesIdVsSeriesHdrMTable,
                            efCells);

                        efMTables.Add(efMTable);
                    }

                    // Sheet info post processing
                    // If this is a master tables sheet, then no need to process
                    if (mRangesWithRow.Count > 0 && mSeriesWithHeaderTableRow.Count > 0)
                    {
                        postProcessSheet(
                           mRangesWithRow,
                           mSeriesWithHeaderTableRow,
                           dictSeriesIdVsSeriesHdrMTable,
                           seriesDetailMTablesWithRow);
                    }
                    // Save sheet specific data
                    addRangesSeriesAndTablesToContext(
                        dbContext,
                        efMTables,
                        mRangesWithRow,
                        mSeriesWithHeaderTableRow,
                        efCells
                    );

                    // Add sheet cells                    
                    dbContext.Cells.AddRange(efCells);
                    dbContext.SaveChanges();

                } // end for-sheets

                // Workbook-wide data
                saveProductData(
                    dbContext,
                    dictProducts,
                    dictProductTypes);

                // Execute update query to update ranges
                dbContext.Database.ExecuteSql($@"
                    UPDATE  mpm.MTables SET
                    RangeID = s.RangeID
                    FROM
	                    mpm.MTables t JOIN mpm.MSeries s ON t.SeriesId = s.SeriesId
                    WHERE
                        t.TableType = 3 AND t.RangeID IS NULL");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }
        }


        private (int code, string msg) postProcessSheet(
            List<MRangeWithRow> mRangesWithRow,
            List<MSeriesWithRow> mSeriesWithHeaderTableRow,
            Dictionary<int, MTable> dictSeriesIdVsSeriesHdrMTable,
            List<MTableWithRow> seriesDetailMTablesWithRow)  // cannot make into dict of "series name" vs "series detail MTable" as
                                                             // same series name can be there in multiple range.
                                                             // Series id of series detail table is not known at this point.
        {
            // Sort the ranges and assign RangeNum which is the order in which the range appears in its Sheet
            mRangesWithRow.Sort((r1, r2) =>
            {
                return r1.Row.CompareTo(r2.Row);
            });

            // Now assign the correct Range to the Series using the sorted ranges
            for (int rangeIdx = 0; rangeIdx < mRangesWithRow.Count; ++rangeIdx)
            {
                MRangeWithRow currRangeWithRow = mRangesWithRow[rangeIdx];
                currRangeWithRow.DBRange.RangeNum = (short)(rangeIdx + 1);  // assign RangeNum
                int seriesNum = 1;
                // Series name to Series mapping for this range - built while assigning series
                // Used later to assign SeriesId to series detail MTables & DetailTableId back to the Series as well.
                Dictionary<string, MSeries> dictSeriesNameVsMSeries = new();
                foreach (var seriesWithRow in mSeriesWithHeaderTableRow)
                {
                    if (seriesWithRow.DBSeries.RangeId == -1)
                    {
                        // Unassigned series
                        if (rangeIdx < (mRangesWithRow.Count - 1))
                        {
                            // There is a next range
                            var nextRange = mRangesWithRow[rangeIdx + 1];
                            if (seriesWithRow.Row < nextRange.Row)  // ranges are sorted, so the first iter when this is true. the range above nextRange is the one
                            {
                                seriesWithRow.DBSeries.RangeId = currRangeWithRow.DBRange.RangeId; // Assign RangeId to Series(Series header & detail tables updated in next loops)
                                seriesWithRow.DBSeries.SeriesNum = (short)seriesNum++;
                                dictSeriesNameVsMSeries[seriesWithRow.DBSeries.Name] = seriesWithRow.DBSeries;
                                // Assign RangeId to Series Header table
                                var seriesId = seriesWithRow.DBSeries.SeriesId;
                                if (dictSeriesIdVsSeriesHdrMTable.ContainsKey(seriesId))
                                {
                                    var seriesHdrMTable = dictSeriesIdVsSeriesHdrMTable[seriesWithRow.DBSeries.SeriesId];
                                    seriesHdrMTable.RangeId = seriesWithRow.DBSeries.RangeId;
                                }
                                else
                                {
                                    Console.WriteLine($"postProcessSheet: Series Header MTable not found for SeriesId:{seriesId} while assigning RangeId to Series Header MTable");
                                }
                            }
                        }
                        else
                        {
                            // There is still some unassigned series and this is the last range
                            seriesWithRow.DBSeries.RangeId = currRangeWithRow.DBRange.RangeId;   // Assign RangeId to Series
                            seriesWithRow.DBSeries.SeriesNum = (short)seriesNum++;
                            dictSeriesNameVsMSeries[seriesWithRow.DBSeries.Name] = seriesWithRow.DBSeries;
                            // Assign RangeId to Series Header table
                            var seriesId = seriesWithRow.DBSeries.SeriesId;
                            if (dictSeriesIdVsSeriesHdrMTable.ContainsKey(seriesId))
                            {
                                var seriesHdrMTable = dictSeriesIdVsSeriesHdrMTable[seriesWithRow.DBSeries.SeriesId];
                                seriesHdrMTable.RangeId = seriesWithRow.DBSeries.RangeId;
                            }
                            else
                            {
                                Console.WriteLine($"postProcessSheet: Series Header MTable not found for SeriesId:{seriesId} while assigning RangeId to Series Header MTable");
                            }
                        }
                    }
                } // end-foreach (var seriesWithRow

                // Now update the series names in the series details tables
                // but only for the ones found to be in *this* range
                foreach (var seriesDetailWithRow in seriesDetailMTablesWithRow)
                {
                    if (seriesDetailWithRow.DBTable.RangeId == -1)
                    {
                        // Unassigned series
                        if (rangeIdx < (mRangesWithRow.Count - 1))
                        {
                            // There is a next range
                            var nextRange = mRangesWithRow[rangeIdx + 1];
                            if (seriesDetailWithRow.Row < nextRange.Row)
                            {
                                seriesDetailWithRow.DBTable.RangeId = currRangeWithRow.DBRange.RangeId; // Assign RangeId to Series Detail table
                                assignSeriesIdToSeriesDetail(
                                    currRangeWithRow.setSeriesNames,
                                    rangeIdx,
                                    dictSeriesNameVsMSeries,
                                    seriesDetailWithRow);

                            }
                        }
                        else
                        {
                            // This is the last range
                            seriesDetailWithRow.DBTable.RangeId = currRangeWithRow.DBRange.RangeId;  // Assign RangeId to Series Detail table
                            assignSeriesIdToSeriesDetail(
                                    currRangeWithRow.setSeriesNames,
                                    rangeIdx,
                                    dictSeriesNameVsMSeries,
                                    seriesDetailWithRow);

                        }
                    }
                } // end-foreach (var seriesDetailWithRow 

                if (mRangesWithRow[rangeIdx].setSeriesNames.Count > 0)
                {
                    Console.WriteLine($"setSeriesNames is not empty after RangeId:{currRangeWithRow.DBRange.RangeId} completed");
                }


            } // end-for (int rangeIdx...
            return (0, "");
        }


        private (int code, string msg) assignSeriesIdToSeriesDetail(
            HashSet<string> setSeriesNames,
            int rangeIdx,
            Dictionary<string, MSeries> dictSeriesNameVsMSeries,
            MTableWithRow seriesDetailWithRow)
        {
            // Assign SeriesId
            var seriesName = seriesDetailWithRow.DBTable.Name;
            if (setSeriesNames.Contains(seriesName))
            {
                if (dictSeriesNameVsMSeries.ContainsKey(seriesName))
                {
                    seriesDetailWithRow.DBTable.SeriesId = dictSeriesNameVsMSeries[seriesName].SeriesId;
                    seriesDetailWithRow.DBTable.Name = $"{RANGE_SERIES_DETAIL_TABLE}_{seriesName}_seriesid_{seriesDetailWithRow.DBTable.SeriesId}";
                    dictSeriesNameVsMSeries[seriesName].DetailTableId = seriesDetailWithRow.DBTable.TableId;
                    // Clear the series from setSeriesNames for later checking whether this becomes empty(should be empty after range is done)
                    setSeriesNames.Remove(seriesName);
                }
                else
                {
                    Console.WriteLine("Range assigned for Series Detail table, but its name is not in dictSeriesNameVsID");
                    //return;
                }
            }
            else
            {
                Console.WriteLine("Range assigned for Series Detail table, but its name is not in the list got from the Range Header");
                //return;
            }
            return (0, "");
        }

        private void addRangesSeriesAndTablesToContext(
            RDBContext db,
            List<MTable> efMTables,
            List<MRangeWithRow> mRangesWithRow,
            List<MSeriesWithRow> mSeriesWithHeaderTableRow,
            List<Cell> efCells)
        {


            var listDBMRanges = mRangesWithRow.Select(r => r.DBRange).ToList();
            if (listDBMRanges.Count > 0)
            {
                db.Ranges.AddRange(listDBMRanges);
            }

            var listDBMSeries = mSeriesWithHeaderTableRow.Select(s => s.DBSeries).ToList();
            if (listDBMSeries.Count > 0)
            {
                db.Series.AddRange(listDBMSeries);
            }

            //db.SaveChanges();

            db.Tables.AddRange(efMTables);
            //db.Cells.AddRange(efCells);

            //db.SaveChanges();
        }


        private void saveProductData(
            RDBContext db,
            Dictionary<string, Product> dictProducts,
            Dictionary<string, ProductType> dictProductTypes
            )
        {

            var listDBProducts = dictProducts.Values.ToList();
            if (listDBProducts.Count > 0)
            {
                db.Products.AddRange(listDBProducts);
            }

            var listDBProductTypes = dictProductTypes.Values.ToList();
            if (listDBProductTypes.Count > 0)
            {
                db.ProductTypes.AddRange(listDBProductTypes);
            }

            db.SaveChanges();
        }




        const string PRODUCT = "Product";
        const string PRODUCT_TYPE = "Product Type";
        const string RANGE = "RANGE";
        // Minimum table dimensions for various types
        const int RANGE_HEADER_MIN_ROWS = 6;
        const int RANGE_HEADER_MIN_COLS = 2;
        const int RANGE_SERIES_HEADER_MIN_ROWS = 6;
        const int RANGE_SERIES_HEADER_MIN_COLS = 2;
        const int RANGE_SERIES_DETAIL_MIN_ROWS = 5;
        const int RANGE_SERIES_DETAIL_MIN_COLS = 3;


        private (int code, string msg) processTable(
            int fileId,
            ExcelTable table,
            MTable efMTable,
            Sheet efSheet,
            ref int productID,
            ref int productTypeID,
            Dictionary<string, Product> dictProducts,
            Dictionary<string, ProductType> dictProductTypes,
            ref int rangeID,
            ref int seriesID,
            List<MRangeWithRow> mRangesWithRow,
            List<MSeriesWithRow> mSeriesWithHeaderTableRow,
            List<MTableWithRow> seriesDetailMTablesWithRow,
            Dictionary<int, MTable> dictSeriesIdVsSeriesHdrMTable,
            List<Cell> efCells)
        {
            if (efMTable.NumRows < 1 || efMTable.NumCols < 2)
            {
                string msg = $"ERROR: Invalid TABLE, Cannot get Table Name, num rows:{efMTable.NumRows}, num cols:{efMTable.NumCols}";
                Console.WriteLine(msg);
                return (-(int)ParseRetCode.INVALID_TABLE_DIMS, msg);
            }
            var addr = table.Address;
            var startRow = addr.Start.Row;
            var endRow = addr.End.Row;
            var startCol = addr.Start.Column;
            var endCol = addr.End.Column;
            var cells = table.WorkSheet.Cells;
            string tableName = cells[startRow, startCol + 1].Value?.ToString() ?? "";
            tableName = tableName.Trim();
            Console.WriteLine($"Processing {tableName}...");
            // Process each table type
            if (tableName.StartsWith(RANGE_HEADER_TABLE))
            {
                // Process range header
                // Check dims
                if (efMTable.NumRows < RANGE_HEADER_MIN_ROWS || efMTable.NumCols < RANGE_HEADER_MIN_COLS)
                {
                    string msg = $"ERROR: Invalid RANGE HEADER, dimensions for {tableName}, rows:{efMTable.NumRows}, cols:{efMTable.NumCols}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_RANGE_HEADER_DIMS, msg);
                }
                // Check table name
                var tokens = tableName.Split(';');
                if (tokens.Length < 2)
                {
                    string msg = $"ERROR: Invalid RANGE HEADER Table Name length:{tableName}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_RANGE_HEADER_TABLE_NAME, msg);
                }
                tokens = tokens[1].Split(",");
                for (int r = startRow + 2; r <= endRow; r++)
                {
                    string productHeadValue = cells[r, startCol].Value?.ToString() ?? "";
                    if (productHeadValue.StartsWith(PRODUCT) && endRow > r + 1)
                    {
                        string productTypeHeadValue = cells[r + 1, startCol].Value?.ToString() ?? "";
                        string rangeHeadValue = cells[r + 2, startCol].Value?.ToString() ?? "";
                        if (productTypeHeadValue.StartsWith(PRODUCT_TYPE) && rangeHeadValue.StartsWith(RANGE))
                        {
                            // Update the table type in the MTable as its now known
                            efMTable.TableType = (int)TableTypes.RANGE_HEADER;
                            efMTable.RangeId = rangeID;
                            // Get the product details
                            var productName = cells[r, startCol + 1].Value?.ToString() ?? "";
                            productName = productName.Trim();
                            var productTypeName = cells[r + 1, startCol + 1].Value?.ToString() ?? "";
                            productTypeName = productTypeName.Trim();
                            var rangeName = cells[r + 2, startCol + 1].Value?.ToString() ?? "";
                            rangeName = rangeName.Trim();
                            efMTable.Name = $"{RANGE_HEADER_TABLE}_{rangeName}_rangeid_{rangeID}";
                            // Add the product
                            int chosenProductID = -1;
                            if (dictProducts.ContainsKey(productName))
                            {
                                var prod = dictProducts[productName];
                                chosenProductID = prod.ProductId;
                            }
                            else
                            {
                                chosenProductID = productID++;
                                dictProducts.Add(productName, new()
                                {
                                    VfileId = fileId,
                                    ProductId = chosenProductID,
                                    Name = productName,
                                    SheetId = efSheet.SheetId,
                                    CreatedBy = 1,
                                    CreatedDate = DateTime.Today,
                                    Rstatus = (byte)RStatus.Active
                                });
                            }
                            // Add the product type
                            int chosenProductTypeID = -1;
                            var productTypeKey = string.Join(productName, ">", productTypeName);
                            if (dictProductTypes.ContainsKey(productTypeKey))
                            {
                                var pt = dictProductTypes[productTypeKey];
                                chosenProductTypeID = pt.ProductTypeId;
                            }
                            else
                            {
                                chosenProductTypeID = productTypeID++;
                                dictProductTypes.Add(productTypeKey, new()
                                {
                                    VfileId = fileId,
                                    ProductTypeId = chosenProductTypeID,
                                    ProductId = chosenProductID,
                                    Name = productTypeName,
                                    CreatedBy = 1,
                                    CreatedDate = DateTime.Today,
                                    Rstatus = (byte)RStatus.Active
                                });
                            }
                            // Add the range just figured out above
                            MRangeWithRow mRWR = new()
                            {
                                Row = startRow,   // Add the range's row num to do series range assigns using distance closeness later & decide the RangeNum
                                DBRange = new()
                                {
                                    VfileId = fileId,
                                    RangeId = rangeID,
                                    Name = rangeName,
                                    SheetId = efSheet.SheetId,
                                    ProductId = chosenProductID,
                                    ProductTypeId = chosenProductTypeID,
                                    HeaderTableId = efMTable.TableId,   // Will be updated later when the table entry is created
                                    RangeNum = -1,      // Updated later
                                    CreatedBy = 1,
                                    CreatedDate = DateTime.Today,
                                    Rstatus = (byte)RStatus.Active
                                },
                            };
                            mRWR.setSeriesNames.UnionWith(tokens); // The series names got from the RANGE HEADER are put here
                            mRangesWithRow.Add(mRWR);
                            //To be done at end only
                            ++rangeID;
                        }
                        else
                        {
                            string msg = $"ERROR: Invalid Product Type/Range for table {tableName}, pt:{productTypeHeadValue}, r:{rangeHeadValue}";
                            Console.WriteLine(msg);
                            return (-(int)ParseRetCode.INVALID_PRODUCT_TYPE_OR_RANGE, msg);
                        }
                        break; // break out of RANGE_HEADER field parsing for-loop
                    }
                    else
                    {
                        string msg = $"ERROR: Invalid Product/Product Type/Range for {tableName}, p: {productHeadValue}";
                        Console.WriteLine(msg);
                        return (-(int)ParseRetCode.INVALID_PRODUCT, msg);
                    }
                } // RANGE_HEADER field parsing for-loop
            }
            else if (tableName.StartsWith(RANGE_SERIES_TABLE))
            {
                // Process range series header
                // Create MSeries & Series Header MTable Cells, update the Series Header MTable with the MSeries Id
                var tokens = tableName.Split(';');
                if (tokens.Length < 2)
                {
                    string msg = $"ERROR: Invalid series header Table Name: {tableName}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_SERIES_HEADER_TABLE_NAME, msg);
                }
                var seriesName = tokens[1];
                if (seriesName.Trim() == "")
                {
                    string msg = $"ERROR: Invalid SERIES HEADER, no series name:{tableName}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_SERIES_HEADER_SERIES_NAME, msg);
                }
                if (efMTable.NumRows < RANGE_SERIES_HEADER_MIN_ROWS || efMTable.NumCols < RANGE_SERIES_HEADER_MIN_COLS)
                {
                    string msg = $"ERROR: Invalid SERIES HEADER, dimensions for {tableName}, rows:{efMTable.NumRows}, cols:{efMTable.NumCols}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_SERIES_HEADER_DIMS, msg);
                }
                // Add MSeries with row from Series Header table
                mSeriesWithHeaderTableRow.Add(new()
                {
                    DBSeries = new()
                    {
                        VfileId = fileId,
                        SeriesId = seriesID,
                        Name = seriesName,
                        RangeId = -1,          // n/a atm, will be updated later                
                        SheetId = efSheet.SheetId,
                        HeaderTableId = efMTable.TableId,
                        DetailTableId = -1,   // n/a atm, will be updated later 
                        SeriesNum = -1, // updtd later
                        CreatedBy = 1,
                        CreatedDate = DateTime.Today,
                        Rstatus = (byte)RStatus.Active
                    },
                    Row = startRow
                });
                // Update the Series Header MTable 
                efMTable.SeriesId = seriesID;   // For Series Header table, the SeriesId is known, but RangeId unknown at this point
                efMTable.RangeId = -1;
                efMTable.TableType = (int)TableTypes.RANGE_SERIES_HEADER;
                efMTable.Name = $"{RANGE_SERIES_TABLE}_{seriesName}_seriesid_{seriesID}";
                // Add to dict
                dictSeriesIdVsSeriesHdrMTable[seriesID] = efMTable;
                //To be done at end only
                ++seriesID;
            }
            else if (tableName.StartsWith(RANGE_SERIES_DETAIL_TABLE))
            {
                // Process range series detail
                // Just add the MTable cells as MTable is already created
                // Checks
                var tokens = tableName.Split(';');
                if (tokens.Length < 2)
                {
                    string msg = $"ERROR: Invalid SERIES DETAIL Table Name: {tableName}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_SERIES_DETAIL_TABLE_NAME, msg);
                }
                var seriesName = tokens[1];
                if (seriesName.Trim() == "")
                {
                    string msg = $"ERROR: Invalid SERIES DETAIL table, no series name:{tableName}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_SERIES_DETAIL_SERIES_NAME, msg);
                }
                if (efMTable.NumRows < RANGE_SERIES_DETAIL_MIN_ROWS || efMTable.NumCols < RANGE_SERIES_DETAIL_MIN_COLS)
                {
                    string msg = $"ERROR: Invalid SERIES DETAIL table, dimensions for {tableName}, rows:{efMTable.NumRows}, cols:{efMTable.NumCols}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.INVALID_SERIES_DETAIL_DIMS, msg);
                }
                // Add to list
                seriesDetailMTablesWithRow.Add(new()
                {
                    DBTable = efMTable,   // SeriesId in it will be updtd later 
                    Row = startRow,
                });
                // TODO: Need to add more series detail table types
                efMTable.TableType = (int)TableTypes.RANGE_SERIES_DETAIL_MINMAX;
                efMTable.Name = seriesName;
                efMTable.RangeId = -1;    // For series detail table, the RangeId & SeriesId is unknown at this point
                efMTable.SeriesId = -1;
            }
            else
            {
                // master table
                // Just add the cells, MTable already created
                // We might need a specific order among them, so a MasterNum field could be useful
                // Or alphabetical order.
                //string msg = $"Adding MASTER table {tableName}...";
                //Console.WriteLine(msg);
                efMTable.TableType = (int)TableTypes.MASTER;
                efMTable.Name = tableName; // $"{MASTER_TABLE}_{efMTable.Name}";
                efMTable.SheetId = efSheet.SheetId;
            }

            return ((int)ParseRetCode.SUCCESS, "");
        }


        private void addSheetCells(
            int sheetId,
            List<Cell> efCells,
            int fileId,
            int startRow,
            int endRow,
            int startCol,
            int endCol,
            ExcelRange cells)
        {
            // Add the sheet cells
            int cellID = 1;       // NOTE: unique only within a sheet
            for (int tr = startRow; tr <= endRow; ++tr)
            {
                for (int tc = startCol; tc <= endCol; ++tc)
                {
                    var cell = cells[tr, tc];
                    string cellValue, cellFormula, cellComment, cellFormat;
                    MPMCellStyle cellStyle;
                    GetCellProperties(cell, out cellValue, out cellFormula, out cellComment, out cellFormat, out cellStyle);
                    // Condition for defining an 'empty' cell is below:
                    if (IsCellEmpty(cellValue, cellFormula, cellComment, cellFormat, cellStyle))   // TODO: The style may need more detailed checks
                    {
                        // Skip cell as its values are empty
                        // TODO: Style and formatting needs to be checked too
                        continue;
                    }
                    // TODO: Detect if all properties are the default values and shorten the json or make this empty string            
                    var styleJson = GetCellStyleAsJSONString(cellStyle);
                    // Add db cell
                    var efCell = new Cell
                    {
                        VfileId = fileId,
                        SheetId = sheetId,
                        CellId = cellID,
                        RowNum = tr,  // 1 based index
                        ColNum = tc,
                        Value = cellValue,
                        Formula = cellFormula,
                        Format = cellFormat,
                        Style = styleJson,
                        Comment = cellComment,
                        CreatedBy = 1,
                        CreatedDate = DateTime.Today,
                        Rstatus = (byte)RStatus.Active
                    };
                    efCells.Add(efCell);
                    ++cellID;
                }
            }
        }

        // Parses the 'Table Name' field of an Excel table to get the table type
        public TableTypes GetSheetTableType(ExcelTable epTable,
            int startRow, int startCol, int numRows, int numCols)
        {
            if (numRows < 1 || numCols < 2)
            {
                string msg = $"ERROR: Invalid TABLE, Cannot get Table Name, num rows:{numRows}, num cols:{numCols}";
                Console.WriteLine(msg);
                return TableTypes.UNKNOWN;
            }
            var cells = epTable.WorkSheet.Cells;
            string tableName = cells[startRow, startCol + 1].Value?.ToString() ?? "";
            tableName = tableName.Trim();
            // Process each table type
            if (tableName.StartsWith(RANGE_HEADER_TABLE))
            {
                return TableTypes.RANGE_HEADER;
            }
            else if (tableName.StartsWith(RANGE_SERIES_TABLE))
            {
                return TableTypes.RANGE_SERIES_HEADER;
            }
            else if (tableName.StartsWith(RANGE_SERIES_DETAIL_TABLE))
            {
                // TODO: Need to add more series detail table types
                return TableTypes.RANGE_SERIES_DETAIL_MINMAX;
            }
            else
            {
                return TableTypes.MASTER;
            }
        }


        public void SaveExcelPackage(ExcelPackage ep, string path)
        {
            var xlFile = GetCleanFileInfo(path);
            ep.SaveAs(xlFile);
        }


        public FileInfo GetCleanFileInfo(string file)
        {
            var fi = new FileInfo("" + Path.DirectorySeparatorChar + file);
            if (fi.Exists)
            {
                fi.Delete();  // ensures we create a new workbook
            }
            return fi;
        }




    }



}
