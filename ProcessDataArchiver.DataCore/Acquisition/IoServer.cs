using CPDev.Public;
using ProcessDataArchiver.DataCore.Acquisition;
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.DbEntities;
using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;

namespace ProcessDataArchiver.DataCore.Acquisition
{
    public class IoServer
    {
        private IDictionary<int, IList<ITag>> groupedTags;
        private IDictionary<int, IList<IEvent>> groupedEvents;
        private IDictionary<double, IList<CyclicEvent>> cyclicEvents;

        private IDictionary<int, IList<GlobalVariable>> groupedGvs;

        private IList<Timer> todEventTimers;
        private IList<Timer> eventTimers;
        private IList<Timer> readDataTimers;
        private Timer MainTimer { get; set; }
        private int Counter { get; set; }
        private IEnumerable<int> TimeSpans { get; set; }
        private ConcurrentDictionary<int, byte> SpansToRead { get; } = new ConcurrentDictionary<int, byte>();
        private int TaskCycleMs { get; }

        private string programPath;
        private static IoServer server;
        private EntityContext cnx = EntityContext.GetContext();
        private System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
        public IDatabaseProvider DbProvider { get; set; }
        public ICPSim_Target Target { get; set; }
        public bool IsStarted { get; set; }
        public bool ProgramLoaded { get; set; }

        public event EventHandler StateChanged;
        public event EventHandler<string> ErrorOccured;
        public event EventHandler GlobalVariablesReaded;
        private IoServer(ArchvieProjectInfo proj)
        {
            groupedTags = new Dictionary<int, IList<ITag>>();
            groupedEvents = new Dictionary<int, IList<IEvent>>();
            cyclicEvents = new Dictionary<double, IList<CyclicEvent>>();
            groupedGvs = new Dictionary<int, IList<GlobalVariable>>();

            eventTimers = new List<Timer>();
            readDataTimers = new List<Timer>();
            todEventTimers = new List<Timer>();

            DbProvider = EntityContext.GetContext().DbProvider;
            Target = proj.DataSource;
            programPath = proj.CPDevProgramPath;
            TaskCycleMs = proj.TaskCycleMS;
        }
        public static IoServer GetIoServer(ArchvieProjectInfo proj)
        {
            server = new IoServer(proj);
            return server;
        }
        public static IoServer GetIoServer()
        {
            return server;
        }
        private void GroupEntities()
        {
            groupedTags.Clear();
            var tagGroups = from t in cnx.Tags
                            where t.ArchivingType != ArchivingType.Disabled
                            group t by ((int)(t.RefreshSpan.TotalMilliseconds / 1000));

            foreach (var t in tagGroups)
            {
                groupedTags.Add(t.Key, t.ToList());
            }

            groupedEvents.Clear();
            foreach (var item in cnx.Events.Where(e => e.EventType == EventType.Cyclic))
            {
                var cEvent = (item as CyclicEvent);
                cEvent.SetupEvent();
            }
            var groups = from e in cnx.Events
                         where (e.EventType != EventType.Cyclic ||
                         (e.EventType == EventType.Cyclic && ((e as CyclicEvent).EventCycleType == EventCycleType.Periodic)))
                         && e.Enabled
                         group e by ((int)(e.RefreshSpan.TotalMilliseconds / 1000));

            foreach (var g in groups)
            {
                groupedEvents.Add(g.Key, g.ToList());
            }

            var cEve = from e in cnx.Events
                       where e.EventType == EventType.Cyclic &&
                       (e as CyclicEvent).EventCycleType != EventCycleType.Periodic
                       select e as CyclicEvent;

            foreach (var item in cEve)
            {
                item.SetupEvent();
            }

            var todEve = from e in cEve
                         group e by e.RefreshSpan.TotalMilliseconds;

            foreach (var c in todEve)
            {
                cyclicEvents.Add(c.Key, c.ToList());
            }

        }
        private void PrepareTimers()
        {
            TimeSpans = groupedTags.Keys.Union(groupedEvents.Keys);

            MainTimer = new Timer(1000);
            MainTimer.AutoReset = true;
            MainTimer.Elapsed += MainTimer_Elapsed;

            foreach (var span in TimeSpans)
            {
                var gvSet = new HashSet<GlobalVariable>();
                if (groupedEvents.ContainsKey(span))
                {
                    var events = groupedEvents[span];

                    foreach (var ev in events)
                    {
                        if ((ev as DiscreteEvent) != null)
                        {
                            var dev = ev as DiscreteEvent;
                            if (dev.GlobalVariable != null)
                                gvSet.Add(dev.GlobalVariable);
                        }
                        else if ((ev as AnalogEvent) != null)
                        {
                            var aev = ev as AnalogEvent;
                            if (aev.GlobalVariable != null)
                                gvSet.Add(aev.GlobalVariable);
                        }
                    }
                }


                if (groupedTags.ContainsKey(span))
                {
                    var tags = groupedTags[span];
                    foreach (var tag in tags)
                    {
                        if (tag.GlobalVariable != null)
                            gvSet.Add(tag.GlobalVariable);
                    }
                }
                if (!groupedGvs.ContainsKey(span))
                    groupedGvs.Add(span, gvSet.ToList());
            }



            todEventTimers.Clear();
            foreach (var item in cyclicEvents)
            {
                foreach (var v in item.Value)
                {
                    v.SetupEvent();
                }
                Timer timer = new Timer(item.Key);
                timer.Elapsed += CyclicEventTimer_Elapsed;
                timer.AutoReset = true;
                todEventTimers.Add(timer);
            }

        }
        private void MainTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TimeSpans != null && TimeSpans.Count() > 0)
            {
                Counter++;
                SpansToRead.Clear();
                foreach (int span in TimeSpans)
                {
                    if (span != 0)
                    {
                        if (Counter % span == 0)
                        {
                            SpansToRead.AddOrUpdate(span, 0, (i, b) => b);
                        }
                    }
                }
                if (Counter == TimeSpans.Max())
                    Counter = 0;
            }
        }

        private void CyclicEventTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var tim = sender as Timer;
            double interval = tim.Interval;
            foreach (var item in cyclicEvents[interval])
            {
                if (item.InvokeAction())
                {
                    DbProvider.InsertEventsValueAsync(item);
                }
                tim.Stop();
                item.SetupEvent();
                tim.Interval = item.RefreshSpan.TotalMilliseconds;
                tim.Start();

            }

        }
        public void LoadProgram()
        {
            var crc = Target.GetProgramCRC();
            string xpath = System.IO.Path.ChangeExtension(programPath, "xcp");
            int x = Target.LoadProgram(xpath, UInt16.MaxValue);
            Target.SetTaskCycle((uint)TaskCycleMs, 0);
            Target.OnCycleEnd += Target_OnCycleEnd;
            Target.OnCodeError += Target_OnCodeError;

            ProgramLoaded = true;
        }
        public void LoadProgram(string path)
        {
            programPath = path;
            string xpath = System.IO.Path.ChangeExtension(path, "xcp");
            int x = Target.LoadProgram(xpath, UInt16.MaxValue);
            Target.SetTaskCycle((uint)TaskCycleMs, 0);
            Target.OnCycleEnd += Target_OnCycleEnd;
            Target.OnCodeError += Target_OnCodeError;
            var crc = Target.GetProgramCRC();
            ProgramLoaded = true;
        }
        private void Target_OnCodeError(string sErrorMessage)
        {
            server.Stop();
            ErrorOccured?.Invoke(this, sErrorMessage);
        }
        public void Start()
        {
            IsStarted = true;
            GroupEntities();
            PrepareTimers();
            MainTimer.Start();

            Task.Run(() => { Target.Run(0x41); });

            foreach (var timer in todEventTimers)
            {
                timer.Start();
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        private async void Target_OnCycleEnd()
        {
            if (SpansToRead.Count > 0)
            {
                foreach (var span in SpansToRead.Keys)
                {
                    if (groupedGvs.Keys.Contains(span))
                    {
                        await Task.Run(() =>
                        {
                            foreach (var gv in groupedGvs[span])
                            {
                                ReadValues(gv);
                            }

                        });
                        GlobalVariablesReaded?.Invoke(this, EventArgs.Empty);
                    }
                    await Task.Run(() =>
                    {
                        var tagsToSave = groupedTags.Where(k => SpansToRead.Keys.Contains(k.Key))
                            .SelectMany(k => k.Value).Where(t => t.CheckCondition());
                        if (tagsToSave != null && tagsToSave.Count() > 0)
                        {
                            DbProvider.InsertTagsValueAsync(tagsToSave.ToArray());
                            SpansToRead.Clear();
                        }
                    });

                    if (groupedEvents.Keys.Contains(span))
                    {
                        await Task.Run(() =>
                        {
                            foreach (var ev in groupedEvents[span])
                            {
                                if (ev.InvokeAction())
                                    DbProvider.InsertEventsValueAsync(ev);
                            }
                        });
                    }
                }
                SpansToRead.Clear();
            }
        }
        public void Stop()
        {
            if (MainTimer != null && MainTimer.Enabled)
                MainTimer.Stop();

            IsStarted = false;
            groupedEvents.Clear();
            groupedTags.Clear();
            groupedGvs.Clear();

            foreach (var item in todEventTimers)
            {
                item.Stop();
                item.Elapsed -= CyclicEventTimer_Elapsed;
                item.Dispose();
            }

            todEventTimers.Clear();
            Target.Stop();

            Target.ResetState();


            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        private void ReadValues(GlobalVariable globVar)
        {
            //var globVar = tag.GlobalVariable;

            if (globVar.NetType == typeof(Boolean))
                globVar.CurrentValue = Target.GetDataByte(globVar.Address) == 1 ? true : false;

            else if (globVar.NetType == typeof(sbyte))
                globVar.CurrentValue = (sbyte)Target.GetDataByte(globVar.Address);

            else if (globVar.NetType == typeof(short))
                globVar.CurrentValue = (short)Target.GetDataWord(globVar.Address);

            else if (globVar.NetType == typeof(int))
                globVar.CurrentValue = (int)Target.GetDataDWord(globVar.Address);

            else if (globVar.NetType == typeof(long))
                globVar.CurrentValue = (long)Target.GetDataLWord(globVar.Address);

            else if (globVar.NetType == typeof(float))
                globVar.CurrentValue = (float)Target.GetDataReal(globVar.Address);

            else if (globVar.NetType == typeof(double))
                globVar.CurrentValue = (double)Target.GetDataLReal(globVar.Address);

            else if (globVar.NetType == typeof(byte))
                globVar.CurrentValue = (byte)Target.GetDataByte(globVar.Address);

            else if (globVar.NetType == typeof(ushort))
                globVar.CurrentValue = Target.GetDataWord(globVar.Address);

            else if (globVar.NetType == typeof(uint))
                globVar.CurrentValue = Target.GetDataDWord(globVar.Address);

            else if (globVar.NetType == typeof(ulong))
                globVar.CurrentValue = Target.GetDataLWord(globVar.Address);
        }
    }


}
