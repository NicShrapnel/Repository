//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PracticingRepository
{
    using System;
    using System.Collections.Generic;
    
    public partial class WorkerType
    {
        public int WorkerTypeID { get; set; }
        public string WorkerTypeName { get; set; }
        public Nullable<int> MaterialID { get; set; }
    
        public virtual Materials Materials { get; set; }
    }
}
