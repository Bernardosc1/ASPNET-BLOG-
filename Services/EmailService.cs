using System.Net;
using System.Net.Mail;

namespace Blog.Services
{
    public class EmailService
    {
        public int MyProperty { get; set; }

        public bool Send(
             string toName,
            string toEmail,
            string subject,
            string body,
            string fromName = "Equipe Bersol",
            string fromEmail = "beuscunha@gmail.com"
            )
        {
            var smtpClient = new SmtpClient(Configuration.Smtp.Host, Configuration.Smtp.Port)
            {
                Credentials = new NetworkCredential(Configuration.Smtp.UserName, Configuration.Smtp.Password), //Define as credenciais de rede 
                DeliveryMethod = SmtpDeliveryMethod.Network, //Define o método de entrega
                EnableSsl = true //Habilita o SSL
            };

            var mail = new MailMessage();

                mail.From = new MailAddress(fromEmail, fromName); //Define o remetente
                mail.To.Add(new MailAddress(toEmail, toName)); //Define o destinatário
                mail.Subject = subject; //Define o assunto
                mail.Body = body; //Define o corpo
                mail.IsBodyHtml = true; //Define que o corpo é HTML


            try //Tenta enviar o email
            {
                smtpClient.Send(mail); //Envia o email
                return true; //Retorna verdadeiro
            }
            catch //Caso ocorra uma exceção
            {
                return false; //Retorna falso
            }
        }
    }
}
