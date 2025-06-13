// TODO: Checks on table formats:
// There must be all possible checks done according to known facts.
// Must detect invalid tables
//

// Issues:
//   Some of the functions do not return with error code, return error code not checked in caller.


using EFCore_DBLibrary;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System.Drawing;
using System.Text.Json;
using WildExcelLoader.models;
using WildSheetLoader;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;



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
                        VfileId = FILE_ID,
                        SheetId = sheet.Index+1,  // dont want this 0 based
                        Name = sheet.Name,
                        SheetNum = (short)(sheet.Index+1),
                        Style = "",
                        StartRowNum = sheetStartRow,
                        StartColNum = sheetStartCol,
                        EndRowNum = sheetEndRow,
                        EndColNum = sheetEndCol,
                        CreatedBy = 1,
                        CreatedDate = DateTime.Today,
                        Rstatus = (byte)RStatus.Active
                    };
                    db.Sheets.Add(efSheet);
                    // Add the cells of the sheet  
                    addSheetCells(
                        efSheet.SheetId,
                        efCells,
                        FILE_ID,
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
                            VfileId = FILE_ID,
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
                        db,                       
                        efMTables,
                        mRangesWithRow,
                        mSeriesWithHeaderTableRow,                        
                        efCells
                    );

                    // Add sheet cells                    
                    db.Cells.AddRange(efCells);
                    db.SaveChanges();

                } // end for-sheets

                // Workbook-wide data
                saveProductData(
                    db,
                    dictProducts,
                    dictProductTypes);

                // Execute update query to update ranges
                // TODO - wasnt this solved already? Do we still need it?
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

        if(mRangesWithRow[rangeIdx].setSeriesNames.Count > 0){
            Console.WriteLine($"setSeriesNames is not empty after RangeId:{currRangeWithRow.DBRange.RangeId} completed");
        }


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

void addRangesSeriesAndTablesToContext(
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
    //db.Cells.AddRange(efCells);

    //db.SaveChanges();
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
    Dictionary<int, MTable> dictSeriesIdVsSeriesHdrMTable,
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
                    // Add the range just figured out above
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
                    mRWR.setSeriesNames.UnionWith(tokens); // The series names got from the RANGE HEADER are put here
                    mRangesWithRow.Add(mRWR);
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



static void addSheetCells(
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
            CellStyle cellStyle;
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

static bool IsCellEmpty(
    string cellValue, 
    string cellFormula, 
    string cellComment, 
    string cellFormat, 
    CellStyle cellStyle)
{
    return cellValue == "" && 
        cellFormula == "" && 
        cellComment == "" &&
        cellFormat == "" &&
        cellStyle.bg == "";
}

static void GetCellProperties(
        ExcelRange cell,
        out string cellValue,
        out string cellFormula,
        out string cellComment,
        out string cellFormat,
        out CellStyle cellStyle)
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

// Apply default values here to set empty strings
// {"bg":"","font":{"c":"","n":"Calibri","b":true,"i":false,"u":false,"s":false}}
static CellStyle GetBriefCellStyle(ExcelRange cell)
{
    CellStyle cellStyle = new()
    {
        bg = cell.Style.Fill.BackgroundColor.Rgb ?? "",
        font = new() {
            c = cell.Style.Font.Color.Rgb,
            n = cell.Style.Font.Name,
            b = cell.Style.Font.Bold ? "1":"",
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
static string GetCellStyleAsJSONString(CellStyle style)
{
    var font = style.font;
    if (style.bg== "" && 
        font.c =="" &&
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
                    var destCells = outSheet.Cells;
                    // Create the cells of the sheet
                    var vFileIdParam = new SqlParameter("vFileIdParam", fileId);
                    var activeStatusParam = new SqlParameter("activeStatusParam", RStatus.Active);
                    var sheetId = sheet.SheetId;
                    var sheetIdParam = new SqlParameter("sheetIdParam", sheetId);
                    var listCellsResult = db.Database.SqlQuery<Cell>(
                            @$"SELECT * FROM mpm.Cells WHERE 
                            SheetId={sheetIdParam} AND VFileID={vFileIdParam} AND RStatus={activeStatusParam}
                            ORDER BY RowNum, ColNum").ToList();                    
                    foreach (var dbCell in listCellsResult)
                    {
                        var r = dbCell.RowNum;
                        var c = dbCell.ColNum;
                        var cellDest = destCells[r, c];
                        if (dbCell.Formula.Trim().Length > 0)
                        {
                            cellDest.Formula = dbCell.Formula;
                        }
                        else
                        {
                            cellDest.Value = dbCell.Value;
                        }                        
                        // TODO: Apply style, format, comment
                    }
                    // Create all the tables
                    var listMTables = db.Tables.Where(t => t.VfileId == fileId && t.SheetId == sheet.SheetId).ToList();
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

/*
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
*/

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

createExcelFile(FILE_ID, outputPath);



//compareWorkbooks(path1, path2);

