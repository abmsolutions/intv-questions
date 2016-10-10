using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;


namespace cloancalculationapp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //PrintLogicalTree(0, this);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            //PrintVisualTree(0, this);
        }

        void PrintLogicalTree(int depth, object obj)
        {
            Debug.WriteLine(new string(' ', depth) + obj);

            if (!(obj is DependencyObject))
                return;

            foreach (object child in LogicalTreeHelper.GetChildren(obj as DependencyObject))
                PrintLogicalTree(depth + 1, child);
        }

        void PrintVisualTree(int depth, DependencyObject obj)
        {
            Debug.WriteLine(new string(' ', depth) + obj);

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                PrintVisualTree(depth + 1, VisualTreeHelper.GetChild(obj, i));
            }
        }

        private void btnCompute_Click(object sender, RoutedEventArgs e)
        {
            double PresentVal, Payment, FutureVal = 0;
            double APR;
            int TotalPmts;
            string Format = "###,###,##0.00";
            double[,] MonthlyStatus;

            if (!doSomeFormValidation())
            {
                MessageBox.Show("יש לתקן את השדה המסומן", "שגיאת הזנה", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            getLoanDataFromWindow(out PresentVal, out APR, out TotalPmts, out FutureVal);

            computeFullLoanTable(PresentVal, APR, out Payment, FutureVal, TotalPmts, out MonthlyStatus);

            populateResultsFieldsInWindow(PresentVal, Payment, TotalPmts, Format, MonthlyStatus, FutureVal);

            //populateExcelSheet();
        }

        private void populateResultsFieldsInWindow(double PresentVal, double Payment, int TotalPmts, string Format, double[,] MonthlyStatus, double FutureVal)
        {
            string TotalCost, MonthlyCost;
            TotalCost = (MonthlyStatus[TotalPmts - 1, 4] - PresentVal - FutureVal).ToString(Format);
            MonthlyCost = ((MonthlyStatus[TotalPmts - 1, 4] - PresentVal - FutureVal) / TotalPmts).ToString(Format);

            lblResults.Content = "התשלום הקבוע הוא " + Payment.ToString(Format) + " לחודש.";
            if (FutureVal == 0)
            {
                lblResults.Content += "\n" + "העלות הכוללת של ההלוואה היא " + TotalCost + ", או " + MonthlyCost + " לחודש.";
            }

            lvProfile.Items.Clear();

            for (int lvi = 0; lvi < TotalPmts; lvi++)
            {
                lvProfile.Items.Add(new EachMonth
                {
                    Month = (lvi + 1),
                    Interest = MonthlyStatus[lvi, 0].ToString(Format),
                    Principle = MonthlyStatus[lvi, 1].ToString(Format),
                    Payment = MonthlyStatus[lvi, 2].ToString(Format),
                    Remaining = MonthlyStatus[lvi, 3].ToString(Format),
                    Paid = MonthlyStatus[lvi, 4].ToString(Format)
                });
            }
        }

        public class EachMonth
        {
            public int Month { get; set; }
            public string Interest { get; set; }
            public string Principle { get; set; }
            public string Payment { get; set; }
            public string Remaining { get; set; }
            public string Paid { get; set; }
        }

        private void getLoanDataFromWindow(out double PresentVal, out double APR, out int TotalPmts, out double FutureVal)
        {

            PresentVal = double.Parse(txtLoanAmount.Text);

            APR = double.Parse(txtAPR.Text);
            APR = APR / 100;

            TotalPmts = int.Parse(txtMonths.Text);

            FutureVal = double.Parse(txtFinalLumpSum.Text);
        }

        private bool doSomeFormValidation()
        {
            System.Windows.Controls.TextBox currTxtBox;
            double number;
            bool pass = false;

            foreach (Control ctrl in myMainGrid.Children)
            {
                if (ctrl.GetType() == typeof(System.Windows.Controls.TextBox))                  // if (ctrl is TextBox)
                {
                    ctrl.Background = Brushes.White;
                    currTxtBox = ctrl as System.Windows.Controls.TextBox;                       // ((TextBox)ctrl);

                    if (!(double.TryParse(currTxtBox.Text, out number)))  // if parsing fails then
                    {
                        currTxtBox.Background = Brushes.Pink;
                        //currTxtBox.Select(0, currTxtBox.Text.Length);
                        currTxtBox.Focus();
                        pass = false;
                        return pass;
                    }
                    else
                    {
                        pass = true;
                    }
                }
            }
            return pass;
        }

        private static void computeFullLoanTable(double PresentVal, double APR, out double Payment, double FutureVal, int TotalPmts, out double[,] MonthlyStatus)
        {
            MonthlyStatus = new double[TotalPmts, 6];

            Payment = Microsoft.VisualBasic.Financial.Pmt(APR / 12, TotalPmts, -PresentVal, FutureVal, Microsoft.VisualBasic.DueDate.BegOfPeriod);

            //' calc the interest portion
            MonthlyStatus[0, 0] = PresentVal * APR / 12;
            //' calc the principle portion
            MonthlyStatus[0, 1] = Payment - MonthlyStatus[0, 0];
            //' store the payment 
            MonthlyStatus[0, 2] = Payment;
            //' calc the remaining balance
            MonthlyStatus[0, 3] = PresentVal - Payment;
            //' calc the total paid so far
            MonthlyStatus[0, 4] = Payment;
            //' store nothing
            MonthlyStatus[0, 5] = 0;

            for (int m = 1; m < TotalPmts; m++)
            {
                //' calc the interest portion
                MonthlyStatus[m, 0] = MonthlyStatus[m - 1, 3] * APR / 12;
                //' calc the principle portion
                MonthlyStatus[m, 1] = Payment - MonthlyStatus[m, 0];
                //' store the payment 
                MonthlyStatus[m, 2] = Payment;
                //' calc the remaining balance
                MonthlyStatus[m, 3] = MonthlyStatus[m - 1, 3] - MonthlyStatus[m, 1];
                //' calc the total paid so far
                MonthlyStatus[m, 4] = MonthlyStatus[m - 1, 4] + Payment;
                //' store nothing
                MonthlyStatus[m, 5] = MonthlyStatus[m, 0] + MonthlyStatus[m, 1];
            }
        }


        /*
        private void btnDoExcel_Click(object sender, RoutedEventArgs e)
        {
            const string fileName = @"C:\Users\Amit Morag\Documents\_May 2016 Reinvent\Excel_Stuff\Book1.xlsx";
            const string topLeft = "A1";
            const string bottomRight = "C14";
            const string graphTitle = "Graph Title";
            const string xAxis = "Time";
            const string yAxis = "Value";

            // Open Excel and get first worksheet.
            var myXLApplication = new Microsoft.Office.Interop.Excel.Application();
            myXLApplication.Visible = true;
            var workbook = myXLApplication.Workbooks.Open(fileName);
            var worksheet = workbook.Worksheets[1] as Worksheet;

            // Add chart.
            var charts = worksheet.ChartObjects() as ChartObjects;
            var chartObject = charts.Add(60, 10, 300, 300) as ChartObject;
            var chart = chartObject.Chart;

            // Set chart range.
            var range = worksheet.get_Range(topLeft, bottomRight);
            chart.SetSourceData(range);

            // Set chart properties.
            chart.ChartType = XlChartType.xlLine;
            chart.ChartWizard(Source: range,
                Title: graphTitle,
                CategoryTitle: xAxis,
                ValueTitle: yAxis);

            // Save.
            workbook.Save();
            myXLApplication.ActiveWindow.Close();
            Marshal.FinalReleaseComObject(myXLApplication);
        }
        */

        private void populateExcelSheet()
        {
            const string fileName = @"C:\Users\Amit Morag\Documents\_May 2016 Reinvent\Excel_Stuff\Book1.xlsx";

            // Open Excel and create new worksheet.
            var myXLApplication = new Microsoft.Office.Interop.Excel.Application();
            myXLApplication.Visible = true;
            var workbook = myXLApplication.Workbooks.Open(fileName);
            workbook.Sheets.Add();
            var worksheet = workbook.ActiveSheet as Worksheet;

            Range rng = worksheet.Cells[1, 1];

            rng.Select();
            rng.Value = 111;
            rng.Offset[0, 1].Value = 222;

        }

        private void txtLoanAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            doSomeFormValidation();
        }

        private void txtAPR_TextChanged(object sender, TextChangedEventArgs e)
        {
            doSomeFormValidation();
        }

        private void txtMonths_TextChanged(object sender, TextChangedEventArgs e)
        {
            doSomeFormValidation();
        }

        private void txtFinalLumpSum_TextChanged(object sender, TextChangedEventArgs e)
        {
            doSomeFormValidation();
        }
    }
}
