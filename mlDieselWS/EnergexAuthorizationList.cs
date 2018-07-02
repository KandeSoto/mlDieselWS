using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mlDieselWS
{
    public class EnergexAuthorizationList
    {
        public string idTxnEnquiryMaxLoad { get; set; }
        public DateTime? Date { get; set; }
        public string fkIdMerchant { get; set; }
        public string group_member { get; set; }
        public string orden { get; set; }
        public string MaxLitres { get; set; }
        public string vehicle { get; set; }
        public string comment { get; set; }
    }
}
