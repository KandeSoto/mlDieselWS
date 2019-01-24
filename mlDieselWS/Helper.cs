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
        public const string AUTORIZACIONES_VENCIDAS = "4";
    }

    public partial class Configuration
    {
        public const string USERNAME = "112";
        public const string PASS = "333";
        public const string DEVICE = "000001";
        public const int TIME = 60000;
        public const string COMMENT = "Se realizo autorizacion de combustible desde el servicio con el viaje: ";
        public const int ExpirationDays = 3;        
    }

    public partial class ConfigurationTicketCar
    {        
        public const string EXTERNAL_VALID_USER_ID = "157";
        public const string EXTERNAL_VALID_PASSWORD = "PMCOM23243533EDV27SA25";
        public const string PROVIDED_TOKEN = "0Wl9d5diXMV6wjT8LTMR0YqfCeaVMOl4o3o6Xf+thotASepkb7F2wK9OAimAPJxCFFokgMfakqG/1gq8kFqk7NT2krR/Q52/OXHr1dZP0WWdq1Vfxus4iP2cqsJiD1IzoATpS34h1JDd0gtlfBxtyg==";
        public const string CLIENT_IP = "131.72.230.116";
        public const int PAGE_RECORDS = 1000;
        public const int PAG_NUMBER = 0;
        public const bool GET_ALL_RECORDS = false;
        public const bool ONLINE_TRX = false;
        public const int VECES_UTILIZADAS = 99;
    }

    public partial class Producto
    {
        public const string MAGNA = "1";
        public const string PREMIUM = "2";
        public const string DIESEL = "3";
        public const int MAGNA_TICKET = 106;
        public const int PREMIUM_TICKET = 108;
        public const int DIESEL_TICKET = 103;
    }

    public partial class StatusProcess
    {
        public const int Error = 0;
        public const int EnvioAutorizacion = 1;
        public const int EnvioTotal = 2;
        public const int SoloEjecutoProceso = 3;
        public const int EnvioAutorizacionErrorCancelar = 5;
        public const int EnvioTicketCar = 6;
    }

    public partial class NotaValeEstatus
    {
        public const int USADA = 11;
        public const int EN_ESPERA = 12;
        public const int ANULADA = 13;
        public const int VENCIDA = 14;
        public const int NO_DEFINIDO = 19;
    }

    public partial class NotaValeSubEstatus
    {
        public const int SUB_EN_ESPERA = 29;
        public const int SUB_EN_USO = 21;
        public const int SUB_PENDIENTE_ANULAR = 23;
        public const int SUB_ANULADA_CON_CONSUMO = 24;
        public const int SUB_ANULADA_SIN_CONSUMO = 25;
        public const int SUB_VENCIDA = 22;        
    }

    public partial class OrderStatusIdentification
    {
        public const int AGENDADO = 1;
        public const int ESPERANDO_PROCESAMIENTO = 2;
        public const int PROCESANDO = 3;
        public const int FINALIZADO_SUCESO = 4;
        public const int ANULACION_PENDIENTE = 5;
        public const int ANULADO = 6;
        public const int NO_ACEPTO = 7;
        public const int REGISTRO_SUCESO = 8;
        public const int FINALIZADO_PARCIALMENTE = 9;
    }

    public partial class TypeIdentification
    {
        public const int MONTO = 1;
        public const int LITROS = 2;
    }

    public partial class OperationIdentification
    {
        public const int CREATE = 0;
        public const int UPDATE = 1;
        public const int CANCEL = 2;
    }

    public partial class SolicitudDepositoStatus
    {
        public const int SOLICITADO = 0;
        public const int APROBADO = 1;
        public const int RECHAZADO = 2;
        public const int ELIMINADO = 3;
        public const int TERMINADO = 4;
    }

}
