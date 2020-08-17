using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.Windows.Commands
{
    interface ISqlQueryCommands:IQueryCommands
    {
        bool CanRedo { get; }
        bool CanUndo { get; }
        bool CanCut { get; }
        bool CanDelete { get; }
        bool CanCopy { get; }
        bool CanPaste { get; }
        bool CanSelect { get; }


        void Redo();
        void Undo();
        void Cut();
        void Copy();
        void Delete();
        void Paste();
        void Select();

        event EventHandler StateChanged;
    }
}
