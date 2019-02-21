using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mlDieselWS.DAL;
using mlDieselWS.EnergexService;
using System.Configuration;

namespace mlDieselWS
{
    public class EnergexBLL
    {
        private readonly CombustibleEntities db;

        public EnergexBLL()
        {
            db = new CombustibleEntities();
        }        

        public int TotalAuthorizationProcess(List<EnergexAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;
                    foreach (var item in List)
                    {
                        if (item.orden != null)
                        {
                            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                            var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim() && w.Estatus == Estatus).FirstOrDefault();

                            if (LitrosCargados != null)
                            {
                                LitrosCargados.LitrosCargados1 = !String.IsNullOrEmpty(item.MaxLitres.Trim()) ? Convert.ToDecimal(item.MaxLitres.Trim()) : 0;
                                LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);

                                db.Entry(LitrosCargados).State = EntityState.Modified;
                                db.SaveChanges();

                                AutorizacionesEnergex Log = new AutorizacionesEnergex();

                                Log.idTxnEnquiryMaxLoad = item.idTxnEnquiryMaxLoad.Trim();
                                Log.Date = item.Date;
                                Log.fkIdMerchant = item.fkIdMerchant.Trim();
                                Log.group_member = item.group_member.Trim();
                                Log.orden = item.orden.Trim();
                                Log.MaxLitres = !String.IsNullOrEmpty(item.MaxLitres.Trim()) ? item.MaxLitres.Trim() : "El Servicio Energex no trajo los litros cargados";
                                Log.vehicle = item.vehicle.Trim();
                                Log.comment = item.comment.Trim();
                                Log.Estatus = "UTILIZADA TOTAL";

                                db.AutorizacionesEnergex.Add(Log);
                                db.SaveChanges();

                                Result = StatusProcess.EJECUTOPROCESO;                                                         
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
                    EnviarCorreoError(ex.Message,"Autorizaciones Totales Energex");
                    return Result;
                }
            }
        }

        public int PartialAuthorizationProcess(List<EnergexPartialAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;
                    foreach (var item in List)
                    {
                        if (item.orden != null)
                        {
                            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                            var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim() && w.Estatus == Estatus).FirstOrDefault();

                            if (LitrosCargados != null)
                            {
                                LitrosCargados.LitrosCargados1 = Convert.ToDecimal(item.Litres.Trim());
                                decimal? LitrosRestantes = LitrosCargados.LitrosSolicitados - LitrosCargados.LitrosCargados1;
                                string EstatusComentario = string.Empty;

                                if (LitrosRestantes <= Configuration.LITROMINIMO)
                                {
                                    LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);
                                    Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);                                    
                                    EstatusComentario = "AUTORIZACION PARCIAL ES MENOR A 1 LITRO";
                                }
                                else
                                {
                                    int? NumEmpleadoMexLog = db.EmpleadoEnergex.Where(w => w.NumEmpleadoEnergex == LitrosCargados.NumeroEmpleadoEnergex).Select(s => s.NumEmpleadoMexLog).FirstOrDefault();
                                    string NumEmpleadoEnergex = LitrosCargados.NumeroEmpleadoEnergex;
                                    LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);
                                    string Folio = db.LitrosCargados.OrderByDescending(o => o.FolioEnergex).Select(s => s.FolioEnergex).Take(1).FirstOrDefault();
                                    Folio = FolioGenerator(Folio);
                                    DateTime FechaVencimiento = DateTime.Now.AddDays(Configuration.ExpirationDays);

                                    string Comentario = string.Empty;

                                    if (LitrosCargados.SolicitudDepositoId != null)
                                    {
                                        int numeroviaje = 0;
                                        numeroviaje = db.SolicitudDeposito.Where(w => w.SolicitudDepositoId == LitrosCargados.SolicitudDepositoId)
                                                .Select(s => s.ClaveCarga).FirstOrDefault();

                                        Comentario = Configuration.COMMENT + numeroviaje + ".";
                                    }

                                    if (LitrosCargados.SolicitudDepositoTraficoId != null)
                                    {
                                        string FolioOrden = string.Empty;
                                        int Id = 0;

                                        Id = db.SolicitudDepositoTraficoDiesel.Where(w => w.SolicitudDepositoDieselId == LitrosCargados.SolicitudDepositoTraficoId)
                                            .Select(s => s.SolicitudDepositoId).FirstOrDefault();

                                        FolioOrden = db.SolicitudDepositoTrafico.Where(w => w.SolicitudDepositoId == Id)
                                        .Select(s => s.FolioOrden).FirstOrDefault();

                                        Comentario = Configuration.COMMENT + FolioOrden + ".";
                                    }

                                    if (LitrosCargados.SolicitudDepositoComplementoId != null)
                                    {
                                        int Id = 0;
                                        int clavecarga = 0;

                                        Id = db.SolicitudDepositoComplemento.Where(w => w.SolicitudDepositoComplementoId == LitrosCargados.SolicitudDepositoComplementoId)
                                                .Select(s => s.SolicitudDepositoId).FirstOrDefault();

                                        clavecarga = db.SolicitudDeposito.Where(w => w.SolicitudDepositoId == Id)
                                            .Select(s => s.ClaveCarga).FirstOrDefault();

                                        Comentario = Configuration.COMMENT + clavecarga + ".";
                                    }

                                    if (LitrosCargados.MovimientoExternoId != null)
                                    {
                                        Comentario = "Se realizo autorizacion de combustible desdes el servicio, Movimiento Externo con Folio: " + Folio;
                                    }

                                    SP4GLwsService Servicio = new SP4GLwsService();
                                    string request = "com.spinvent.gascard.dbobj.Txnenquirymaxload;registro_autorizacion;Cliente;" + Configuration.USERNAME + ";Contraseña;" + Configuration.PASS + ";usuario_energex;" + NumEmpleadoEnergex + ";Litros;" + Convert.ToString(LitrosRestantes) + ";Folio;" + Folio + ";Estacion;;FechaValidez;" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", FechaVencimiento) + ";usuario_cliente;" + Convert.ToString(NumEmpleadoMexLog) + ";Producto;" + item.fkIdProduct + ";Comentario;" + Comentario + ";";
                                    var response = Servicio.executeProcedureDevice(Configuration.USERNAME, Configuration.PASS, Configuration.DEVICE, request);

                                    if (response != string.Empty)
                                    {
                                        var registro = response.Split(';');
                                        int arrayIndex = 0;
                                        if (registro[arrayIndex] == "0")
                                        {
                                            LitrosCargados Cargados = new LitrosCargados();

                                            if (LitrosCargados.SolicitudDepositoId != null)
                                                Cargados.SolicitudDepositoId = LitrosCargados.SolicitudDepositoId;

                                            if (LitrosCargados.SolicitudDepositoTraficoId != null)
                                                Cargados.SolicitudDepositoTraficoId = LitrosCargados.SolicitudDepositoTraficoId;

                                            if (LitrosCargados.SolicitudDepositoComplementoId != null)
                                                Cargados.SolicitudDepositoComplementoId = LitrosCargados.SolicitudDepositoComplementoId;

                                            if (LitrosCargados.MovimientoExternoId != null)
                                                Cargados.MovimientoExternoId = LitrosCargados.MovimientoExternoId;

                                            Cargados.FolioEnergex = Folio;
                                            Cargados.LitrosSolicitados = LitrosRestantes;
                                            Cargados.LitrosCargados1 = 0;
                                            Cargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                                            Cargados.NumeroEmpleadoEnergex = NumEmpleadoEnergex;
                                            Cargados.FechaRegistro = DateTime.Now;

                                            db.LitrosCargados.Add(Cargados);
                                            db.SaveChanges();
                                            
                                            EstatusComentario = "AUTORIZACION PARCIAL, GENERO OTRA AUTORIZACION LITROS " + LitrosRestantes.ToString();
                                        }
                                        else
                                        {
                                            EstatusComentario = "AUTORIZACION PARCIAL NO GENERO OTRA AUTORIZACION POR EL ERROR: " + registro[1];
                                        }
                                    }
                                    else
                                    {
                                        EstatusComentario = "AUTORIZACION PARCIAL PROBLEMAS CON EL SERVICIO NO RESPONDE PARA GENERAR OTRA AUTORIZACION";
                                    }

                                }

                                db.Entry(LitrosCargados).State = EntityState.Modified;
                                db.SaveChanges();

                                AutorizacionesParcialesEnergex Log = new AutorizacionesParcialesEnergex();

                                Log.idTxnPurchaseCashCard = item.idTxnPurchaseCashCard.Trim();
                                Log.Date = item.Date;
                                Log.fkIdMerchant = item.fkIdMerchant.Trim();
                                Log.fkIdTerminal = item.fkIdTerminal.Trim();
                                Log.fkIdProduct = item.fkIdProduct.Trim();
                                Log.Amount = item.Amount.Trim();
                                Log.LicensePlate = item.LicensePlate.Trim();
                                Log.Litres = item.Litres.Trim();
                                Log.Kms = item.Kms.Trim();
                                Log.ProductUnitPrice = item.ProductUnitPrice.Trim();
                                Log.AuthNum = item.AuthNum.Trim();
                                Log.orden = item.orden.Trim();
                                Log.Estatus = EstatusComentario;

                                db.AutorizacionesParcialesEnergex.Add(Log);
                                db.SaveChanges();
                                Result = StatusProcess.EJECUTOPROCESO;
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
                    EnviarCorreoError(ex.Message, "Autorizaciones Parciales Energex");
                    return Result;
                }
            }
        }

        public int CanceledAuthorizationProcess(List<EnergexAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;
                    foreach (var item in List)
                    {
                        if (item.orden != null)
                        {
                            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                            var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim() && w.Estatus == Estatus).FirstOrDefault();

                            if (LitrosCargados != null)
                            {
                                LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);

                                db.Entry(LitrosCargados).State = EntityState.Modified;
                                db.SaveChanges();

                                AutorizacionesEnergex Log = new AutorizacionesEnergex();

                                Log.idTxnEnquiryMaxLoad = item.idTxnEnquiryMaxLoad.Trim();
                                Log.Date = item.Date;
                                Log.fkIdMerchant = item.fkIdMerchant.Trim();
                                Log.group_member = item.group_member.Trim();
                                Log.orden = item.orden.Trim();
                                Log.MaxLitres = item.MaxLitres.Trim();
                                Log.vehicle = item.vehicle.Trim();
                                Log.comment = item.comment.Trim();
                                Log.Estatus = "AUTORIZACION CANCELADA (PLATAFORMA)";

                                db.AutorizacionesEnergex.Add(Log);
                                db.SaveChanges();

                                Result = StatusProcess.EJECUTOPROCESO;

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
                    EnviarCorreoError(ex.Message, "Autorizaciones Canceladas Manual Energex");
                    return Result;
                }
            }
        }

        public int ValidateAuthorizationProcess(List<EnergexAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;
                    foreach (var item in List)
                    {
                        if (item.orden != null)
                        {
                            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                            var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim() && w.Estatus == Estatus).FirstOrDefault();

                            if (LitrosCargados != null)
                            {
                                TimeSpan ts = DateTime.Now - LitrosCargados.FechaRegistro.Value;

                                if (ts.Days >= Configuration.ExpirationDays)
                                {
                                    int? NumEmpleadoMexLog = db.EmpleadoEnergex.Where(w => w.NumEmpleadoEnergex == LitrosCargados.NumeroEmpleadoEnergex).Select(s => s.NumEmpleadoMexLog).FirstOrDefault();
                                    bool canceloautorizacion = CancelationProcess(item.orden.Trim(), NumEmpleadoMexLog);

                                    if (canceloautorizacion)
                                    {
                                        LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);

                                        db.Entry(LitrosCargados).State = EntityState.Modified;
                                        db.SaveChanges();

                                        AutorizacionesEnergex Log = new AutorizacionesEnergex();

                                        Log.idTxnEnquiryMaxLoad = item.idTxnEnquiryMaxLoad.Trim();
                                        Log.Date = item.Date;
                                        Log.fkIdMerchant = item.fkIdMerchant.Trim();
                                        Log.group_member = item.group_member.Trim();
                                        Log.orden = item.orden.Trim();
                                        Log.MaxLitres = item.MaxLitres.Trim();
                                        Log.vehicle = item.vehicle.Trim();
                                        Log.comment = item.comment.Trim();
                                        Log.Estatus = "AUTORIZACION VALIDA VENCIDA, SERVICIO ENERGEX NO DETECTO";

                                        db.AutorizacionesEnergex.Add(Log);
                                        db.SaveChanges();

                                        Result = StatusProcess.EJECUTOPROCESO;
                                    }
                                    else
                                    {
                                        LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);

                                        db.Entry(LitrosCargados).State = EntityState.Modified;
                                        db.SaveChanges();

                                        AutorizacionesEnergex Log = new AutorizacionesEnergex();

                                        Log.idTxnEnquiryMaxLoad = item.idTxnEnquiryMaxLoad.Trim();
                                        Log.Date = item.Date;
                                        Log.fkIdMerchant = item.fkIdMerchant.Trim();
                                        Log.group_member = item.group_member.Trim();
                                        Log.orden = item.orden.Trim();
                                        Log.MaxLitres = item.MaxLitres.Trim();
                                        Log.vehicle = item.vehicle.Trim();
                                        Log.comment = item.comment.Trim();
                                        Log.Estatus = "AUTORIZACION VALIDA VENCIDA, EL SERVICIO ENERGEX NO PUDO CANCELARLA (VERIFICAR CON ENERGEX)";

                                        db.AutorizacionesEnergex.Add(Log);
                                        db.SaveChanges();

                                        Result = StatusProcess.EJECUTOPROCESO;
                                        EnviarCorreoError("AUTORIZACION VALIDA VENCIDA, EL SERVICIO ENERGEX NO PUDO CANCELARLA (VERIFICAR CON ENERGEX)", "Autorizaciones Validas / Validas Vencidas Energex");
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
                    EnviarCorreoError(ex.Message, "Autorizaciones Validas / Validas Vencidas Energex");
                    return Result;
                }
            }
        }

        public int VencidasAuthorizationProcess(List<EnergexAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;
                    foreach (var item in List)
                    {
                        if (item.orden != null)
                        {
                            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                            var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim() && w.Estatus == Estatus).FirstOrDefault();

                            if (LitrosCargados != null)
                            {                                
                                LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);

                                db.Entry(LitrosCargados).State = EntityState.Modified;
                                db.SaveChanges();

                                AutorizacionesEnergex Log = new AutorizacionesEnergex();

                                Log.idTxnEnquiryMaxLoad = item.idTxnEnquiryMaxLoad.Trim();
                                Log.Date = item.Date;
                                Log.fkIdMerchant = item.fkIdMerchant.Trim();
                                Log.group_member = item.group_member.Trim();
                                Log.orden = item.orden.Trim();
                                Log.MaxLitres = item.MaxLitres.Trim();
                                Log.vehicle = item.vehicle.Trim();
                                Log.comment = item.comment.Trim();
                                Log.Estatus = "AUTORIZACION VENCIDA";

                                db.AutorizacionesEnergex.Add(Log);
                                db.SaveChanges();

                                Result = StatusProcess.EJECUTOPROCESO;                                                         
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
                    EnviarCorreoError(ex.Message, "Autorizaciones Vencidas detectadas por Energex");
                    return Result;
                }
            }
        }

        private string FolioGenerator(string Folio)
        {
            int Consecutivo = 0;            
            int Index = Folio.Length;
            int Longitud = 0;

            if (Folio == string.Empty)
                Folio = "F0000000001";
            else
            {
                Consecutivo = Convert.ToInt32(Folio.Substring(1, 10));
                Consecutivo += 1;
                Longitud = Consecutivo.ToString().Length;
                Index -= Longitud;
                Folio = Folio.Replace(Folio.Substring(Index, Longitud), Consecutivo.ToString());                
            }

            return Folio;
        }

        private bool CancelationProcess(string Folio, int? NumEmpleadoMexLog)
        {
            bool Result = false;
            SP4GLwsService Servicio = new SP4GLwsService();

            if (NumEmpleadoMexLog.HasValue)
            {
                string request = "com.spinvent.gascard.dbobj.Txnenquirymaxload;cancelacion_vale;Cliente;" + Configuration.USERNAME + ";Contraseña;" + Configuration.PASS + ";Folio;" + Folio + ";usuario_cliente;" + Convert.ToString(NumEmpleadoMexLog) + ";";
                var response = Servicio.executeProcedureDevice(Configuration.USERNAME, Configuration.PASS, Configuration.DEVICE, request);

                if (response != string.Empty)
                {
                    var registro = response.Split(';');
                    int arrayIndex = 0;

                    if (registro[arrayIndex] == "0")
                        Result = true;
                }
            }

            return Result;
        }

        public DateTime GetStarDate()
        {
            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
            DateTime? StarDate = db.LitrosCargados.Where(w => w.Estatus == Estatus && w.FolioEnergex != null).OrderBy(o => o.FechaRegistro).Select(s => s.FechaRegistro).FirstOrDefault();

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
                       + "<p>En el proceso: "+ TipoAutorizacion + "</p>"
                       + "<p>Error: "+Error+"</p>"
                       + "<p>Fecha: "+DateTime.Now.ToString()+"</p>";

            bool envio = email.SendMail(destinatario, Subject, msj);
        }
    }
}
