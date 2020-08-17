using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.WinGui.Windows.Commands
{
    public interface ITrendCommands:IEditCommands
    {
        bool IsStarted { get; }
        bool IsStatic { get; set; }

        TimeSpan Timespan { get; set; }
        DateTime FromDate { get; set; }
        DateTime ToDate { get; set; }

        void Draw();
        void Start();
        void Stop();
        void ShowAdvanced();
        void ZoomIn();
        void ZoomOut();

        void ChangeTimespan(TimeSpan timespan);
        void ChangeTimeRange(DateTime from, DateTime to);

    }
}
