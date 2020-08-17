using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace ProcessDataArchiver.DataCore.Infrastructure
{
    public static class DataExportProvider
    {
        public static void ExportData(DataTable dt, string path)
        {
            string ext = Path.GetExtension(path);

            if (ext.Equals(".xml", StringComparison.InvariantCultureIgnoreCase))
            {
                ExportDataAsXml(dt,path);
            }
            else if (ext.Equals(".csv", StringComparison.InvariantCultureIgnoreCase))
            {
                ExportDataAsCsv(dt, path);
            }
            else if (ext.Equals(".xls", StringComparison.InvariantCultureIgnoreCase))
            {
                ExportDataAsXls(dt, path);
            }
        }

        public static Task ExportDataAsync(DataTable dt,string path)
        {
            return Task.Run(() => { ExportData(dt, path); });
        }


        private static void ExportDataAsXml(DataTable dt, string path)
        {
            dt.WriteXml(File.Create(path));
        }

        private static void ExportDataAsCsv(DataTable dt, string path)
        {
            int columnsCount = dt.Columns.Count;
            int rowCount = dt.Rows.Count;
            var lines = new List<StringBuilder>(rowCount);
            StringBuilder sb = new StringBuilder();

            foreach (DataColumn dc in dt.Columns)
            {
                sb.Append(dc.ColumnName + ";");
            }
            lines.Add(sb);

            for (int i = 0; i < rowCount; i++)
            {
                lines.Add(new StringBuilder());

                for (int j = 0; j < columnsCount; j++)
                {
                    lines[i].Append(dt.Rows[i][j].ToString() + ";");
                }
            }

            using (var sw = File.CreateText(path))
            {
                foreach (var line in lines)
                {
                    sw.WriteLine(line.ToString());
                }
            }
        }

        private static void ExportDataAsXls(DataTable dt, string path)
        {
            int columnsCount = dt.Columns.Count;
            int rowCount = dt.Rows.Count;
            var lines = new List<StringBuilder>(rowCount);

            var exApp = new Excel.Application();
            exApp.Workbooks.Add();
            Excel._Worksheet ws = exApp.ActiveSheet;

            for (int i = 0; i < columnsCount; i++)
            {
                ws.Cells[1, i+1] = dt.Columns[i].ColumnName;
            }

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnsCount; j++)
                {
                    ws.Cells[i+2, j+1] = dt.Rows[i][j];
                }
            }
            ws.SaveAs(path);
            Excel._Workbook wb = exApp.ActiveWorkbook;
            Marshal.ReleaseComObject(ws);
            Marshal.ReleaseComObject(exApp);

            wb.Close(true);
        }
    }
}
