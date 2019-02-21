using mlDieselWS.DAL;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        public int DispensarioProcess()
        {
            #region DispensarioProcess
            int Result;
            using (DbContextTransaction transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Result = StatusProcess.INICIOPROCESO;

                    List<ObtenerListaSolicitudDispensarioActivas_Result> ListaDispensario = ObtenerListadoDispensario();

                    if (ListaDispensario != null)
                    {
                        foreach (var item in ListaDispensario)
                        {
                            if (item.SolicitudDepositoId.HasValue)
                            {
                                var LitrosCargados = db.LitrosCargados.Where(w => w.SolicitudDepositoId == item.SolicitudDepositoId).FirstOrDefault();

                                if (LitrosCargados != null)
                                {
                                    TimeSpan ts = DateTime.Now - LitrosCargados.FechaRegistro.Value;

                                    if (ts.Days >= Configuration.ExpirationDays)
                                    {
                                        LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);

                                        db.Entry(LitrosCargados).State = EntityState.Modified;
                                        db.SaveChanges();

                                        db.CancelarSolicitudDispensarioServicio(item.DepositosDieselId);
                                        Result = StatusProcess.INICIOPROCESO;
                                    }
                                }
                            }
                            else if (item.SolicitudDepositoComplementoId.HasValue)
                            {
                                var LitrosCargados = db.LitrosCargados.Where(w => w.SolicitudDepositoComplementoId == item.SolicitudDepositoComplementoId).FirstOrDefault();

                                if (LitrosCargados != null)
                                {
                                    TimeSpan ts = DateTime.Now - LitrosCargados.FechaRegistro.Value;

                                    if (ts.Days >= Configuration.ExpirationDays)
                                    {
                                        LitrosCargados.Estatus = Convert.ToInt32(AuthorizationStatus.AUTORIZACIONES_VENCIDAS);

                                        db.Entry(LitrosCargados).State = EntityState.Modified;
                                        db.SaveChanges();

                                        db.CancelarSolicitudDispensarioServicio(item.DepositosDieselId);
                                        Result = StatusProcess.INICIOPROCESO;
                                    }
                                }
                            }
                            else
                            {
                                TimeSpan ts = DateTime.Now - item.fechaDepositoUTC;

                                if (ts.Days >= Configuration.ExpirationDays)
                                {
                                    db.CancelarSolicitudDispensarioServicio(item.DepositosDieselId);
                                    Result = StatusProcess.INICIOPROCESO;
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
                    EnviarCorreoError(ex.Message, "Problemas Autorizaciones Vencidas Dispensario");
                    Result = StatusProcess.ERROR;
                    return Result;
                }
            }
            #endregion
        }

        private List<ObtenerListaSolicitudDispensarioActivas_Result> ObtenerListadoDispensario()
        {
            #region ObtenerListadoDispensario            

            try
            {
                var ListaDispesario = db.ObtenerListaSolicitudDispensarioActivas().ToList();
                return ListaDispesario;
            }
            catch (Exception ex)
            {
                throw ex;
            }            

            #endregion
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
