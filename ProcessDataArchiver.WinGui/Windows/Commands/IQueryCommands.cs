using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.Windows.Commands
{
    public interface IQueryCommands
    {
        bool CanRunQuery { get; }
        bool CanSaveQuery { get; }
        bool CanExport { get; }

        void RunQuery();
        void SaveQuery();
        void OpenQuery();
        void Export();

    }
}
