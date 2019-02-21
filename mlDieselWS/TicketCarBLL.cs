using mlDieselWS.DAL;
using mlDieselWS.TicketCardService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mlDieselWS
{
    public class TicketCarBLL
    {
        private readonly CombustibleEntities db;

        public TicketCarBLL()
        {
            db = new CombustibleEntities();
        }        

        public int TicketCarProcess(string NotaEstatus)
        {
            #region TicketCarProcess
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;                    

                    DateTime DateStart = GetStarDate(NotaEstatus);
                    DateTime DateEnd = DateTime.Now.AddDays(1);

                    int EstatusLitros = Convert.ToInt32(NotaEstatus);
                    var LitrosCargadosList = db.LitrosCargados.Where(w => w.OrdenIdentificadorTicket != null 
                    && w.Estatus == EstatusLitros && (w.FechaRegistro >= DateStart && w.FechaRegistro <= DateEnd)).ToList();

                    foreach (var item in LitrosCargadosList)
                    {                        
                        if (item != null)
                        {
                            Tuple<int, int> EstatusTupla = ObtenerEstatusOrdenNotaVale(item.OrdenIdentificadorTicket.Value);
                            int Estatus = 0;

                            if (EstatusTupla.Item1 != 0)
                            {                                 
                                if (EstatusTupla.Item2 < ConfigurationTicketCar.VECES_UTILIZADAS)
                                {
                                    Tuple<bool, decimal> Transaccion = ObtenerListadoTransacciones(DateStart, DateEnd, item.NumeroTarjetaTicket, item.IdentificadorTicket.Value);

                                    if (Transaccion.Item1 == true && Transaccion.Item2 <= 0)
                                        Transaccion = new Tuple<bool, decimal>(false, 0);

                                    if (Transaccion.Item1 == true)
                                    {
                                        if (Transaccion.Item2 == item.LitrosSolicitados)
                                            Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
                                        else
                                        {
                                            TimeSpan ts = DateTime.Now - item.FechaRegistro.Value;
                                            if (ts.Days >= Configuration.ExpirationDays || EstatusTupla.Item1 == NotaValeEstatus.VENCIDA)
                                            {
                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);
                                            }
                                            else if (EstatusTupla.Item1 == NotaValeEstatus.ANULADA)
                                            {
                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
                                            }
                                            else
                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);
                                        }

                                        item.LitrosCargados1 = Transaccion.Item2;
                                        item.Estatus = Estatus;

                                        db.Entry(item).State = EntityState.Modified;
                                        db.SaveChanges();

                                        Result = StatusProcess.EJECUTOPROCESO;
                                    }
                                }
                                else
                                {
                                    TimeSpan ts = DateTime.Now - item.FechaRegistro.Value;
                                    if (ts.Days >= Configuration.ExpirationDays || EstatusTupla.Item1 == NotaValeEstatus.VENCIDA)
                                    {
                                        item.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);

                                        db.Entry(item).State = EntityState.Modified;
                                        db.SaveChanges();

                                        Result = StatusProcess.EJECUTOPROCESO;
                                    }
                                    else if (EstatusTupla.Item1 == NotaValeEstatus.ANULADA)
                                    {
                                        item.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);

                                        db.Entry(item).State = EntityState.Modified;
                                        db.SaveChanges();

                                        Result = StatusProcess.EJECUTOPROCESO;
                                    }
                                }                                
                            }
                        }
                    }
                    
                    transaction.Commit();
                    return Result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Result = StatusProcess.ERROR;
                    EnviarCorreoError(ex.Message, "Autorizaciones Notavale Ticket Car");
                    return Result;
                }
            }
            #endregion
        }

        private List<int> ObtenerListadoOrdenes(DateTime Star, DateTime End)
        {
            #region ObtenerListadoOrdenes
            List<int> ListaOrdenIdentification = new List<int>();

            try
            {
                var ExtClient = new TicketCarFacadeServicesClient("BasicHttpsBinding_ITicketCarFacadeServices");
                ExtClient.ClientCredentials.UserName.UserName = ConfigurationTicketCar.EXTERNAL_VALID_USER_ID;
                ExtClient.ClientCredentials.UserName.Password = ConfigurationTicketCar.EXTERNAL_VALID_PASSWORD;

                var request = new OrderDTORequest();
                request.Security = new SecurityDTO { Token = ConfigurationTicketCar.PROVIDED_TOKEN, Ip = ConfigurationTicketCar.CLIENT_IP };
                request.Paging = new PagingDTO { All = ConfigurationTicketCar.GET_ALL_RECORDS, PageNumber = ConfigurationTicketCar.PAG_NUMBER, PageRecords = ConfigurationTicketCar.PAGE_RECORDS };
                request.Item = new OrderDTO()
                {
                    OrderDateStart = Star,
                    OrderDateEnd = End,
                    OrderTypeIdentification = OrderType.NoteVoucher19
                };

                OrderListDTOResponse response = new OrderListDTOResponse();
                response = ExtClient.OrderGetFilteredList(request);

                if (response.Success)
                {
                    foreach (var item in response.List)
                    {
                        ListaOrdenIdentification.Add(Convert.ToInt32(item.OrderNumber));
                    }
                }
                else if (response.ErrorList != null && response.ErrorList.Any())
                {                    
                    EnviarCorreoError("Error: " + response.ErrorList.Select(s => s.Message).FirstOrDefault() + " Codigo: " + response.ErrorList.Select(s => s.Code).FirstOrDefault(), "Obtener Listados de Ordenes de Ticket Car");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ListaOrdenIdentification;

            #endregion
        }

        private Tuple<int,int> ObtenerEstatusOrdenNotaVale(int OrderNumber)
        {
            #region ObtenerOrdenEstatusNotaVale            
            Tuple<int, int> NotaValeEstatus = new Tuple<int, int>(0,0);
            try
            {
                var ExtClient = new TicketCarFacadeServicesClient("BasicHttpsBinding_ITicketCarFacadeServices");
                ExtClient.ClientCredentials.UserName.UserName = ConfigurationTicketCar.EXTERNAL_VALID_USER_ID;
                ExtClient.ClientCredentials.UserName.Password = ConfigurationTicketCar.EXTERNAL_VALID_PASSWORD;

                var request = new OrderDTORequest();
                request.Security = new SecurityDTO { Token = ConfigurationTicketCar.PROVIDED_TOKEN, Ip = ConfigurationTicketCar.CLIENT_IP };
                request.Paging = new PagingDTO { All = ConfigurationTicketCar.GET_ALL_RECORDS, PageNumber = ConfigurationTicketCar.PAG_NUMBER, PageRecords = ConfigurationTicketCar.PAGE_RECORDS };
                request.Item = new OrderDTO()
                {
                    OrderNumber = Convert.ToUInt32(OrderNumber),
                    OrderTypeIdentification = OrderType.NoteVoucher19
                };

                OrdeItemDTOResponse response = new OrdeItemDTOResponse();
                response = ExtClient.OrderGetItem(request);

                if (response.Success)
                {                    
                    NotaValeEstatus = new Tuple<int, int>(Convert.ToInt32(response.Item.DetailList.Select(s => s.PreAuthorizationStatusIdentification).FirstOrDefault()), Convert.ToInt32(response.Item.PreAuthorizationAvailableQuantity));                                                           
                }
                else if (response.ErrorList != null && response.ErrorList.Any())
                {                    
                    EnviarCorreoError("Error: " + response.ErrorList.Select(s => s.Message).FirstOrDefault() + " Codigo: " + response.ErrorList.Select(s => s.Code).FirstOrDefault(), "Obtener Estatus NotaVale y el uso de la NotaVale de Ticket Car");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NotaValeEstatus;

            #endregion
        }

        private Tuple<bool,decimal> ObtenerListadoTransacciones(DateTime Star, DateTime End, string CardNumber, int IdentificadorTicket)
        {
            decimal LitrosCargados = 0;
            bool Resultado = false;

            #region ObtenerListadoTransacciones
            try
            {
                var ExtClient = new TicketCarFacadeServicesClient("BasicHttpsBinding_ITicketCarFacadeServices");
                ExtClient.ClientCredentials.UserName.UserName = ConfigurationTicketCar.EXTERNAL_VALID_USER_ID;
                ExtClient.ClientCredentials.UserName.Password = ConfigurationTicketCar.EXTERNAL_VALID_PASSWORD;

                var request = new TransactionDTORequest();
                request.Security = new SecurityDTO { Token = ConfigurationTicketCar.PROVIDED_TOKEN, Ip = ConfigurationTicketCar.CLIENT_IP };
                request.Paging = new PagingDTO { All = ConfigurationTicketCar.GET_ALL_RECORDS, PageNumber = ConfigurationTicketCar.PAG_NUMBER, PageRecords = ConfigurationTicketCar.PAGE_RECORDS };
                request.Item = new TransactionDTO()
                {
                    TransactionDateTimeStart = Star,
                    TransactionDateTimeEnd = End,
                    InformationType = ConfigurationTicketCar.ONLINE_TRX ? 1 : 0,
                    CardNumber = CardNumber
                };

                TransactionListDTOResponse response = new TransactionListDTOResponse();
                response = ExtClient.TransactionGetFilteredList(request);

                if (response.Success)
                {
                    if (response.List != null)
                    {
                        foreach (var item in response.List)
                        {
                            if (item.CardRequisition.PreAuhorizationIdentification == IdentificadorTicket && item.Status == "APROBADA")
                                LitrosCargados += item.Detail.Merchandise.Quantity;
                            else if (item.CardRequisition.PreAuhorizationIdentification == IdentificadorTicket && item.Status == "PENDIENTE")
                                LitrosCargados += item.Detail.Merchandise.Quantity;
                        }                        
                                                
                        Resultado = true;
                    }
                }
                else if (response.ErrorList != null && response.ErrorList.Any())
                {
                    EnviarCorreoError("Error: " + response.ErrorList.Select(s => s.Message).FirstOrDefault() + " Codigo: " + response.ErrorList.Select(s => s.Code).FirstOrDefault(), "Obtener Listados de Transacciones de Ticket Car");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new Tuple<bool, decimal>(Resultado, LitrosCargados); 
            #endregion
        }        

        public DateTime GetStarDate(string NotaEstatus)
        {
            int Estatus = Convert.ToInt32(NotaEstatus);
            DateTime? StarDate = db.LitrosCargados.Where(w => w.Estatus == Estatus && w.OrdenIdentificadorTicket != null).OrderBy(o => o.FechaRegistro).Select(s => s.FechaRegistro).FirstOrDefault();

            if (StarDate != null)
            {
                DateTime Fecha = Convert.ToDateTime(StarDate).AddDays(-1);
                return Fecha;
            }
            else
                return DateTime.Now;
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

    }
}
