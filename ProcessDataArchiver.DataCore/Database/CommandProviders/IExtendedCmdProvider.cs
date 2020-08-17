using ProcessDataArchiver.DataCore.DbEntities.Events;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessDataArchiver.DataCore.Database.CommandProviders
{
    public interface IExtendedCmdProvider:ICommandProvider
    {
        IEnumerable<string> CreateArchiveCmd(TagArchive archive);// bo triggery
        string InsertTagValueCmd(TagArchive archive, IEnumerable<ITag> tags);
        string InsertSummaryCmd(IEvent archiveEvent, ITag tag, string action, DateTime fd);
        string ChangeNameCmd(string table, string oldName, string newName, Type t);
        string ChangeDtCmd(ITag tag, Type newType);
        string GetUpdateCmd(IDictionary<string, object> values, Type type);
        string GetUpdateCmd(IDictionary<string, object> values, Type baseType, Type derrivedType);
    }
}
