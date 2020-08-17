using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.DbEntities
{
    public class GlobalVariable:ICloneable
    {
        public int Id { get; set; }
        public uint Address { get; set; }
        public int Size { get; set; }
        public string Name { get; set; }
        public Type NetType { get; set; }
        public object CurrentValue { get; set; } = 0.0;
        public object Clone()
        {
            return new GlobalVariable
            {
                Id = this.Id,
                Address = this.Address,
                Size = this.Size,
                Name = this.Name,
                NetType = this.NetType
            };
        }
        public override bool Equals(object obj)
        {
            var gv = obj as GlobalVariable;

            return gv != null 
                && gv.Id == Id
                && string.Equals(gv.Name,Name)
                && gv.Size == Size
                && gv.NetType.Equals(NetType);

        }
        public override int GetHashCode()
        {
            int hash = 10;
            hash = hash + Id.GetHashCode();
            hash = hash + Name == null? 0:Name.GetHashCode();
            hash = hash + Address.GetHashCode();
            hash = hash + Size.GetHashCode();
            hash = hash + NetType.GetHashCode();

            return hash;
        }
    }
}

