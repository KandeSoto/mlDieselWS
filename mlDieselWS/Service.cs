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
            Process.Interval = Configuration.TIME * Convert.ToInt32(TimeAppConfig);           
            Process.Elapsed += Process_Elapsed;
        }

        public void Debugger()
        {
            Process.Enabled = true;
            Process.Start();
            EventLog.WriteEntry("Se inicia el proceso energex.", EventLogEntryType.Information);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Process.Enabled = true;
                Process.Start();
                EventLog.WriteEntry("Se inicia el proceso energex.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {

                throw ex;
            }            
        }

        private void Process_Elapsed(object sender, ElapsedEventArgs e)
        {
            Process.Enabled = false;
            EventLog.WriteEntry("Inicia la ejecucion del proceso de autorizaciones energex.", EventLogEntryType.Information);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
            ExecuteProcess(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);            
            EventLog.WriteEntry("Se reinicia el proceso.", EventLogEntryType.Information);
            Process.Enabled = true;
        }

        private void ExecuteProcess(string Estatus)
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

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_CANCELADAS)
                        {
                            int Result = Ejecution.CanceledAuthorizationProcess(ObjList);
                            LogStatusProcess(Result, "Canceladas");
                        }

                        if (Estatus == AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE)
                        {
                            int Result = Ejecution.PartialAuthorizationProcess(ObjPartialList);
                            LogStatusProcess(Result, "Parciales");
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

        public void LogStatusProcess(int Result, string Estatus)
        {
            switch (Result)
            {
                case StatusProcess.SoloEjecutoProceso:
                    EventLog.WriteEntry("Solo se ejecuto correctamente el proceso de autorizaciones "+Estatus+".", EventLogEntryType.Information);
                    break;
                case StatusProcess.EnvioTotal:
                    if(Estatus != "Canceladas")
                        EventLog.WriteEntry("Se ejecuto el envio a totales correctamente desde el proceso de autorizaciones " + Estatus + ".", EventLogEntryType.Information);
                    else
                        EventLog.WriteEntry("Se ejecuto el envio a canceladas correctamente desde el proceso de autorizaciones " + Estatus + ".", EventLogEntryType.Information);
                    break;
                case StatusProcess.EnvioAutorizacion:
                    EventLog.WriteEntry("Se ejecuto un envio de autorizacion correctamente desde el proceso de autorizaciones " + Estatus + ".", EventLogEntryType.Information);
                    break;
                case StatusProcess.Error:
                    EventLog.WriteEntry("Se encontro un error en el proceso de autorizaciones " + Estatus + ".", EventLogEntryType.Error);
                    break;
                case StatusProcess.EnvioAutorizacionErrorCancelar:
                    EventLog.WriteEntry("Se encontro un error en el proceso de cancelacion cuando se intento mandar una autorizacion " + Estatus + ".", EventLogEntryType.Error);
                    break;
            }
        }

        protected override void OnStop()
        {
            Process.Stop();            
            EventLog.WriteEntry("Termino el proceso de energex", EventLogEntryType.Information);
        }
    }
}
