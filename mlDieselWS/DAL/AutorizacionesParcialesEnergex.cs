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
    
    public partial class AutorizacionesParcialesEnergex
    {
        public int AutorizacionesParcialesEnergexId { get; set; }
        public string idTxnPurchaseCashCard { get; set; }
        public Nullable<System.DateTime> Date { get; set; }
        public string fkIdMerchant { get; set; }
        public string fkIdTerminal { get; set; }
        public string fkIdProduct { get; set; }
        public string Amount { get; set; }
        public string LicensePlate { get; set; }
        public string Litres { get; set; }
        public string Kms { get; set; }
        public string ProductUnitPrice { get; set; }
        public string AuthNum { get; set; }
        public string orden { get; set; }
    }
}
