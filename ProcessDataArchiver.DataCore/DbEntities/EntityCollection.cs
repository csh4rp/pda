using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public class EntityCollection<T> : IList<T> where T:ICloneable
    {
        private List<EntityPair<T>> entities = new List<EntityPair<T>>();
        private List<EntityPair<T>> added = new List<EntityPair<T>>();
        private List<EntityPair<T>> removed = new List<EntityPair<T>>();
        private EntityChangeTracker tracker;

        public event EventHandler CollectionChanged;
        public EntityCollection(EntityChangeTracker t)
        {
            tracker = t;
        }
        public EntityCollection(EntityChangeTracker t, IEnumerable<T> data)
        {
            tracker = t;
            foreach (var item in data)
            {
                entities.Add(new EntityPair<T> { Original = item, State = EntityState.Unchanged });
            }
        }
        public int Count
        {
            get
            {
                return entities.Count;
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        public T this[int index]
        {
            get
            {
                return entities[index].Current;
            }

            set
            {
                entities[index].Current = value;
            }
        }
        public void Add(T item)
        {            
            var toAdd = new EntityPair<T> { Current = item, State = EntityState.Added };
            entities.Add(toAdd);
            added.Add(toAdd);
            if (removed.Contains(toAdd))
            {
                removed.Remove(toAdd);
            }
            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Clear()
        {
            entities.Clear();
            added.Clear();
            removed.Clear();
            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        public bool Contains(T item)
        {
            return entities.Select(e => e.Current).Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            entities.Select(e => e.Current).ToList().CopyTo(array, arrayIndex);
        }
        public IEnumerator<T> GetEnumerator()
        {
            return entities.Select(e => e.Current).GetEnumerator();
        }
        public bool Remove(T item)
        {
            var toRemove = entities.Where(e => e.Current.Equals(item)).FirstOrDefault();
            if (toRemove != null)
            {
                toRemove.State = EntityState.Removed;
                entities.Remove(toRemove);
                
                if (added.Contains(toRemove))
                {
                    added.Remove(toRemove);
                }
                else
                {
                    removed.Add(toRemove);
                }
                CollectionChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return entities.Select(e => e.Current).GetEnumerator();
        }
        public int IndexOf(T item)
        {
            return entities.Select(e => e.Current).ToList().IndexOf(item);
        }
        public void Insert(int index, T item)
        {
            var toInsert = new EntityPair<T> { Current = item, State = EntityState.Added };
            entities.Insert(index, toInsert);
            added.Add(toInsert);
            if (removed.Contains(toInsert))
            {
                removed.Remove(toInsert);
            }
            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        public void RemoveAt(int index)
        {
            var toRemove = entities.ToList()[index];
            entities.Remove(toRemove);           
            if(added.Contains(toRemove))
            {
                added.Remove(toRemove);
            }
            else
            {
                removed.Add(toRemove);
            }
            CollectionChanged?.Invoke(this, EventArgs.Empty);
        }
        public void SaveChanges()
        {
            removed.Clear();
            added.Clear();
            foreach (var item in entities)
            {
                item.SaveChanges();
            }
        }
        public void DetectChanges()
        {
            foreach (var item in entities)
            {
                var changes = item.GetChanges();
                if (changes != null)
                    tracker.Changes.Add(changes);
            }
            foreach (var item in removed)
            {
                item.State = EntityState.Removed;
                var changes = item.GetChanges();
                if(changes!=null)
                    tracker.Changes.Add(changes);
            }
        }
        public void RestoreChanges()
        {
            foreach (var item in removed)
            {
                entities.Add(item);
            }
            foreach (var item in added)
            {
                entities.Remove(item);
            }
            foreach (var item in entities)
            {
                item.RestoreChanges();
            }
        }
        public void LoadData(IEnumerable<T> data)
        {
            foreach (var item in data)
            {
                entities.Add(new EntityPair<T> { Original = item,Current = (T)item.Clone(),
                    State = EntityState.Unchanged });
            }
        }
    }
}

