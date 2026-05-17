using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public static class FeedbackSystem
{
    public enum Opinion
    {
        unassigned = 0,
        Positive = 1,
        LightlyPositive = 2,
        LightlyNegative = 3,
        Negative = 4
    }

    public class FeedbackStruct
    {
        public string Version = "v";
        public Vector3 Coordinates = Vector3.zero;
        public Opinion Assessment = Opinion.unassigned;
        public bool IsBugReport = false;
        public string Message = "";
        public string Time = "00/00/0001";
        public string ScreenshotFilePath;

        /// <summary>
        /// Utility Property to fix naming
        /// </summary>
        public string _Assessment // couldn't figure out a better naming convention
        {
            get
            {
                if (Assessment == Opinion.unassigned) return "Unassigned";
                else if (Assessment == Opinion.LightlyNegative) return "Lightly Negative";
                else if (Assessment == Opinion.LightlyPositive) return "Lightly Positive";
                else return Assessment.ToString(); // naming is fine
            }
        }
    }
    public static FeedbackStruct FeedbackVariable = new();

    public static void SetFeedback(int assessment) => FeedbackVariable.Assessment = (Opinion)assessment;
    public static void SetFeedback(string message) => FeedbackVariable.Message = message;
    public static void SetFeedback(bool bugReport) => FeedbackVariable.IsBugReport = bugReport;
    public static void SetFeedback(Vector3 position) => FeedbackVariable.Coordinates = position;

    private static string FormatMessage(FeedbackStruct f)
    {
        return "Coordinates: " + f.Coordinates.ToString() + "\nIs a Bug Report: " + f.IsBugReport.ToString() + "\nAssessment: " + f._Assessment + "\nMessage: " + f.Message + "\nTime: " + f.Time;
    }

    public static void SendMail(string path)
    {
        FeedbackVariable.Version = Application.version;
        FeedbackVariable.Time = System.DateTime.Today.ToShortDateString();
        FeedbackVariable.ScreenshotFilePath = path;

        string Address = "feedback.unknownsimprograms@gmail.com"; //put address here (NOT A PERSONAL ADDRESS)
        string Password = "zrmxbrfioqghfcrr"; //put password here (if you're some random person on GitHub, please don't screw us over ;-;)
        MailMessage mail = new(Address, Address);
        mail.Subject = "MESOCYCLONE FEEDBACK (" + FeedbackVariable.Version + ")";
        mail.Body = FormatMessage(FeedbackVariable);
        if (FeedbackVariable.ScreenshotFilePath != null)
            mail.Attachments.Add(new Attachment(FeedbackVariable.ScreenshotFilePath));

        SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
        smtpServer.Port = 587;//GIVE CORRECT PORT HERE (either 587 or 465)
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
