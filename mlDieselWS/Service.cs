using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using mlDieselWS.EnergexService;
using System.Timers;
using System.Text.RegularExpressions;
using System.Configuration;

namespace mlDieselWS
{
    public partial class Service : ServiceBase
    {
        private SP4GLwsService EnergexService;
        private Timer Process;
        private string TimeAppConfig = ConfigurationManager.AppSettings["Tiempo"];

        public Service()
        {                     
            InitializeComponent();
            Process = new Timer();
            EnergexService = new SP4GLwsService();           
           // Process.Interval = Configuration.TIME * Convert.ToInt32(TimeAppConfig);
            Process.Interval = Configuration.TIME * 0.1;
            Process.Elapsed += Process_Elapsed;
        }

        public void Debugger()
        {
            Process.Enabled = true;
            Process.Start();
            EventLog.WriteEntry("Inicio el servicio de combustibles.", EventLogEntryType.Information);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Process.Enabled = true;
                Process.Start();
                EventLog.WriteEntry("Se inicia el proceso de los Servicios.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {

                throw ex;
            }            
        }

        protected override void OnStop()
        {
            Process.Stop();
            EventLog.WriteEntry("Se detubo el Servicio de Combustibles", EventLogEntryType.Information);
            EnviarCorreoError("Se detubo el Servicio de Combustibles inesperada por algun error", "Servicio en General");
        }

        private void Process_Elapsed(object sender, ElapsedEventArgs e)
        {
            Process.Enabled = false;
            EventLog.WriteEntry("Inicio el servicio de combustibles.", EventLogEntryType.Information);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
            EventLog.WriteEntry("Termino el servicio de combustibles.", EventLogEntryType.Information);
            Process.Enabled = true;
        }

        private void ExecuteProcess(string Estatus)
        {
            EnergexProcess(Estatus);

            if (Estatus == AuthorizationStatus.AUTORIZACIONES_VALIDAS || Estatus == AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE)
                TickeCarProcess(Estatus);

            if (Estatus == AuthorizationStatus.AUTORIZACIONES_VENCIDAS)
                DispensarioProcess();
        }

        public void LogStatusProcess(int Result, string Estatus)
        {
            switch (Result)
            {
                case StatusProcess.INICIOPROCESO:
                    EventLog.WriteEntry("Solo se inicio el proceso sin modificaciones de autorizaciones "+Estatus+".", EventLogEntryType.Information);
                    break;
                case StatusProcess.EJECUTOPROCESO:                    
                    EventLog.WriteEntry("Se ejecuto el proceso correctamente de autorizaciones " + Estatus + ".", EventLogEntryType.Information);
                    break;                
                case StatusProcess.ERROR:
                    EventLog.WriteEntry("Se detecto un error en el proceso de autorizaciones " + Estatus + ".", EventLogEntryType.Error);                    
                    break;
            }
        }

        private void EnergexProcess(string Estatus)
        {
            try
            {
                EnergexBLL Ejecution = new EnergexBLL();

                DateTime DateStart = Ejecution.GetStarDate();                    
                DateTime DateEnd = DateTime.Now.AddHours(2);
                string Parametro = "";

                if (Estatus != AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE)
                    Parametro = "com.spinvent.gascard.dbobj.Txnenquirymaxload;listado_autorizacion;Cliente;" + Configuration.USERNAME + ";Contraseña;" + Configuration.PASS + ";FechaInicial; " + String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateStart) + "; FechaFinal;" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateEnd) + "; Estatus;" + Estatus + "; ";
                else
                    Parametro = "com.spinvent.gascard.dbobj.Txnpurchasecashcard;listado_consumos;Cliente;" + Configuration.USERNAME + ";Contraseña;" + Configuration.PASS + ";FechaInicial; " + String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateStart) + "; FechaFinal;" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateEnd) + "; ";

                var Response = EnergexService.executeProcedureDevice(Configuration.USERNAME, Configuration.PASS, Configuration.DEVICE, Parametro);

                if (Response != string.Empty)
                {
                    var HasError = Response.Split(';');
                    if (HasError[0] == ServiceState.SERVICE_SUCCESS)
                    {
                        string[] List = Regex.Split(Response, "\n");
                        List<EnergexAuthorizationList> ObjList = new List<EnergexAuthorizationList>();
                        List<EnergexPartialAuthorizationList> ObjPartialList = new List<EnergexPartialAuthorizationList>();

                        if (Estatus != AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE)
                        {
                            foreach (var item in List)
                            {
                                var Obj = GetEnergexClass(item);
                                ObjList.Add(Obj);
                            }
                        }
                        else
                        {
                            foreach (var item in List)
                            {
                                var Obj = GetEnergexPartialClass(item);
                                ObjPartialList.Add(Obj);
                            }
                        }

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD)
                        {
                            int Result = Ejecution.TotalAuthorizationProcess(ObjList);
                            LogStatusProcess(Result, "Totales");
                        }

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE)
                        {
                            int Result = Ejecution.PartialAuthorizationProcess(ObjPartialList);
                            LogStatusProcess(Result, "Parciales");
                        }                                            

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_CANCELADAS)
                        {
                            int Result = Ejecution.CanceledAuthorizationProcess(ObjList);
                            LogStatusProcess(Result, "Canceladas");
                        }                        

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_VENCIDAS)
                        {
                            int Result = Ejecution.VencidasAuthorizationProcess(ObjList);
                            LogStatusProcess(Result, "Vencidas");
                        }

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_VALIDAS)
                        {
                            int Result = Ejecution.ValidateAuthorizationProcess(ObjList);
                            LogStatusProcess(Result, "Validas");
                        }

                    }
                    else if (HasError[0] == ServiceState.SERVICE_NULL)
                        EventLog.WriteEntry("El servicio contiene un listado vacio", EventLogEntryType.Information);
                    else
                        EventLog.WriteEntry("Hay un error con el servicio energex", EventLogEntryType.Error);
                }
                else
                    EventLog.WriteEntry("No se obtuvo una respuesa del servicio", EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ex.Message, EventLogEntryType.Error);
                Process.Stop();
            }
        }

        private void TickeCarProcess(string Estatus)
        {
            try
            {
                TicketCarBLL Ejecution = new TicketCarBLL();

                int Result = Ejecution.TicketCarProcess(Estatus);
                LogStatusProcess(Result, "TicketCar");               

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void DispensarioProcess()
        {
            try
            {
                DispensarioBLL Ejecution = new DispensarioBLL();

                int Result = Ejecution.DispensarioProcess();
                LogStatusProcess(Result, "Dispensario");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void EnviarCorreoError(string Error, string TipoAutorizacion)
        {
            Mail email = new Mail();

            string Subject = "Alerta Error Servicio Combustible";
            string destinatario = ConfigurationManager.AppSettings["EmailDestino"];

            string msj = "<h2>Alerta se detecto un error en el Servicio de Combustibles</h2>"
                       + "<p>En el proceso: " + TipoAutorizacion + "</p>"
                       + "<p>Error: " + Error + "</p>"
                       + "<p>Fecha: " + DateTime.Now.ToString() + "</p>";

            bool envio = email.SendMail(destinatario, Subject, msj);
        }

        public EnergexAuthorizationList GetEnergexClass(string fila)
        {
            var registro = fila.Split(';');
            var obj = new EnergexAuthorizationList();
            int arrayIndex = 0;

            foreach (var item2 in registro)
            {
                switch (item2)
                {
                    case "idTxnEnquiryMaxLoad":
                        obj.idTxnEnquiryMaxLoad = registro[arrayIndex + 1];
                        break;
                    case "Date":
                        string date = registro[arrayIndex + 1];
                        if (date != string.Empty)
                            obj.Date = DateTime.Parse(registro[arrayIndex + 1]);
                        else
                            obj.Date = null;
                        break;
                    case "fkIdMerchant":
                        obj.fkIdMerchant = registro[arrayIndex + 1];
                        break;
                    case "group_member":
                        obj.group_member = registro[arrayIndex + 1];
                        break;
                    case "orden":
                        obj.orden = registro[arrayIndex + 1];
                        break;
                    case "MaxLitres":
                        obj.MaxLitres = registro[arrayIndex + 1];
                        break;
                    case "vehicle":
                        obj.vehicle = registro[arrayIndex + 1];
                        break;
                    case "comment":
                        obj.comment = registro[arrayIndex + 1];
                        break;
                    default:
                        break;
                }
                arrayIndex++;
            }
            return obj;
        }

        public EnergexPartialAuthorizationList GetEnergexPartialClass(string fila)
        {
            var registro = fila.Split(';');
            var obj = new EnergexPartialAuthorizationList();
            int arrayIndex = 0;

            foreach (var item2 in registro)
            {
                switch (item2)
                {
                    case "idTxnPurchaseCashCard":
                        obj.idTxnPurchaseCashCard = registro[arrayIndex + 1];
                        break;
                    case "Date":
                        string date = registro[arrayIndex + 1];
                        if (date != string.Empty)
                            obj.Date = DateTime.Parse(registro[arrayIndex + 1]);
                        else
                            obj.Date = null;
                        break;
                    case "fkIdMerchant":
                        obj.fkIdMerchant = registro[arrayIndex + 1];
                        break;
                    case "fkIdTerminal":
                        obj.fkIdTerminal = registro[arrayIndex + 1];
                        break;
                    case "fkIdProduct":
                        obj.fkIdProduct = registro[arrayIndex + 1];
                        break;
                    case "Amount":
                        obj.Amount = registro[arrayIndex + 1];
                        break;
                    case "LicensePlate":
                        obj.LicensePlate = registro[arrayIndex + 1];
                        break;
                    case "Litres":
                        obj.Litres = registro[arrayIndex + 1];
                        break;
                    case "Kms":
                        obj.Kms = registro[arrayIndex + 1];
                        break;
                    case "ProductUnitPrice":
                        obj.ProductUnitPrice = registro[arrayIndex + 1];
                        break;
                    case "AuthNum":
                        obj.AuthNum = registro[arrayIndex + 1];
                        break;
                    case "orden":
                        obj.orden = registro[arrayIndex + 1];
                        break;
                    default:
                        break;
                }
                arrayIndex++;
            }
            return obj;
        }
    }
}
