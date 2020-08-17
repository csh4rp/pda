using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.Windows.Commands
{
    public interface IEditCommands
    {
        bool CanAdd { get; }
        bool CanRemove { get; }

        event EventHandler StateChanged;

        void Add();
        void Remove();
    }
}
