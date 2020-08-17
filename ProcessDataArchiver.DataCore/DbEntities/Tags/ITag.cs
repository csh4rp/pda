using System;

namespace ProcessDataArchiver.DataCore.DbEntities.Tags
{
    public interface ITag : ICloneable
    {
        int Id { get; set; }
        string Name { get; set; }
        int GlobalVariableId { get; set; }
        GlobalVariable GlobalVariable {get;set;}
        int TagArchiveId { get; set; }
        TagArchive TagArchive { get; set; }
        TagType TagType { get; }
        object LastSavedValue { get; }
        object CurrentValue { get; }
        TimeSpan RefreshSpan { get; set; }
        ArchivingType ArchivingType { get; set; }
        string Comment { get; set; }
        DateTime LastChanged { get; set; }
        bool CheckCondition();
        void Copy(ITag tag);     
    }


}
