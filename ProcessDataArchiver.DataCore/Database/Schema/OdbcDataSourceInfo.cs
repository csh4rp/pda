using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ProcessDataArchiver.DataCore.Database.Schema
{
    public class OdbcDataSourceInfo
    {
        public string Dsn { get; set; }
        public string Driver { get; set; }

    }
}
