using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mlDieselWS
{
   public class EnergexPartialAuthorizationList
    {
        public string idTxnPurchaseCashCard { get; set; }
        public DateTime? Date { get; set; }
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
