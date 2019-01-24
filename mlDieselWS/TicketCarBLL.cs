using mlDieselWS.DAL;
using mlDieselWS.TicketCardService;
using System;
using System.Collections.Generic;
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

        public int TicketCarProcess(DateTime Star, DateTime End)
        {
            #region TicketCarProcess
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.SoloEjecutoProceso;

                    List<int> ListaOrdenes = ObtenerListadoOrdenes(Star, End);

                    if (ListaOrdenes != null)
                    {
                        foreach (var item in ListaOrdenes)
                        {
                            int EstatusLitros = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                            var LitrosCargados = db.LitrosCargados.Where(w => w.OrdenIdentificadorTicket == item && w.Estatus == EstatusLitros).FirstOrDefault();

                            if (LitrosCargados != null && LitrosCargados.SolicitudDepositoId == 19912)
                            {
                                Tuple<int,int> EstatusTupla = ObtenerEstatusOrdenNotaVale(item);
                                int Estatus = 0;

                                if (EstatusTupla.Item1 != 0)
                                {
                                    Tuple<bool, decimal> Transaccion = new Tuple<bool, decimal>(true, 0);

                                    if (EstatusTupla.Item1 != 13 && EstatusTupla.Item2 < ConfigurationTicketCar.VECES_UTILIZADAS)
                                    {
                                        Transaccion = ObtenerListadoTransacciones(Star, End, LitrosCargados.NumeroTarjetaTicket, LitrosCargados.IdentificadorTicket.Value);

                                        if(Transaccion.Item1 == true && Transaccion.Item2 <= 0)
                                            Transaccion = new Tuple<bool, decimal>(false, 0);
                                    }

                                    if (Transaccion.Item1 != false)
                                    {
                                        switch (EstatusTupla.Item1)
                                        {
                                            case NotaValeEstatus.EN_ESPERA:
                                                if (EstatusTupla.Item2 < ConfigurationTicketCar.VECES_UTILIZADAS)
                                                {
                                                    if(Transaccion.Item2 == LitrosCargados.LitrosSolicitados)
                                                        Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
                                                    else
                                                        Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);
                                                }
                                                else
                                                {
                                                    Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                                                }                                                                                             
                                                break;
                                            case NotaValeEstatus.USADA:
                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
                                                break;
                                            case NotaValeEstatus.ANULADA:
                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
                                                break;
                                            case NotaValeEstatus.VENCIDA:                                               
                                                    Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
                                                break;
                                        }

                                        if (LitrosCargados.SolicitudDepositoId != null && Estatus != Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS))
                                        {

                                            var solicitud = db.SolicitudDeposito.Where(w => w.SolicitudDepositoId == LitrosCargados.SolicitudDepositoId).FirstOrDefault();

                                            if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD))
                                                solicitud.SolicitudDepositoStatusId = SolicitudDepositoStatus.APROBADO;

                                            if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS))
                                                solicitud.SolicitudDepositoStatusId = SolicitudDepositoStatus.RECHAZADO;

                                            db.Entry(solicitud).State = EntityState.Modified;
                                            db.SaveChanges();

                                        }

                                        if (LitrosCargados.SolicitudDepositoTraficoId != null && Estatus != Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS))
                                        {
                                            var solicitudDiesel = db.SolicitudDepositoTraficoDiesel.Where(w => w.SolicitudDepositoDieselId == LitrosCargados.SolicitudDepositoTraficoId).FirstOrDefault();

                                            if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD))
                                                solicitudDiesel.SolicitudDepositoStatusId = SolicitudDepositoStatus.APROBADO;

                                            if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS))
                                                solicitudDiesel.SolicitudDepositoStatusId = SolicitudDepositoStatus.RECHAZADO;

                                            db.Entry(solicitudDiesel).State = EntityState.Modified;
                                            db.SaveChanges();

                                        }

                                        LitrosCargados.LitrosCargados1 = Transaccion.Item2;
                                        LitrosCargados.Estatus = Estatus;

                                        db.Entry(LitrosCargados).State = EntityState.Modified;
                                        db.SaveChanges();

                                        Result = StatusProcess.EnvioTicketCar;
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

                    Result = StatusProcess.Error;
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
                    throw new ArgumentException("Error: " + response.ErrorList.Select(s => s.Message).FirstOrDefault() + " Codigo: " + response.ErrorList.Select(s => s.Code).FirstOrDefault());
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
            // int NotaValeEstatus = 0;
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
                    throw new ArgumentException("Error: " + response.ErrorList.Select(s => s.Message).FirstOrDefault() + " Codigo: " + response.ErrorList.Select(s => s.Code).FirstOrDefault());
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return NotaValeEstatus;

            #endregion
        }

        private Tuple<bool,decimal> ObtenerListadoTransacciones(DateTime Star, DateTime End, string CardNumber, int Identificador)
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
                            if (item.CardRequisition.PreAuhorizationIdentification == Identificador)
                                LitrosCargados += item.Detail.Merchandise.Quantity;                                                        
                        }                        
                        
                        Resultado = true;
                    }
                }
                else if (response.ErrorList != null && response.ErrorList.Any())
                {
                    throw new ArgumentException("Error: " + response.ErrorList.Select(s => s.Message).FirstOrDefault() + " Codigo: " + response.ErrorList.Select(s => s.Code).FirstOrDefault());
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return new Tuple<bool, decimal>(Resultado, LitrosCargados); 
            #endregion
        }

        public DateTime GetStarDate()
        {
            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
            DateTime? StarDate = db.LitrosCargados.Where(w => w.Estatus == Estatus && w.OrdenIdentificadorTicket != null).OrderBy(o => o.FechaRegistro).Select(s => s.FechaRegistro).FirstOrDefault();

            if (StarDate != null)
            {
                DateTime Fecha = Convert.ToDateTime(StarDate).AddDays(-1);
                return Fecha;
            }
            else
                return DateTime.Now;
        }

    }
}
