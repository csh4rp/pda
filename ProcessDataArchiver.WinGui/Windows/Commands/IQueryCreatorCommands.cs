using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.Windows.Commands
{
    public interface IQueryCreatorCommands:IQueryCommands,IEditCommands
    {
        void ChangeTable(string tableName);
    }
}
