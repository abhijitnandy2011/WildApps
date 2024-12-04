// See https://aka.ms/new-console-template for more information


using OfficeOpenXml;
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;


const string filePath = "E:\\Code\\Wild\\sheets\\CarsV1.xlsm";

loadExcel(filePath);


void loadExcel(string filePath)
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


        ExcelWorksheets worksheets = p.Workbook.Worksheets;
        try
        {
            foreach (var sheet in worksheets)
            {
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
                Console.WriteLine("End Row:" + sheet.Dimension.End.Column);
                var wsSrc = sheet;
                // Limit data copying from really big sheets, those will need checks
                // TODO: No row limits - should be configurable
                var numCols = sheet.Dimension.End.Column;
                if (numCols > 500)
                {
                    Console.WriteLine("Really big COLS:" + numCols);
                    numCols = 500;
                }                

                // Copy sheet data
                for (int r = 1; r <= sheet.Dimension.End.Row; ++r) 
                {
                    for (int c = 1; c <= numCols; ++c) 
                    { 
                        var cellSrc = wsSrc.Cells[r,c];
                        var value = cellSrc.Value;
                    }
                }

                var tables = sheet.Tables;
                foreach(var table in tables)
                {  
                    Console.WriteLine($"Table: {table.Name} range:{table.Range}");
                }

            }
        }
        catch(Exception ex) 
        { 
            Console.WriteLine(ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Ex: {ex.InnerException.Message}\n Trace:{ex.StackTrace}");
            }
        }


    }

}