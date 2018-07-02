using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mlDieselWS.DAL;
using mlDieselWS.EnergexService;

namespace mlDieselWS
{
    public class EnergexBLL
    {
        private readonly CombustibleEntities db;

        public EnergexBLL()
        {
            db = new CombustibleEntities();
        }

        public int PartialAuthorizationProcess(List<EnergexPartialAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result =  StatusProcess.SoloEjecutoProceso;
                    foreach (var item in List)
                    {
                        int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
                        var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim() && w.Estatus == Estatus).FirstOrDefault();                        

                        if (LitrosCargados != null)
                        {                            
                            LitrosCargados.LitrosCargados1 = Convert.ToDecimal(item.Litres.Trim());
                            decimal? LitrosRestantes = LitrosCargados.LitrosSolicitados - LitrosCargados.LitrosCargados1;

                            if (LitrosRestantes <= 0)
                            {
                                LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);                                
                                Result = StatusProcess.EnvioTotal;
                            }
                            else
                            {                                
                                int? NumEmpleadoMexLog = db.EmpleadoEnergex.Where(w => w.NumEmpleadoEnergex == LitrosCargados.NumeroEmpleadoEnergex).Select(s => s.NumEmpleadoMexLog).FirstOrDefault();
                                int? NumEmpleadoEnergex = LitrosCargados.NumeroEmpleadoEnergex;
                                LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_PARCIALMENTE);                                
                                string Folio = db.LitrosCargados.OrderByDescending(o => o.LitrosCargadosId).Select(s => s.FolioEnergex).Take(1).FirstOrDefault();
                                Folio = FolioGenerator(Folio);
                                DateTime FechaVencimiento = DateTime.Now.AddDays(Configuration.ExpirationDays);

                                string Comentario = Configuration.COMMENT + item.orden.Trim() + ".";

                                SP4GLwsService Servicio = new SP4GLwsService();
                                string request = "com.spinvent.gascard.dbobj.Txnenquirymaxload;registro_autorizacion;Cliente;" + Configuration.USERNAME + ";Contraseña;" + Configuration.PASS + ";usuario_energex;" + Convert.ToString(NumEmpleadoEnergex) + ";Litros;" + Convert.ToString(LitrosRestantes) + ";Folio;" + Folio + ";Estacion;;FechaValidez;" + String.Format("{0:yyyy-MM-dd HH:mm:ss}", FechaVencimiento) + ";usuario_cliente;" + Convert.ToString(NumEmpleadoMexLog) + ";Producto;"+item.fkIdProduct+";Comentario;" + Comentario + ";";
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
                                        Result = StatusProcess.EnvioAutorizacion;
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Error: "+registro[1]+" Codigo: "+ registro[arrayIndex]);
                                    }
                                }
                                else
                                {
                                    throw new ArgumentException("No fue posible enviar autorizacion");
                                }

                            }

                            LitrosCargados.FechaRegistro = DateTime.Now;
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

                            db.AutorizacionesParcialesEnergex.Add(Log);
                            db.SaveChanges();

                        }
                        //else
                        //    throw new ArgumentException("Folio Energex viene vacio");

                    }

                    transaction.Commit();
                    return Result;
                }
                catch (Exception)
                {
                    transaction.Rollback();

                    Result = StatusProcess.Error;
                    return Result;
                }
            }
        }

        public int TotalAuthorizationProcess(List<EnergexAuthorizationList> List)
        {
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.SoloEjecutoProceso;
                    foreach (var item in List)
                    {
                        var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim()).FirstOrDefault();

                        if (LitrosCargados != null)
                        {                            
                            LitrosCargados.LitrosCargados1 = Convert.ToDecimal(item.MaxLitres.Trim());
                            decimal? LitrosRestantes = LitrosCargados.LitrosSolicitados - LitrosCargados.LitrosCargados1;

                            if (LitrosRestantes <= 0)                            
                                LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_UTILIZADAS_TOTALIDAD);                            
                            else
                                throw new ArgumentException("El folio "+ LitrosCargados.FolioEnergex + " energex no es una autorizacion en su totalidad");

                            LitrosCargados.FechaRegistro = DateTime.Now;                            
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

                            db.AutorizacionesEnergex.Add(Log);
                            db.SaveChanges();

                            Result = StatusProcess.EnvioTotal;

                        }
                        //else
                        //    throw new ArgumentException("Folio Energex viene vacio");

                    }

                    transaction.Commit();
                    return Result;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    Result = StatusProcess.Error;
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
                    Result = StatusProcess.SoloEjecutoProceso;
                    foreach (var item in List)
                    {
                        var LitrosCargados = db.LitrosCargados.Where(w => w.FolioEnergex == item.orden.Trim()).FirstOrDefault();

                        if (LitrosCargados != null)
                        {                            
                            LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_CANCELADAS);
                            LitrosCargados.FechaRegistro = DateTime.Now;                            

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

                            db.AutorizacionesEnergex.Add(Log);
                            db.SaveChanges();

                            Result = StatusProcess.EnvioTotal;

                        }
                        //else
                        //    throw new ArgumentException("Folio Energex viene vacio");

                    }

                    transaction.Commit();
                    return Result;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    Result = StatusProcess.Error;
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

        public DateTime GetStarDate()
        {
            int Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VALIDAS);
            DateTime? StarDate = db.LitrosCargados.Where(w => w.Estatus == Estatus).OrderBy(o => o.FechaRegistro).Select(s => s.FechaRegistro).FirstOrDefault();

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
