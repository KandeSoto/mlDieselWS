using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace mlDieselWS
{
    public class Mail
    {
        private SmtpClient objEmail;
        private MailPriority _MailPriority;
        private string _smtpServer;
        private bool _esHtml;

        public Mail()
        {
            objEmail = new SmtpClient();
            objEmail.EnableSsl = true;
            _MailPriority = MailPriority.Normal;
            _esHtml = true;
        }

        /// <summary>
        /// Envía un correo electrónico.
        /// </summary>
        /// <param name="sTo">Quién lo recibe</param>
        /// <param name="sFrom">Quién lo envía</param>
        /// <param name="sCc">Carbon Copy</param>
        /// <param name="sSubject">Título</param>
        /// <param name="sBody">Cuerpo del mensaje</param>
        /// <param name="Priority">Nivel de prioridad del correo.</param>
        /// <param name="Format">Formato del correo: Texto plano/HTML</param>
        /// <returns>Retorna un true/false indicando si el correo fue enviado con éxito.</returns>
        /// <example>
        /// <code>
        /// Email Email = new Email();
        /// Email.SendMail("ivan.valenzuela@mexlog.com", "raymundo.porchas@mexlog.com"
        /// "Prueba de envío de correo", "&lt;font color="#008000"&gt;Saludos!&lt;/font&gt;",
        /// , MailPriority.Normal, true);
        /// </code>
        /// </example> 
        public bool SendMail(string sTo, string sSubject, string sBody, MailPriority Priority, bool esHtml)
        {
            bool mailSend = false;
            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(ConfigurationManager.AppSettings["EmailInformatica"], "IT", Encoding.UTF8);
            msg.To.Add(sTo);                       
            msg.Subject = sSubject;
            msg.SubjectEncoding = Encoding.UTF8;
            msg.Body = sBody;
            msg.BodyEncoding = Encoding.UTF8;
            msg.IsBodyHtml = esHtml;
            msg.Priority = Priority;

            try
            {                                
                objEmail.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["EmailInformatica"], ConfigurationManager.AppSettings["PassEmailInformatica"]);
                
                objEmail.Send(msg);
                mailSend = true;
            }
            catch (System.Net.Mail.SmtpException ex)
            {                
                mailSend = false;
            }

            if (mailSend == false)
            {
                try
                {
                    msg.From = new MailAddress(ConfigurationManager.AppSettings["EmailInformatica"], "IT", Encoding.UTF8);                    
                    objEmail.Credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["EmailInformatica"], ConfigurationManager.AppSettings["PassEmailInformatica"]);
                    objEmail.Send(msg);
                    mailSend = true;
                }
                catch (System.Net.Mail.SmtpException ex)
                {                    
                    mailSend = false;
                }
            }
            
            return mailSend;
        }

        public bool SendMail(string sTo, string sSubject, string sBody)
        { 
            return this.SendMail(sTo, sSubject, sBody, _MailPriority, _esHtml);
        }

    }
}
