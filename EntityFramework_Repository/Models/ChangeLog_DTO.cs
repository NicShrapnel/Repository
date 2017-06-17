using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework_Repository.Models
{
    internal class ChangeLog_Entry
    {
        private static long ID_Counter;
        internal static long GetID_Counter()
        {
            return ID_Counter;
        }
        internal static void SetID_Counter(long id)
        {
            ID_Counter = ++id;
        }

        public ChangeLog_Entry()
        {
            if (ID_Counter == 0)
                ID_Counter++;

            _Entry_ID = ID_Counter++;
        }

        private long _Entry_ID;
        public long Entry_ID { get { return _Entry_ID; } }
        public string FQAType { get; set; }
        public string TableUpdated { get; set; }
        public string PrimaryKey { get; set; }
        public string ColumnUpdated { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
        public string Operation { get; set; }
        public DateTime? CommittedAt { get; set; }
        public DateTime? ErrorAt { get; set; }
        public string ErrorNotes { get; set; }
    }
    public class Change_Entry
    {
        public long Entry_ID { get; set; }
        internal string FQAType { get; set; }
        public string TableUpdated { get; set; }
        public string PrimaryKey { get; set; }
        public string ColumnUpdated { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
        public string Operation { get; set; }
        public DateTime? CommittedAt { get; set; }
        public DateTime? ErrorAt { get; set; }
        public string ErrorNotes { get; set; }
    }
}
