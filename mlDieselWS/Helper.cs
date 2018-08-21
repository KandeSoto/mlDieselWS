using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mlDieselWS
{
    public partial class ServiceState
    {
        public const string SERVICE_SUCCESS = "0";
        public const string SERVICE_NULL = "-10";        
    }

    public partial class AuthorizationStatus
    {
        public const string AUTORIZACIONES_VALIDAS = "0";
        public const string AUTORIZACIONES_UTILIZADAS_TOTALIDAD = "1";
        public const string AUTORIZACIONES_UTILIZADAS_PARCIALMENTE = "2";
        public const string AUTORIZACIONES_CANCELADAS = "3";
    }

    public partial class Configuration
    {
        public const string USERNAME = "112";
        public const string PASS = "333";
        public const string DEVICE = "000001";
        public const int TIME = 60000;
        public const string COMMENT = "Se realizo autorizacion de combustible desdes el sistema desde el folio: ";
        public const int ExpirationDays = 7;
    }

    public partial class Producto
    {
        public const string MAGNA = "1";
        public const string PREMIUM = "2";
        public const string DIESEL = "3";
    }

    public partial class StatusProcess
    {
        public const int Error = 0;
        public const int EnvioAutorizacion = 1;
        public const int EnvioTotal = 2;
        public const int SoloEjecutoProceso = 3;
        public const int EnvioAutorizacionErrorCancelar = 5;
    }
    
}
