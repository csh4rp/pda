using ProcessDataArchiver.DataCore.Database.DbProviders;

using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public class EntityContext
    {
        private static EntityContext cnx;

        public EntityCollection<TagArchive> TagArchives { get; }
        public EntityCollection<ITag> Tags { get; }
        public EntityCollection<GlobalVariable> GlobalVariables { get;  }
        public EntityCollection<IEvent> Events { get; }
        public EntityChangeTracker ChangeTracker { get;  }
        public IDatabaseProvider DbProvider { get; set; }
        public static IDictionary<string, TimeSpan> CycleTimes { get; private set; }
        public event EventHandler CollectionsChanged;

        private EntityContext()
        {
            ChangeTracker = new EntityChangeTracker();
            TagArchives = new EntityCollection<TagArchive>(ChangeTracker);
            Events = new EntityCollection<IEvent>(ChangeTracker);
            Tags = new EntityCollection<ITag>(ChangeTracker);
            GlobalVariables = new EntityCollection<GlobalVariable>(ChangeTracker);

            TagArchives.CollectionChanged += Entity_CollectionChanged;
            Events.CollectionChanged += Entity_CollectionChanged;
            Tags.CollectionChanged += Entity_CollectionChanged;
            GlobalVariables.CollectionChanged += Entity_CollectionChanged;

            SetCycleTimes();
        }
        private void Entity_CollectionChanged(object sender, EventArgs e)
        {
            //SetReferences();
            foreach (var item in Events)
            {
                if (item is DiscreteEvent)
                {
                    var dev = item as DiscreteEvent;
                    var itemsToAdd = from g in GlobalVariables
                                     where g.Id.Equals(dev.GlobalVariableId)
                                     select g;
                    dev.GlobalVariable = itemsToAdd.FirstOrDefault();
                }
                else if (item is AnalogEvent)
                {
                    var aev = item as AnalogEvent;
                    var itemsToAdd = from g in GlobalVariables
                                     where g.Id.Equals(aev.GlobalVariableId)
                                     select g;
                    aev.GlobalVariable = itemsToAdd.FirstOrDefault();
                }
            }
            CollectionsChanged?.Invoke(this, EventArgs.Empty);
        }
        public static async Task<EntityContext> GetContextAsync(IDatabaseProvider prov = null)
        {
            if(cnx == null && prov!=null)
            {
                cnx = new EntityContext() { DbProvider = prov };
                await cnx.PrepareAsync();
            }

            return cnx;   
        }   
        public static EntityContext GetContext(IDatabaseProvider prov = null)
        {
            if (cnx == null && prov != null)
            {
                cnx = new EntityContext() { DbProvider = prov };
                Task t = cnx.PrepareAsync();
                t.Wait();
            }

            return cnx;
        }
        public async Task SaveChangesAsync()
        {
            TagArchives.DetectChanges();
            if (ChangeTracker.Changes.Count > 0)
            {
                await DbProvider.UpdateEntitiesAsync(ChangeTracker.Changes.ToArray());
                ChangeTracker.Changes.Clear();
                TagArchives.SaveChanges();
            }

            SetTagsArchiveIds();
            Tags.DetectChanges();
            if (ChangeTracker.Changes.Count > 0)
            {
                await DbProvider.UpdateEntitiesAsync(ChangeTracker.Changes.ToArray());
                ChangeTracker.Changes.Clear();
                Tags.SaveChanges();
            }


            Events.DetectChanges();
            if (ChangeTracker.Changes.Count > 0)
            {
                await DbProvider.UpdateEntitiesAsync(ChangeTracker.Changes.ToArray());
                ChangeTracker.Changes.Clear();
                Events.SaveChanges();
            }

            GlobalVariables.DetectChanges();
            if (ChangeTracker.Changes.Count > 0)
            {
                await DbProvider.UpdateEntitiesAsync(ChangeTracker.Changes.ToArray());
                ChangeTracker.Changes.Clear();
                GlobalVariables.SaveChanges();
            }
            SetReferences();

        }
        public void RestoreChanges()
        {
            Tags.RestoreChanges();
            TagArchives.RestoreChanges();
            Events.RestoreChanges();
            GlobalVariables.RestoreChanges();
        }
        public void SetReferences()
        {
            foreach (var item in TagArchives)
            {
                var itemsToAdd = from t in Tags
                                 where t.TagArchiveId.Equals(item.Id)
                                 select t;
                item.Tags = itemsToAdd;
            }

            foreach (var item in Tags)
            {
                var archive = from t in TagArchives
                              where t.Id.Equals(item.TagArchiveId)
                              select t;
                item.TagArchive = archive.FirstOrDefault();
            }

            foreach (var item in Tags)
            {
                var itemsToAdd = from g in GlobalVariables
                                 where g.Id.Equals(item.GlobalVariableId)
                                 select g;

                item.GlobalVariable = itemsToAdd.FirstOrDefault();
            }

            foreach (var item in Events)
            {
                if(item is DiscreteEvent)
                {
                    var dev = item as DiscreteEvent;
                    var itemsToAdd = from g in GlobalVariables
                                     where g.Id.Equals(dev.GlobalVariableId)
                                     select g;
                    dev.GlobalVariable = itemsToAdd.FirstOrDefault();
                }
                else if (item is AnalogEvent)
                {
                    var aev = item as AnalogEvent;
                    var itemsToAdd = from g in GlobalVariables
                                     where g.Id.Equals(aev.GlobalVariableId)
                                     select g;
                    aev.GlobalVariable = itemsToAdd.FirstOrDefault();
                }
            }
            ////////
            //await SaveChangesAsync();
        }
        public async Task LoadDataAsync()
        {
            var archives = await DbProvider.GetArchivesAsync();
            TagArchives.LoadData(archives);

            var tags = await DbProvider.GetTagsAsync();
            Tags.LoadData(tags);





            foreach (var item in TagArchives)
            {
                var itemsToAdd = from t in Tags
                                 where t.TagArchiveId.Equals(item.Id)
                                 select t;
                item.Tags = itemsToAdd;
            }

            foreach (var item in Tags)
            {
                var archive = from t in TagArchives
                              where t.Id.Equals(item.TagArchiveId)
                              select t;
                item.TagArchive = archive.FirstOrDefault();
            }












            var events = await DbProvider.GetEventsAsync();
            Events.LoadData(events);

            var gvars = await DbProvider.GetGlobalVariablesAsync();
            GlobalVariables.LoadData(gvars);

            SetReferences();
        }
        public async Task PrepareAsync()
        {
            bool exists = await DbProvider.CheckSchemaExists();
            if (exists)
                await LoadDataAsync();
            else
                await DbProvider.CreateDbSchemaAsync();
        }
        private void SetTagsArchiveIds()
        {
            var tags = Tags.Where(t => t.TagArchiveId == 0).ToList();
            tags.ForEach(t => t.TagArchiveId = t.TagArchive.Id);
        }
        private static void SetCycleTimes()
        {
            CycleTimes = new Dictionary<string, TimeSpan>();

            CycleTimes.Add("1 sekunda", new TimeSpan(0, 0, 1));
            CycleTimes.Add("2 sekundy", new TimeSpan(0, 0, 2));
            CycleTimes.Add("5 sekund", new TimeSpan(0, 0, 5));
            CycleTimes.Add("10 sekund", new TimeSpan(0, 0, 10));
            CycleTimes.Add("15 sekund", new TimeSpan(0, 0, 15));
            CycleTimes.Add("20 sekund", new TimeSpan(0, 0, 20));
            CycleTimes.Add("30 sekund", new TimeSpan(0, 0, 30));
            CycleTimes.Add("1 minuta", new TimeSpan(0, 1, 0));
            CycleTimes.Add("2 minuty", new TimeSpan(0, 2, 0));
            CycleTimes.Add("5 minut", new TimeSpan(0, 5, 0));
            CycleTimes.Add("10 minut", new TimeSpan(0, 10, 0));
            CycleTimes.Add("15 minut", new TimeSpan(0, 15, 0));
            CycleTimes.Add("20 minut", new TimeSpan(0, 20, 0));
            CycleTimes.Add("30 minut", new TimeSpan(0, 30, 0));
            CycleTimes.Add("1 godzina", new TimeSpan(1, 0, 0));
        }

    }
}
