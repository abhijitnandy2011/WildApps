// See https://aka.ms/new-console-template for more information


using EFCore_DBLibrary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System.Net.NetworkInformation;
using WildSheetLoader;
using static OfficeOpenXml.ExcelErrorValue;


ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

//---------------------------------------
// DB setup

IConfigurationRoot _configuration;
DbContextOptionsBuilder<WildContext> _optionsBuilder;

void BuildOptions()
{
    _configuration = ConfigurationBuilderSingleton.ConfigurationRoot;
    _optionsBuilder = new DbContextOptionsBuilder<WildContext>();
    _optionsBuilder.UseSqlServer(_configuration.GetConnectionString("WildDB"));
}


//--------------------------------------
// Excel loading functions
void CreateWorkbook(string name)
{
    using (var db = new WildContext(_optionsBuilder.Options))
    {
        //determine if item exists:
        //var existingItem = db.Workbooks.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        //if (existingItem == null)
        //{
        //doesn't exist, add it.
        var item = new Workbook()
        {
            Name = name,
            CreatedDate = DateTime.Today
        };
        db.Workbooks.Add(item);
        db.SaveChanges();
        //}
    }
}

void loadWorkbook(string filePath)
{
    Console.WriteLine(filePath);

    // Open the workbook (or create it if it doesn't exist)
    using (var p = new ExcelPackage(filePath))
    {
        // Get the Worksheet created in the previous codesample. 
        var ws = p.Workbook.Worksheets["Mahindra"];
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
                

                foreach (var sheet in worksheets)
                {
                    List<Cell> efCells = new();
                    List<XlTable> efXlTables = new();

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

                    Console.WriteLine("End Row:" + sheet.Dimension.End.Row);
                    Console.WriteLine("End Col:" + sheet.Dimension.End.Column);
                    var wsSrc = sheet;
                    // Limit data copying from really big sheets, those will need checks
                    // TODO: No row limits - should be configurable
                    var endCol = sheet.Dimension.End.Column;
                    if (endCol > 500)
                    {
                        Console.WriteLine("Really big COLS:" + endCol);
                        endCol = 500;
                    }
                    // Create the DB sheet
                    var efSheet = new Sheet
                    {
                        WorkbookId = 1,
                        Name = sheet.Name,
                        SheetNum = 1,
                        Style = "",
                        StartRowNum = sheet.Dimension.Start.Row,
                        StartColNum = sheet.Dimension.Start.Column,
                        EndRowNum = sheet.Dimension.End.Row,
                        EndColNum = endCol,
                        CreatedBy = 1,
                        CreatedDate = DateTime.Today,
                        Rstatus = 1
                    };
                    db.Sheets.Add(efSheet);
                    db.SaveChanges();   // Need the sheet id immediately
                    Console.WriteLine(efSheet.Id);

                    // Copy sheet data                    
                    for (int r = 1; r <= sheet.Dimension.End.Row; ++r)
                    {
                        for (int c = 1; c <= endCol; ++c)
                        {
                            var cellSrc = wsSrc.Cells[r, c];
                            string value = cellSrc.Value?.ToString() ?? "";
                            // Add db cell
                            var efCell = new Cell
                            {
                                SheetId = efSheet.Id,
                                RowNum = r,
                                ColNum = c,
                                Value = value,
                                Formula = "",
                                Format = "",
                                Style = "",
                                CreatedBy = 1,
                                CreatedDate = DateTime.Today,
                                Rstatus = 1
                            };
                            efCells.Add(efCell);                            
                        }
                    }

                    var tables = sheet.Tables;
                    foreach (var table in tables)
                    {
                        Console.WriteLine($"Table: {table.Name} range:{table.Range}");
                        var addr = table.Address;
                        // Add db cell
                        var efXlTable = new XlTable
                        {
                            SheetId = efSheet.Id,
                            Name = table.Name,
                            StartRowNum = addr.Start.Row,
                            StartColNum = addr.Start.Column,
                            EndRowNum = addr.End.Row,
                            EndColNum = addr.End.Column,
                            Style = table.TableStyle.ToString(),
                            HeaderRow = table.ShowHeader,
                            TotalRow = table.ShowTotal,
                            BandedRows = table.ShowRowStripes,
                            BandedColumns = table.ShowColumnStripes,
                            FilterButton = table.ShowFilter,
                            CreatedBy = 1,
                            CreatedDate = DateTime.Today,
                            Rstatus = 1
                        };
                        efXlTables.Add(efXlTable);
                    }

                    // Add sheet cells & tables                    
                    db.Cells.AddRange(efCells.ToArray());
                    db.XlTables.AddRange(efXlTables.ToArray());
                    db.SaveChanges();

                } // end for-sheets                
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


//-----------------------------

const string filePath = "E:\\Code\\Wild\\sheets\\CarsV1.xlsm";

BuildOptions();
//CreateWorkbook("TextWB");

loadWorkbook(filePath);
