using ProcessDataArchiver.DataCore.Database.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public class ConnectionSettings:ICloneable
    {
        public string DataSource { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        public string Service { get; set; }
        public object Additional { get; set; }

        public object Clone()
        {
            return new ConnectionSettings
            {
                DataSource = this.DataSource,
                Port = this.Port,
                User = this.User,
                Password = this.Password,
                Database = this.Database,
                Service = this.Service,
                Additional = this.Additional
        };
        }
    }
}
