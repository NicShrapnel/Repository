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
    
    public partial class PEAK_XMLImportAttachmentLog
    {
        public long ImportAttachmentID { get; set; }
        public int FileImportID { get; set; }
        public string DocType { get; set; }
        public string Description { get; set; }
        public string AttachmentFileType { get; set; }
        public string FilePath { get; set; }
        public Nullable<System.DateTime> ImportedAt { get; set; }
        public Nullable<System.DateTime> ErrorAt { get; set; }
        public string SystemNotes { get; set; }
    
        public virtual PEAK_XMLFileImports PEAK_XMLFileImports { get; set; }
    }
}
