using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ProcessDataArchiver.DataCore.Database.Schema
{
    public  class ColumnSchema
    {

        public string ColumnName { get; set; }
        public Type DataType { get;set; }
        public bool IsNullable { get; set; }

        public int MaxLength { get; set; } = 20;

        public ColumnSchema(string columnName)
        {
            this.ColumnName = columnName;
        }

        public ColumnSchema(string columnName, Type type)
        {
            this.ColumnName = columnName;
            this.DataType = type;
        }

        public override string ToString()
        {
            return ColumnName;
        }

    }
}
