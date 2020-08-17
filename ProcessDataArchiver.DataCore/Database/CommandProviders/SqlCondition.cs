using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    [Serializable]
    public class SqlCondition 
    {
        public string ColumnName { get; set; }
        public string AndCondition { get; set; }
        public string OrCondition { get; set; }


        public override bool Equals(object obj)
        {
            var o = obj as SqlCondition;

            return o != null && o.AndCondition.Equals(AndCondition)
                && o.OrCondition.Equals(OrCondition)
                && o.ColumnName.Equals(ColumnName);
        }

        public override int GetHashCode()
        {
            return AndCondition.GetHashCode() + OrCondition.GetHashCode() + ColumnName.GetHashCode();
        }
    }
}
