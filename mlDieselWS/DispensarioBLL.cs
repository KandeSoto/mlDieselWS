using mlDieselWS.DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mlDieselWS
{
    public class DispensarioBLL
    {
        private readonly CombustibleEntities db;

        public DispensarioBLL()
        {
            db = new CombustibleEntities();
        }

        //public int DispensarioProcess()
        //{
        //    #region DispensarioProcess
        //    int Result;
        //    using (DbContextTransaction transaction = db.Database.BeginTransaction())
        //    {
        //        try
        //        {
        //            Result = StatusProcess.SoloEjecutoProceso;

        //            List<ObtenerListaSolicitudDispensarioActivas_Result> ListaDispensario = ObtenerListadoDispensario();

        //            if (ListaDispensario != null)
        //            {
        //                foreach (var item in ListaDispensario)
        //                {
        //                    int EstatusLitros = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
        //                    var LitrosCargados = db.LitrosCargados.Where(w => w.OrdenIdentificadorTicket == item).FirstOrDefault();

        //                    if (LitrosCargados != null)
        //                    {
        //                        Tuple<int, int> EstatusTupla = ObtenerEstatusOrdenNotaVale(item);
        //                        int Estatus = 0;

        //                        if (EstatusTupla.Item1 != 0)
        //                        {
        //                            Tuple<bool, decimal> Transaccion = new Tuple<bool, decimal>(true, 0);

        //                            if (EstatusTupla.Item1 != 13)
        //                            {
        //                                Transaccion = ObtenerListadoTransacciones(Star, End, LitrosCargados.NumeroTarjetaTicket, LitrosCargados.IdentificadorTicket.Value);

        //                                if (Transaccion.Item1 == true && Transaccion.Item2 <= 0)
        //                                    Transaccion = new Tuple<bool, decimal>(false, 0);
        //                            }

        //                            if (Transaccion.Item1 != false)
        //                            {
        //                                switch (EstatusTupla.Item1)
        //                                {
        //                                    case NotaValeEstatus.EN_ESPERA:
        //                                        switch (EstatusTupla.Item2)
        //                                        {
        //                                            case NotaValeSubEstatus.SUB_EN_ESPERA:
        //                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
        //                                                break;
        //                                            case NotaValeSubEstatus.SUB_EN_USO:
        //                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
        //                                                break;
        //                                            case NotaValeSubEstatus.SUB_PENDIENTE_ANULAR:
        //                                                Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
        //                                                break;
        //                                        }
        //                                        break;
        //                                    case NotaValeEstatus.USADA:
        //                                        Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
        //                                        break;
        //                                    case NotaValeEstatus.ANULADA:
        //                                        Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
        //                                        break;
        //                                    case NotaValeEstatus.VENCIDA:
        //                                        if (Transaccion.Item2 > 0)
        //                                            Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);
        //                                        else
        //                                            Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
        //                                        break;
        //                                }

        //                                if (LitrosCargados.SolicitudDepositoId != null && Estatus != Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS))
        //                                {

        //                                    var solicitud = db.SolicitudDeposito.Where(w => w.SolicitudDepositoId == LitrosCargados.SolicitudDepositoId).FirstOrDefault();

        //                                    if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD))
        //                                        solicitud.SolicitudDepositoStatusId = SolicitudDepositoStatus.APROBADO;

        //                                    if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS))
        //                                        solicitud.SolicitudDepositoStatusId = SolicitudDepositoStatus.RECHAZADO;

        //                                    db.Entry(solicitud).State = EntityState.Modified;
        //                                    db.SaveChanges();

        //                                }

        //                                if (LitrosCargados.SolicitudDepositoTraficoId != null && Estatus != Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS))
        //                                {
        //                                    var solicitudDiesel = db.SolicitudDepositoTraficoDiesel.Where(w => w.SolicitudDepositoDieselId == LitrosCargados.SolicitudDepositoTraficoId).FirstOrDefault();

        //                                    if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD))
        //                                        solicitudDiesel.SolicitudDepositoStatusId = SolicitudDepositoStatus.APROBADO;

        //                                    if (Estatus == Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS))
        //                                        solicitudDiesel.SolicitudDepositoStatusId = SolicitudDepositoStatus.RECHAZADO;

        //                                    db.Entry(solicitudDiesel).State = EntityState.Modified;
        //                                    db.SaveChanges();

        //                                }


        //                                LitrosCargados.LitrosCargados1 = Transaccion.Item2;
        //                                LitrosCargados.Estatus = Estatus;

        //                                db.Entry(LitrosCargados).State = EntityState.Modified;
        //                                db.SaveChanges();

        //                                Result = StatusProcess.EnvioTicketCar;
        //                            }
        //                        }

        //                    }
        //                }
        //            }

        //            transaction.Commit();
        //            return Result;
        //        }
        //        catch (Exception ex)
        //        {
        //            transaction.Rollback();

        //            Result = StatusProcess.Error;
        //            return Result;
        //        }
        //    }
        //    #endregion
        //}

        private List<ObtenerListaSolicitudDispensarioActivas_Result> ObtenerListadoDispensario()
        {
            #region ObtenerListadoDispensario
            List<ObtenerListaSolicitudDispensarioActivas_Result> ListaDispesario = new List<ObtenerListaSolicitudDispensarioActivas_Result>();

            try
            {
                ListaDispesario = db.ObtenerListaSolicitudDispensarioActivas().ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ListaDispesario;

            #endregion
        }
    }
}
