//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace mlDieselWS.DAL
{
    using System;
    using System.Collections.Generic;
    
    public partial class AutorizacionesEnergex
    {
        public int AutorizacionesEnergexId { get; set; }
        public string idTxnEnquiryMaxLoad { get; set; }
        public Nullable<System.DateTime> Date { get; set; }
        public string fkIdMerchant { get; set; }
        public string group_member { get; set; }
        public string orden { get; set; }
        public string MaxLitres { get; set; }
        public string vehicle { get; set; }
        public string comment { get; set; }
        public string Estatus { get; set; }
    }
}
