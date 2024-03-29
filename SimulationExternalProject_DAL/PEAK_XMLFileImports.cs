//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SimulationExternalProject_DAL
{
    using System;
    using System.Collections.Generic;
    
    public partial class PEAK_XMLFileImports
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PEAK_XMLFileImports()
        {
            this.PEAK_XMLImportLog = new HashSet<PEAK_XMLImportLog>();
            this.PEAK_XMLImportAttachmentLog = new HashSet<PEAK_XMLImportAttachmentLog>();
        }
    
        public int FileImportID { get; set; }
        public string PeakMatterID { get; set; }
        public string MatterID { get; set; }
        public string XMLFileType { get; set; }
        public string FilePath { get; set; }
        public Nullable<System.DateTime> ImportedAt { get; set; }
        public Nullable<System.DateTime> ErrorAt { get; set; }
        public string SystemNotes { get; set; }
        public string Product { get; set; }
        public string ProductClass { get; set; }
        public string UpdatedBy { get; set; }
        public Nullable<int> RetryCounter { get; set; }
        public Nullable<System.DateTime> LastAttemptMade { get; set; }
        public Nullable<System.DateTime> SuccessfullyDeserialized { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PEAK_XMLImportLog> PEAK_XMLImportLog { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PEAK_XMLImportAttachmentLog> PEAK_XMLImportAttachmentLog { get; set; }
    }
}
