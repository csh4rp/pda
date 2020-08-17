using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.DbProviders
{
    public class ConnectionException:Exception
    {
        public ConnectionException()
        {

        }

        public ConnectionException(string msg):base(msg)
        {
            
        }
    }
}
