using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Configuration;
using System.Threading;

namespace CheckMissingComp
{
  
    class Program
    {

        static string MailSubject = ConfigurationManager.AppSettings["MailSubject"];
        static string notifyFrom = ConfigurationManager.AppSettings["notifyFrom"];
        static string notifyTo = ConfigurationManager.AppSettings["NotifyTo"];
        static string notifyTo_Page = ConfigurationManager.AppSettings["notifyTo_Page"];
        static string NotifyOwner = ConfigurationManager.AppSettings["NotifyOwner"];
        static void Main(string[] args)
        {try
            {
                CheckCTSDB();
            }
            catch (Exception e)
            {
                SendEmailNotification(e.ToString(), notifyFrom, notifyTo, MailSubject);

            }
        }


        /// <summary>
        /// Connect to TBMSSPROD410
        /// </summary>
        public static SqlConnection ConnectCTS
        {

            get
            {
                SqlConnection conn = new SqlConnection();
                try
                {
                    conn.ConnectionString = "Data Source=tbmssprod55.tatw.micron.com,52404;Initial Catalog=substrate_mapping;User id=WW_MAPPING;Password=sillyuncle";
                    return conn;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return conn;

                }
            }
        }
        public static void CheckCTSDB()
        {
            string Lot = "";
            var queryStr = string.Format(@"
           declare @Start_TIME datetime
            declare @End_TIME datetime
            set @End_TIME  = GETDATE()
            set @Start_TIME  = dateadd(day,-90,getdate())
            select [lot_id],count([device_oid]) as Total_Device_Id
            from [substrate_mapping].[dbo].[device_lot_assoc] 
            where [device_oid] in (
	              SELECT [device_oid]
                  FROM [substrate_mapping].[dbo].[device]
                  where [substrate_oid] in (
			              SELECT    [substrate_oid]
			              FROM [substrate_mapping].[dbo].[substrate]
			              where [frame_id] in (
				                 SELECT [frame_id]
					             FROM [substrate_mapping].[dbo].[substrate]
					             where [status_code] = 'A' and [insert_datetime] > @Start_TIME and [insert_datetime] < @End_TIME and [frame_id] != ''
					             group by [frame_id]
					             Having count(*) > 1
			              )
	              )
  
            )
            group by [lot_id]
            Having count([device_oid])>1
            order by [lot_id]
        ");
            int count = 0;
            using (var conn = ConnectCTS)
            {
                
                conn.Open();
                SqlCommand cmd = new SqlCommand(queryStr, conn);
                cmd.CommandTimeout = 500;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                           count++;
                           Lot += "<p>" + reader["lot_id"].ToString() + "</p>";
                       

                    }
                }
               conn.Close();

            }
            string msg = "<p>There are " + count + " lots need to manually delete duplicate device_oid:</p>" + Lot + "<p>Reference SOE:'[Issue]: HBM Delete duplicate Scribe on one Wafer' in onenote</p>";

            Console.WriteLine(msg);
            Thread.Sleep(12000);
            //if (count > 0)
            //{
            //    SendEmailNotification(msg, notifyFrom, notifyTo, MailSubject );
            //    SendEmailNotification(msg, notifyFrom, notifyTo_Page, "Missing CompID :There are " + count + " strips need to manually create compID, please check the mail for detail.");
            //}
            //else SendEmailNotification("No Strip need to create Comp ID", notifyFrom, notifyTo, MailSubject);
          
        }

        static public void SendEmailNotification(string msg, string notifyFrom, string notifyTo, string subject)
        {
            if (notifyTo.EndsWith(","))
                notifyTo = notifyTo;
            else
                notifyTo = notifyTo;
            MailMessage mailObj = new MailMessage(notifyFrom, notifyTo, subject, msg);
            mailObj.IsBodyHtml = true;
            
            SmtpClient SMTPServer = new SmtpClient("relay.micron.com");
            SMTPServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            SMTPServer.Send(mailObj);
           
        }

    }
}
