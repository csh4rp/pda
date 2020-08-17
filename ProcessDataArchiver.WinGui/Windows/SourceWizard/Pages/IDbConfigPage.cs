
using ProcessDataArchiver.DataCore.Database.DbProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.Windows.SourceWizard.Pages
{
    interface IDbConfigPage
    {
        bool HasErrors { get; }
        IDatabaseProvider DataSource { get; }

        event EventHandler<bool> ValidationChanged;
        event EventHandler<bool> ConnectionStateChanged;

        void Validate();
        Task<bool> Finish();
    }
}
