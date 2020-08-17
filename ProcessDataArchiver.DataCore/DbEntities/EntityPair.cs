using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public class EntityPair<T> where T:ICloneable
    {
        public T Current { get; set; }
        public T Original { get; set; }
        public EntityState State { get; set; }
        public EntityChangesInfo GetChanges()
        {
            if (Original == null && Current != null)
            {
                var chOrTuple = CheckChanges();
                return new EntityChangesInfo(Current, State, chOrTuple.Item1,chOrTuple.Item2);
            }
            else if (Original != null && State == EntityState.Removed)
            {
                var chOrTuple = CheckChanges();
                return new EntityChangesInfo(Original, State, chOrTuple.Item1,chOrTuple.Item2);
            }
            else if (Original != null && Current != null && !Original.Equals(Current))//
            {
                var chOrTuple = CheckChanges();
                return new EntityChangesInfo(Current, EntityState.Modified, chOrTuple.Item1,chOrTuple.Item2);                
            }
            return null;
        }
        private Tuple<Dictionary<string, object>, Dictionary<string, object>> CheckChanges()
        {
            Dictionary<string, object> changes = new Dictionary<string, object>();
            Dictionary<string, object> original = new Dictionary<string, object>();
            if (Original == null && Current != null)
            {
                var props = Current.GetType().GetProperties()
                    .Where(p => (p.PropertyType.IsValueType || p.PropertyType == typeof(string)));
                
                foreach (var prop in props)
                {
                    changes.Add(prop.Name, prop.GetValue(Current));
                }

            }
            else if (Original != null && Current != null)
            {
                var props = Original.GetType().GetProperties()
                .Where(p => (p.PropertyType.IsValueType || p.PropertyType == typeof(string)));
                foreach (var prop in props)
                {
                    var valOr = prop.GetValue(Original);
                    var valCur = prop.GetValue(Current);

                    if (valOr != null && !valOr.Equals(valCur) || valCur != null && !valCur.Equals(valOr))
                    {
                        changes.Add(prop.Name, valCur);
                        original.Add(prop.Name, valOr);
                    }
                }
            }

            var tuple = Tuple.Create<Dictionary<string, object>, Dictionary<string, object>>(changes.Count>0?changes:null,
                original.Count>0?original:null);

            return tuple;
         
        }
        public void SaveChanges()
        {
            Original = (T)Current.Clone();
            State = EntityState.Unchanged;
        }
        public void RestoreChanges()
        {
            Current = (T)Original.Clone();
            State = EntityState.Unchanged;
        }
    }
}
