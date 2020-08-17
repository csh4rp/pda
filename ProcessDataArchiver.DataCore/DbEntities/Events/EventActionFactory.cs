
using ProcessDataArchiver.DataCore.Database.DbProviders;
using ProcessDataArchiver.DataCore.DbEntities.Tags;
using ProcessDataArchiver.DataCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProcessDataArchiver.DataCore.DbEntities.Events
{
    public class EventActionFactory
    {
        public static Action CreateAction(IEvent ev)
        {
            EntityContext cnx = EntityContext.GetContext();

            if (ev.EventActionType == EventActionType.Snapshot)
            {
                var tags = new List<ITag>();

                var root = XDocument.Parse(ev.ActionText);
                var snap = root.Descendants("snapshot").FirstOrDefault();
                var tag = snap.Descendants("tag");
                foreach (var item in tag)
                {
                    var name = item.Descendants("name").FirstOrDefault().Value;
                    var archive = item.Descendants("archivename").FirstOrDefault().Value;

                    var tagToAdd = cnx.Tags.Where(t => t.Name.Equals(name)
                    && t.TagArchive.Name.Equals(archive));
                    tags.AddRange(tagToAdd);
                }
                return () =>
                {
                    cnx.DbProvider.InsertEventSnapshotAsync(ev, tags.ToArray());
                };
            }
            else if (ev.EventActionType == EventActionType.Sql)
            {
                return async () =>
                {
                    string cmd = ev.ActionText;
                    await cnx.DbProvider.ExecuteNonQueryAsync(cmd);
                };
            }
            else if (ev.EventActionType == EventActionType.Email)
            {
                return () =>
                {
                    string fromEmail = null;
                    var root = XDocument.Parse(ev.ActionText);
                    var email = root.Descendants("email").FirstOrDefault();
                    var from = email.Descendants("adresses").FirstOrDefault();
                    if (from != null)
                    {
                        fromEmail = from.Value;
                    }
                    string addresses = email.Descendants("addresses").FirstOrDefault().Value;
                    string subject = email.Descendants("subject").FirstOrDefault().Value;
                    string body = email.Descendants("body").FirstOrDefault().Value;

                    EmailProvider eProv = new EmailProvider
                    {
                        From = fromEmail,
                        Addresses = addresses,
                        Subject = subject,
                        Message = body
                    };
                    eProv.SendMailAsync();
                };

            }
            else if (ev.EventActionType == EventActionType.Summary)
            {
                return () =>
                {
                    EntityContext.GetContext().DbProvider.InsertSummaryAsync(ev);
                };
            }
            else
                return null;


        }


    }
}
