using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public static class FeedbackSystem
{
    public class FeedbackStruct
    {
        public string Version;
        public Vector3 Coordinates;
        public int Assessment;
        public string Message;
        public string Time;
        public Texture2D Screenshot;
    }
    public static FeedbackStruct FeedbackVariable;

    private static string FormatMessage(FeedbackStruct f)
    {
        return "Coordinates: " + f.Coordinates.ToString() + "\nAssessment: " + f.Assessment.ToString() + "\nMessage: " + f.Message + "\nTime: " + System.DateTime.Today.ToString();
    }

    public static void SendMail()
    {
        string Address = ""; //put address here (NOT A PERSONAL ADDRESS)
        string Password = ""; //put password here (if you're some random person on GitHub, please don't screw us over ;-;)
        MailMessage mail = new(Address, Address);
        mail.Subject = "MESOCYCLONE FEEDBACK (" + FeedbackVariable.Version + ")";
        mail.Body = FormatMessage(FeedbackVariable);

        SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
        smtpServer.Port = 587;//GIVE CORRECT PORT HERE
        smtpServer.Credentials = new NetworkCredential(Address, Password) as ICredentialsByHost;
        smtpServer.EnableSsl = true;
        ServicePointManager.ServerCertificateValidationCallback =
        delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        { return true; };

        try
        {
            smtpServer.Send(mail);
        }
        catch (System.Exception e)
        {
            Debug.Log("Email error: " + e.ToString());
        }
        finally
        {
            Debug.Log("Email sent successfully!");
        }
    }
}
