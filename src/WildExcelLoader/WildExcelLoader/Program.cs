// TODO: Checks on table formats:
// There must be all possible checks done according to known facts.
// Must detect invalid tables
//


using EFCore_DBLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Drawing;
using WildExcelLoader.models;
using WildSheetLoader;



ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

//---------------------------------------
// DB setup

IConfigurationRoot _configuration;
DbContextOptionsBuilder<WildContext> _optionsBuilder;

const int FILE_ID = 3;

void BuildOptions()
{
    _configuration = ConfigurationBuilderSingleton.ConfigurationRoot;
    _optionsBuilder = new DbContextOptionsBuilder<WildContext>();
    _optionsBuilder.UseSqlServer(_configuration.GetConnectionString("WildDB"));
}


//---------------------------------------------------------------

void loadCarsFile(string filePath)
{
    // Open the workbook (or create it if it doesn't exist)
    using (var p = new ExcelPackage(filePath))
    {
        // Get the Worksheet created in the previous codesample. 
        //var ws = p.Workbook.Worksheets["Mahindra"];
        // Set the cell value using row and column.
        /*ws.Cells[2, 1].Value = "This is cell A2. Its font style is now set to bold";
        // The style object is used to access most cells formatting and styles.
        ws.Cells[2, 1].Style.Font.Bold = true;
        // Save and close the package.
        p.Save();
        */

        //Console.WriteLine(ws.Cells[3, 2].Value);

        using (var db = new WildContext(_optionsBuilder.Options))
        {
            ExcelWorksheets worksheets = p.Workbook.Worksheets;
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
                    var efSheet = new Sheet
                    {
                        VfileId = FILE_ID,
                        SheetId = sheet.Index+1,  // dont want this 0 based
                        Name = sheet.Name,
                        SheetNum = (short)(sheet.Index+1),
                        Style = "",                        
                        CreatedBy = 1,
                        CreatedDate = DateTime.Today,
                        Rstatus = (byte)RStatus.Active
                    };
                    db.Sheets.Add(efSheet);                    

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
                            VfileId = FILE_ID,
                            TableId = tableID++,
                            Name = table.Name,  // TODO: Tablename is not unique, we need to check for this by putting the names in a set
                            NumRows = numRows,
                            NumCols = numCols,
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
                            table,
                            efMTable,
                            efSheet,
                            ref productID,
                            ref productTypeID,
                            dictProducts,
                            dictProductTypes,
                            ref rangeID,
                            ref seriesID,
                            mRangesWithRow,
                            mSeriesWithHeaderTableRow,
                            seriesDetailMTablesWithRow,
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
                           seriesDetailMTablesWithRow);
                    }   
                    // Save sheet specific data
                    saveRangesSeriesAndTablesInDB(
                        db,                       
                        efMTables,
                        mRangesWithRow,
                        mSeriesWithHeaderTableRow,                        
                        efCells
                    );

                    // Add sheet cells & tables                    
                    // db.Cells.AddRange(efCells.ToArray());
                    //db.SaveChanges();

                } // end for-sheets

                // Workbook-wide data
                saveProductData(
                    db,
                    dictProducts,
                    dictProductTypes);

                // Execute update query to update ranges
                db.Database.ExecuteSql($@"
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
    }

}


const string RANGE_HEADER_TABLE = "RangeHeaderTable";
const string RANGE_SERIES_TABLE = "RangeSeriesTable";
const string RANGE_SERIES_DETAIL_TABLE = "RangeSeriesDetailTable";

(int code, string msg) postProcessSheet(
    List<MRangeWithRow> mRangesWithRow,
    List<MSeriesWithRow> mSeriesWithHeaderTableRow,
    List<MTableWithRow> seriesDetailMTablesWithRow)
{
    // Sort the ranges and assign RangeNum which is the order in which the range appears in its Sheet
    mRangesWithRow.Sort((r1, r2) =>
    {
        return r1.Row.CompareTo(r2.Row);
    });   

    // Now assign the series using the sorted ranges
    for (int rangeIdx = 0; rangeIdx < mRangesWithRow.Count; ++rangeIdx)
    {
        mRangesWithRow[rangeIdx].DBRange.RangeNum = (short)(rangeIdx + 1);
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
                    if (seriesWithRow.Row < nextRange.Row)
                    {
                        seriesWithRow.DBSeries.RangeId = mRangesWithRow[rangeIdx].DBRange.RangeId;
                        seriesWithRow.DBSeries.SeriesNum = (short)seriesNum++;
                        dictSeriesNameVsMSeries[seriesWithRow.DBSeries.Name] = seriesWithRow.DBSeries;
                    }
                }
                else
                {
                    // This is the last range
                    seriesWithRow.DBSeries.RangeId = mRangesWithRow[rangeIdx].DBRange.RangeId;
                    seriesWithRow.DBSeries.SeriesNum = (short)seriesNum++;
                    dictSeriesNameVsMSeries[seriesWithRow.DBSeries.Name] = seriesWithRow.DBSeries;
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
                        seriesDetailWithRow.DBTable.RangeId = mRangesWithRow[rangeIdx].DBRange.RangeId;
                        assignSeriesIdToSeriesDetail(
                            mRangesWithRow[rangeIdx].setSeriesNames,
                            rangeIdx,
                            dictSeriesNameVsMSeries,
                            seriesDetailWithRow);

                    }
                }
                else
                {
                    // This is the last range
                    seriesDetailWithRow.DBTable.RangeId = mRangesWithRow[rangeIdx].DBRange.RangeId;
                    assignSeriesIdToSeriesDetail(
                            mRangesWithRow[rangeIdx].setSeriesNames,
                            rangeIdx,
                            dictSeriesNameVsMSeries,
                            seriesDetailWithRow);

                }
            }
        } // end-foreach (var seriesDetailWithRow 

    } // end-for (int rangeIdx...
    return (0, "");
}


(int code, string msg) assignSeriesIdToSeriesDetail(
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

void saveRangesSeriesAndTablesInDB(
    WildContext db,
    List<MTable> efMTables, 
    List<MRangeWithRow> mRangesWithRow,
    List<MSeriesWithRow> mSeriesWithHeaderTableRow,
    List<Cell> efCells)
{
   

    var listDBMRanges = mRangesWithRow.Select(r => r.DBRange).ToList();
    if (listDBMRanges.Count > 0 )
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
    db.Cells.AddRange(efCells);

    db.SaveChanges();
}


void saveProductData(
    WildContext db,
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


(int code, string msg) processTable(
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
    List<Cell> efCells)
{
    var addr = table.Address;
    var startRow = addr.Start.Row;
    var endRow = addr.End.Row;
    var startCol = addr.Start.Column;
    var endCol = addr.End.Column;
    var cells = table.WorkSheet.Cells;
    string tableName = cells[startRow, startCol+1].Value?.ToString() ?? "";    
    tableName = tableName.Trim();
    Console.WriteLine($"Processing {tableName}...");
    // Process each table type
    if(tableName.StartsWith(RANGE_HEADER_TABLE))
    {
        // Process range header
        // Check dims
        if (efMTable.NumRows < RANGE_HEADER_MIN_ROWS || efMTable.NumCols < RANGE_HEADER_MIN_COLS)
        {
            string msg = $"Invalid RANGE HEADER, dimensions for {tableName}, rows:{efMTable.NumRows}, cols:{efMTable.NumCols}";
            Console.WriteLine(msg);
            return(-(int)ParseRetCode.INVALID_RANGE_HEADER_DIMS, msg);
        }
        // Check table name
        var tokens = tableName.Split(';');
        if (tokens.Length < 2) {
            string msg = $"Invalid RANGE HEADER Table Name length:{tableName}";
            Console.WriteLine(msg);
            return(-(int)ParseRetCode.INVALID_RANGE_HEADER_TABLE_NAME, msg);
        }    
        tokens = tokens[1].Split(",");
        for (int r = startRow+2; r<=endRow; r++)
        {
            string productHeadValue = cells[r, startCol].Value?.ToString() ?? "";
            if (productHeadValue.StartsWith(PRODUCT) && endRow > r + 1)
            {
                string productTypeHeadValue = cells[r+1, startCol].Value?.ToString() ?? "";
                string rangeHeadValue = cells[r+2, startCol].Value?.ToString() ?? "";
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
                            VfileId = FILE_ID,
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
                            VfileId = FILE_ID,
                            ProductTypeId = chosenProductTypeID,
                            ProductId = chosenProductID,
                            Name = productTypeName,
                            CreatedBy = 1,
                            CreatedDate = DateTime.Today,
                            Rstatus = (byte)RStatus.Active
                        });
                    }
                    // Add the range
                    MRangeWithRow mRWR = new()
                    {
                        Row = startRow,   // Add the range's row num to do series range assigns using distance closeness later & decide the RangeNum
                        DBRange = new()
                        {
                            VfileId = FILE_ID,
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
                    mRWR.setSeriesNames.UnionWith(tokens);
                    mRangesWithRow.Add(mRWR);
                    addMTableCells(efMTable.TableId, efCells, FILE_ID, startRow, endRow, startCol, endCol, cells);
                    //To be done at end only
                    ++rangeID;
                }
                else
                {
                    string msg = $"Invalid Product Type/Range for table {tableName}, pt:{productTypeHeadValue}, r:{rangeHeadValue}";
                    Console.WriteLine(msg);
                    return(-(int)ParseRetCode.INVALID_PRODUCT_TYPE_OR_RANGE, msg);
                }
                break; // break out of RANGE_HEADER field parsing for-loop
            }
            else
            {
                string msg = $"Invalid Product/Product Type/Range for {tableName}, p: {productHeadValue}";
                Console.WriteLine(msg);
                return(-(int)ParseRetCode.INVALID_PRODUCT, msg);
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
            string msg = $"Invalid series header Table Name: {tableName}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.INVALID_SERIES_HEADER_TABLE_NAME, msg);
        }
        var seriesName = tokens[1];
        if(seriesName.Trim() == "")
        {
            string msg = $"Invalid SERIES HEADER, no series name:{tableName}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.INVALID_SERIES_HEADER_SERIES_NAME, msg);
        }
        if(efMTable.NumRows < RANGE_SERIES_HEADER_MIN_ROWS || efMTable.NumCols < RANGE_SERIES_HEADER_MIN_COLS)
        {
            string msg = $"Invalid SERIES HEADER, dimensions for {tableName}, rows:{efMTable.NumRows}, cols:{efMTable.NumCols}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.INVALID_SERIES_HEADER_DIMS, msg);
        }
        // Add MSeries with row from Series Header table
        mSeriesWithHeaderTableRow.Add(new()
        {
            DBSeries = new()
            {
                VfileId = FILE_ID,
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
        efMTable.SeriesId = seriesID;
        efMTable.TableType = (int)TableTypes.RANGE_SERIES_HEADER;
        efMTable.Name = $"{RANGE_SERIES_TABLE}_{seriesName}_seriesid_{seriesID}";
        // Add the Series Header MTable cells
        addMTableCells(efMTable.TableId, efCells, FILE_ID, startRow, endRow, startCol, endCol, cells);
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
            string msg = $"Invalid SERIES DETAIL Table Name: {tableName}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.INVALID_SERIES_DETAIL_TABLE_NAME, msg);
        }
        var seriesName = tokens[1];
        if (seriesName.Trim() == "")
        {
            string msg = $"Invalid SERIES DETAIL table, no series name:{tableName}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.INVALID_SERIES_DETAIL_SERIES_NAME, msg);
        }
        if (efMTable.NumRows < RANGE_SERIES_DETAIL_MIN_ROWS || efMTable.NumCols < RANGE_SERIES_DETAIL_MIN_COLS)
        {
            string msg = $"Invalid SERIES DETAIL table, dimensions for {tableName}, rows:{efMTable.NumRows}, cols:{efMTable.NumCols}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.INVALID_SERIES_DETAIL_DIMS, msg);
        }
        // Add to list
        seriesDetailMTablesWithRow.Add(new()
        {
            DBTable = efMTable,   // SeriesId in it will be updtd later 
            Row = startRow,
        });
        efMTable.TableType = (int)TableTypes.RANGE_SERIES_DETAIL_MINMAX;
        efMTable.Name = seriesName;  // series id unknown at this point
        efMTable.RangeId = -1;
        efMTable.SeriesId = -1;
        // Add cells
        addMTableCells(efMTable.TableId, efCells, FILE_ID, startRow, endRow, startCol, endCol, cells);

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
        // Add cells
        addMTableCells(efMTable.TableId, efCells, FILE_ID, startRow, endRow, startCol, endCol, cells);
    }

    return ((int)ParseRetCode.SUCCESS, "");
}



static void addMTableCells(
    int tableId, 
    List<Cell> efCells,
    int FILE_ID,
    int startRow,
    int endRow, 
    int startCol, 
    int endCol, 
    ExcelRange cells)
{
    // Add the table cells
    int cellID = 1;
    for (int tr = startRow; tr <= endRow; ++tr)
    {
        for (int tc = startCol; tc <= endCol; ++tc)
        {
            string cellValue = cells[tr, tc].Value?.ToString() ?? "";
            string cellFormula = cells[tr, tc].Formula;
            string cellComment = cells[tr, tc].Comment?.ToString() ?? "";
            // Add db cell
            var efCell = new Cell
            {
                VfileId = FILE_ID,
                TableId = tableId,
                CellId = cellID,
                RowNum = tr - startRow + 1,  // 1 based index
                ColNum = tc - startCol + 1,
                Value = cellValue,
                Formula = cellFormula,
                Format = "",
                Style = "",
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


//----------------------------------- download ----------------------------------------

const int INITIAL_TOP_ROW_OFFSET = 1;
const int MIN_CELL_COUNT_RANGE_HEADER = 10;
const int DEFAULT_OUTPUT_COLUMN_OFFSET = 0;
const int INTER_RANGE_ROW_GAP = 5;
const int SERIES_HEADER_DEAIL_ROW_GAP = 1;
const int INTER_SERIES_ROW_GAP = 2;
const int INTER_MASTER_TABLE_ROW_GAP = 5;

var listSkip = new List<string>()
{
    ""
};

// TODO: return errors
void createExcelFile(int fileId, string outputPath)
{    
    // Open the workbook (or create it if it doesn't exist)
    using (var p = new ExcelPackage(outputPath))
    {
        using (var db = new WildContext(_optionsBuilder.Options))
        {
            //ExcelWorksheets worksheets = p.Workbook.Worksheets;
            try
            {
                var sheets = db.Sheets.Where(s => s.VfileId == FILE_ID).OrderBy(s => s.SheetNum).ToList();
                foreach (var sheet in sheets)
                {
                    if (listSkip.Contains(sheet.Name))
                    {
                        Console.WriteLine($"Skipping {sheet.Name}");
                        continue;
                    }
                    Console.WriteLine($"Creating sheet {sheet.Name}...");
                    // Create sheet in output file
                    var outSheet = p.Workbook.Worksheets.Add(sheet.Name);
                    // Get the ranges in RangeNum order for this sheet
                    var listRanges = (from r in db.Ranges
                                 where r.VfileId == fileId && r.SheetId == sheet.SheetId
                                 orderby r.RangeNum
                                 select (new OutRangeInfo { RangeId = r.RangeId, HeaderTableId = r.HeaderTableId })).ToList();
                    int currentRow = 1 + INITIAL_TOP_ROW_OFFSET;
                    foreach (var range in listRanges)
                    {
                        Console.WriteLine($"Creating range id: {range.RangeId}");
                        // Now put the range header first in outSheet
                        addRangeHeaderToOutSheet(db, FILE_ID, outSheet, ref currentRow, range);
                        // Get the series in SeriesNum order for this sheet and range
                        var listSeries = db.Series.Where(s => s.VfileId == fileId && s.SheetId == sheet.SheetId && s.RangeId == range.RangeId)
                                       .OrderBy(s => s.SeriesNum)
                                       .Select(s => new OutSeriesInfo{ HeaderTableId = s.HeaderTableId, DetailTableId = s.DetailTableId })
                                       .ToList();
                        foreach(var series in listSeries)
                        {
                            addSeriesHeaderAndDetailToOutSheet(db, FILE_ID, outSheet, ref currentRow, series);
                            // Add inter-series gap
                            currentRow += INTER_SERIES_ROW_GAP;
                        }
                        // Add inter-range separator
                        var interRangeRow = currentRow + INTER_RANGE_ROW_GAP/2;
                        var destCells = outSheet.Cells;
                        var dstRange = destCells[interRangeRow, 1, interRangeRow, 20];
                        dstRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        dstRange.Style.Fill.BackgroundColor.SetColor(Color.Pink);
                        // Add inter-range gap
                        currentRow += INTER_RANGE_ROW_GAP;
                    } // foreach (var range
                    // Now put the master tables
                    var listMTables = db.Tables.Where(t => t.VfileId == fileId && t.SheetId == sheet.SheetId && t.TableType == (int)TableTypes.MASTER).ToList();
                    foreach (var table in listMTables)
                    {
                        // TODO: validations
                        var dbCells = db.Cells.Where(c => c.VfileId == fileId && c.TableId == table.TableId).ToList();
                        // TODO: validations
                        addTableCellsToOutSheet(fileId, outSheet, ref currentRow, dbCells, table);
                        currentRow += table.NumRows + 1;
                        currentRow += INTER_MASTER_TABLE_ROW_GAP;
                    }
                    

                } // end-foreach worksheet

                // Save the file
                p.Save();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
                }
            }

        } // end-using WildContext
    } // end-using ExcelPackage    

}

(int, string) addSeriesHeaderAndDetailToOutSheet(WildContext db,int fileId,  ExcelWorksheet outSheet, ref int currentRow, OutSeriesInfo series)
{
    // Add Series Header
    // TODO: validations
    var listMTables = db.Tables.Where(t => t.VfileId == fileId && t.TableId == series.HeaderTableId).ToList();
    // TODO: validations
    var mTable = listMTables[0];
    // TODO: validations
    var dbCells = db.Cells.Where(c => c.VfileId == fileId && c.TableId == mTable.TableId).ToList();
    // TODO: validations
    addTableCellsToOutSheet(fileId, outSheet, ref currentRow, dbCells, mTable);
    currentRow += mTable.NumRows + 1;
    // Add header-detail gap
    currentRow += SERIES_HEADER_DEAIL_ROW_GAP;
    // Add Series Detail
    // TODO: validations
    listMTables = db.Tables.Where(t => t.VfileId == fileId && t.TableId == series.DetailTableId).ToList();
    // TODO: validations
    mTable = listMTables[0];
    // TODO: validations
    dbCells = db.Cells.Where(c => c.VfileId == fileId && c.TableId == mTable.TableId).ToList();
    // TODO: validations
    addTableCellsToOutSheet(fileId, outSheet, ref currentRow, dbCells, mTable);
    currentRow += mTable.NumRows + 1;
    return ((int)ParseRetCode.SUCCESS, "");
}

(int, string) addRangeHeaderToOutSheet(WildContext db, int fileId, ExcelWorksheet outSheet, ref int currentRow, OutRangeInfo range)
{
    // Get the MTable
    var listMTables = db.Tables.Where(t => t.VfileId == fileId && t.TableId == range.HeaderTableId).ToList();
    if (listMTables.Count() > 1)
    {
        string msg = $"There are no RANGE HEADER MTables in vfile: {fileId}";
        Console.WriteLine(msg);
        return (-(int)ParseRetCode.RANGE_HEADER_MTABLES_NOT_FOUND, msg);
    }
    var mTable = listMTables[0];
    if (mTable.RangeId != range.RangeId)
    {
        string msg = $"MTable RangeId does not MRange RangeId in vfile: {fileId}";
        Console.WriteLine(msg);
        return (-(int)ParseRetCode.MTABLE_RANGE_ID_MISMATCH, msg);
    }
    if (mTable.TableType != (int)TableTypes.RANGE_HEADER)
    {
        string msg = $"MTable type is not RANGE HEADER in vfile: {fileId}, type is {mTable.TableType}";
        Console.WriteLine(msg);
        return (-(int)ParseRetCode.MTABLE_TYPE_MISMATCH, msg);
    }
    var dbCells = db.Cells.Where(c => c.VfileId == fileId && c.TableId == mTable.TableId).ToList();
    if (dbCells.Count() < MIN_CELL_COUNT_RANGE_HEADER)
    {
        string msg = $"MTable RANGE HEADER has {dbCells.Count()} cells, but at least ({MIN_CELL_COUNT_RANGE_HEADER}) cells expected, in vfile: {fileId}";
        Console.WriteLine(msg);
        return (-(int)ParseRetCode.MTABLE_LESS_CELLS, msg);
    }
    addTableCellsToOutSheet(fileId, outSheet, ref currentRow, dbCells, mTable);
    // Incr currentRow past the just added table for next table
    currentRow += mTable.NumRows+1;
    return ((int)ParseRetCode.SUCCESS, "");
}


(int, string) addTableCellsToOutSheet(
    int fileId, ExcelWorksheet outSheet, ref int currentRow, List<Cell> dbCells, MTable mTable)
{
    int startCol = DEFAULT_OUTPUT_COLUMN_OFFSET;
    var destCells = outSheet.Cells;
    foreach (var dbCell in dbCells)
    {
        if (dbCell.RowNum > mTable.NumRows)   // TODO this check needs to be done when adding cells too during parsing
        {
            string msg = $"MTable id {mTable.TableId} has Cell.RowNum > MTable.NumRows({dbCell.RowNum} > {mTable.NumRows}), CellID is {dbCell.CellId}, in vfile: {fileId}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.MTABLE_CELL_INVALID_ROW, msg);
        }
        if (dbCell.ColNum > mTable.NumCols)
        {
            string msg = $"MTable id {mTable.TableId} has Cell.ColNum > MTable.NumCols({dbCell.ColNum} > {mTable.NumCols}), CellID is {dbCell.CellId}, in vfile: {fileId}";
            Console.WriteLine(msg);
            return (-(int)ParseRetCode.MTABLE_CELL_INVALID_COL, msg);
        }
        var r = currentRow + dbCell.RowNum;   // offset by current row & col
        var c = startCol + dbCell.ColNum;
        var cellDest = destCells[r, c];
        cellDest.Value = dbCell.Value;
    }
    // Do table adjustments
    var firstRow = currentRow + 1;
    var firstCol = startCol + 1;
    //if (mTable.HeaderRow)
    //    firstRow++;
    var lastRow = firstRow + mTable.NumRows - 1;
    var lastCol = firstCol + mTable.NumCols - 1;
    var dstRange = destCells[firstRow, firstCol, lastRow, lastCol];

    Console.WriteLine("mTable.Name:" + mTable.Name);

    var dstTable = outSheet.Tables.Add(dstRange, mTable.Name);
    dstTable.ShowHeader = mTable.HeaderRow;
    //dstRange.Calculate();  // no formulas in outSheet planned yet.
    dstRange.AutoFitColumns();
    return ((int)ParseRetCode.SUCCESS, "");
}


//---------------------  Comparison ---------------------------------

(int, string) compareWorkbooks(string path1, string path2)
{
    Console.WriteLine($"Comparing {path1} and {path2}...");

    using(var p1 = new ExcelPackage(path1))
    {
        using (var p2 = new ExcelPackage(path2))
        {
            var sheets1 = p1.Workbook.Worksheets;
            var sheets2 = p2.Workbook.Worksheets;
            foreach( var sheet1 in sheets1)
            {
                var sheet2 = sheets2[sheet1.Name];
                if ( sheet2 == null )
                {
                    string msg = $"Sheets 2 does not have sheet {sheet1.Name}";
                    Console.WriteLine(msg);
                    return (-(int)ParseRetCode.COMPARE_SHEET_ABSENT, msg);
                }
                
                var tables1 = sheet1.Tables;
                var tables2 = sheet2.Tables;
                foreach(var table1 in tables1)
                {
                    // Get name from Table Name field only
                    var (tableName1, startRow1, startCol1, endRow1, endCol1) = getTableName(table1);
                    int numRows1 = endRow1 - startRow1 + 1;
                    int numCols1 = endCol1 - startCol1 + 1;
                    // Find table1 in tables2
                    var wasTableFound = false;
                    foreach (var table2 in tables2)
                    {                        
                        var (tableName2, startRow2, startCol2, endRow2, endCol2) = getTableName(table2);                        
                        if ( tableName2 == tableName1)
                        {
                            // Table found
                            wasTableFound = true;
                            // Check num rows & cols for early mismatch exit
                            int numRows2 = endRow2 - startRow2 + 1;
                            int numCols2 = endCol2 - startCol2 + 1;
                            if (numRows1 != numRows2 || numCols1 != numCols2)
                            {
                                string msg = $"Row/col mismatch: numRows1:{numRows1},numCols1:{numCols1}, numRows2:{numRows2},numCols2:{numCols2}, tableName:{tableName1}, sheet:{sheet1.Name}";
                                Console.WriteLine(msg);
                                return (-(int)ParseRetCode.COMPARE_TABLE_ROW_COL_MISMATCH, msg);
                            }
                            // finally compare cells                            
                            var Cells1 = sheet1.Cells;
                            var Cells2 = sheet2.Cells;
                            for(var r = 0; r < numRows1; ++r)
                            {
                                for (var c = 0; c < numCols1; ++c)
                                {
                                    var cr1 = startRow1 + r;
                                    var cc1 = startCol1 + c;
                                    var cr2 = startRow2 + r;
                                    var cc2 = startCol2 + c;
                                    var val1 = Cells1[cr1,cc1].Value?.ToString() ?? "";
                                    var val2 = Cells2[cr2,cc2].Value?.ToString() ?? "";
                                    if (val1 != val2)
                                    {
                                        string msg = $"Cell value mismatch in table {tableName1} cr1:{cr1}, cc1:{cc1}, cr2:{cr2}, cc2:{cc2}";
                                        Console.WriteLine(msg);
                                        return (-(int)ParseRetCode.COMPARE_CELL_VALUE_MISMATCH, msg);
                                    }
                                }
                            }
                            break;
                        }
                    }
                    if(!wasTableFound)
                    {
                        string msg = $"Table not found in sheets 2: table name{tableName1}";
                        Console.WriteLine(msg);
                        return (-(int)ParseRetCode.COMPARE_TABLE_NOT_FOUND, msg);
                    }
                }

            }

        }
    }

    
    
    return ((int)ParseRetCode.SUCCESS, "");
}


(string, int, int, int, int) getTableName(ExcelTable table)
{
    var addr = table.Address;
    var startRow = addr.Start.Row;
    var endRow = addr.End.Row;
    var startCol = addr.Start.Column;
    var endCol = addr.End.Column;
    var cells = table.WorkSheet.Cells;
    string tableName = cells[startRow, startCol + 1].Value?.ToString() ?? "";
    tableName = tableName.Trim();
    //Console.WriteLine($"Processing {tableName}...");
    return (tableName, startRow, startCol, endRow, endCol);
}


//----------------------------- main -----------------------------
// Upload
const string filePath = "E:\\Code\\RApps\\sheets\\CarsV1.xlsm";
// Download
const string outputPath = "E:\\Code\\RApps\\output\\V1Out.xlsx";
// Compare
const string path1 = "E:\\Code\\RApps\\output\\CarsV1.xlsm";
const string path2 = "E:\\Code\\RApps\\output\\V1Out.xlsx";

BuildOptions();
//CreateWorkbook("TextWB");

//loadWorkbook(filePath);

//loadCarsFile(filePath);

//createExcelFile(FILE_ID, outputPath);

//compareWorkbooks(path1, path2);

