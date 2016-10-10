using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Office.Interop.Excel;

namespace cloancalculationapp
{
    class ExcelConnect
    {
        Microsoft.Office.Interop.Excel.Application _excelApp;

        public ExcelConnect()
        {
            _excelApp = new Microsoft.Office.Interop.Excel.Application();
        }
        /// <summary>
        /// Open the file path received in Excel. Then, open the workbook
        /// within the file. Send the workbook to the next function, the internal scan
        /// function. Will throw an exception if a file cannot be found or opened.
        /// </summary>
        public void ExcelOpenSpreadsheets(string thisFileName)
        {
            try
            {
                //
                // This mess of code opens an Excel workbook. I don't know what all
                // those arguments do, but they can be changed to influence behavior.
                //
                Workbook myWorkBook = _excelApp.Workbooks.Open(thisFileName,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing);

                //
                // Pass the workbook to a separate function. This new function
                // will iterate through the worksheets in the workbook.
                //
                ExcelScanIntenal(myWorkBook);

                //
                // Clean up.
                //
                myWorkBook.Close(false, thisFileName, null);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(myWorkBook);

            }
            catch
            {
                //
                // Deal with exceptions.
                //
            }
        }

        /// <summary>
        /// Scan the selected Excel workbook and store the information in the cells
        /// for this workbook in an object[,] array. Then, call another method
        /// to process the data.
        /// </summary>
        private void ExcelScanIntenal(Workbook workBookIn)
        {
            //
            // Get sheet Count and store the number of sheets.
            //
            int numSheets = workBookIn.Sheets.Count;

            //
            // Iterate through the sheets. They are indexed starting at 1.
            //
            for (int sheetNum = 1; sheetNum < numSheets + 1; sheetNum++)
            {
                Worksheet sheet = (Worksheet)workBookIn.Sheets[sheetNum];

                //
                // Take the used range of the sheet. Finally, get an object array of all
                // of the cells in the sheet (their values). You can do things with those
                // values. See notes about compatibility.
                //
                Range excelRange = sheet.UsedRange;
                object[,] valueArray = (object[,])excelRange.get_Value(
                    XlRangeValueDataType.xlRangeValueDefault);

                //
                // Do something with the data in the array with a custom method.
                //
                //ProcessObjects(valueArray);

            }
        }
    }

    class Program
    {
        const string fileName = "C:\\Book1.xlsx";
        const string topLeft = "A1";
        const string bottomRight = "A4";
        const string graphTitle = "Graph Title";
        const string xAxis = "Time";
        const string yAxis = "Value";


        static void Main()
        {
            // Open Excel and get first worksheet.
            var myXLApplication = new Microsoft.Office.Interop.Excel.Application();
            var workbook = myXLApplication.Workbooks.Open(fileName);
            var worksheet = workbook.Worksheets[1] as
                Microsoft.Office.Interop.Excel.Worksheet;

            // Add chart.
            var charts = worksheet.ChartObjects() as
                Microsoft.Office.Interop.Excel.ChartObjects;
            var chartObject = charts.Add(60, 10, 300, 300) as
                Microsoft.Office.Interop.Excel.ChartObject;
            var chart = chartObject.Chart;

            // Set chart range.
            var range = worksheet.get_Range(topLeft, bottomRight);
            chart.SetSourceData(range);

            // Set chart properties.
            chart.ChartType = Microsoft.Office.Interop.Excel.XlChartType.xlLine;
            chart.ChartWizard(Source: range,
                Title: graphTitle,
                CategoryTitle: xAxis,
                ValueTitle: yAxis);

            // Save.
            workbook.Save();
        }
    }
}
