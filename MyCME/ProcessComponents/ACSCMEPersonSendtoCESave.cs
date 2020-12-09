using Aptify.Framework.Application;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using Aptify.Framework.WindowsControls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net.Http;
using System.Net;
using System.Collections.Specialized;
using System.Xml;
using System.Text;
using System.Web;
using Aptify.Framework.BusinessLogic.ProcessPipeline;

namespace ACSMyCMEFormDLLs.ProcessComponents
{
       public class ACSCMEPersonSendtoCESave : IProcessComponent
    {
        private AptifyApplication m_oApp = new AptifyApplication();
        
        private AptifyProperties m_oProps = new AptifyProperties();
        private DataAction m_oda;

        private string m_sResult = "SUCCESS";
        public string InXML = "";
        private string url = "";
        private string service = "";
        AptifyGenericEntityBase AcsCmeSendToBrokerGE;
        AptifyGenericEntityBase AttachmentsGE;
        AptifyGenericEntityBase EventGE;
        static string saveLocalPrefix = "C:\\Users\\Public\\Documents\\";
        static string fileName = "XmlPersonCME" + DateTime.Now.ToString("yyyyMMdd_hhmm") + ".xml";
        DateTime dateGranted;
        string saveLocation = saveLocalPrefix + fileName;
        string searchRecordSql;
        string _eventStartDate;
        string _eventEndDate;
        string firstName;
        string lastName;
        string licenseNumber;
        string licenseeProfession;
        string state;
        long personId;
        int eventId;
        decimal cmeType1;
        int attachmentCatId;
        int entityId;
        long attachId;
        long RecordId;

        string attachmentCatIdSql;
        string entityIdSql;
        byte[] data;
        private DataTable _recordSearchDT;
        DataAction da = new DataAction();
        DateTime Time = DateTime.Now;
        Rosters rosters = new Rosters();
        XDocument xdoc = new XDocument();
        String xmlText;
        public virtual DataAction DataAction  
        {
            get
            {
                if (m_oda == null)
                {
                    m_oda = new DataAction(m_oApp.UserCredentials);
                }
               return m_oda;
            }
        }
        public virtual AptifyApplication Application
        {
            get { return m_oApp; }
        }

        public void Config(Aptify.Framework.Application.AptifyApplication ApplicationObject)
        {
            try
            {
                m_oApp = ApplicationObject;
            }
            catch (Exception ex) 
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
            }
        }

        public Aptify.Framework.Application.AptifyProperties Properties
        {
            get { return m_oProps; }
        }

        /// Result Codes:
        /// SUCCESS, FAILED
        /// 

          
        public string Run() 
        {
            if (da.UserCredentials.Server.ToLower() == "aptify")
            {

            }
            if (da.UserCredentials.Server.ToLower() == "stagingaptify61")
            {

            }
            if (da.UserCredentials.Server.ToLower() == "testaptify610")
            {
                url = "https://test.webservices.cebroker.com/";
                service = "CEBrokerWebService.asmx/UploadXMLString";
            }
            try
            {
                m_sResult = "SUCCESS"; 
               

                //long RecordId = 0;

                //AcsCmeSendToBrokerGE = (AptifyGenericEntityBase)m_oProps.GetProperty("AcsCmeSendToBrokerGE");  //this is our object being passed in when we save an acs cme event record.
                //RecordId = Convert.ToInt64(AcsCmeSendToBrokerGE.GetValue("Id"));
                RecordId = Convert.ToInt64(m_oProps.GetProperty("RecordId"));
                SaveForm();
               


            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
                return "FAILED";
            }
           
            return m_sResult;
        }
      
        
       
        private void SaveForm()
        {
            try
            {
                //xmlText = File.ReadAllText(saveLocation);
                xmlText = Convert.ToString(m_oProps.GetProperty("XmlData"));
                //AcsCmeSendToBrokerGE = (AptifyGenericEntityBase)m_oApp.GetEntityObject("ACSCMESendToBroker", RecordId);
                //xdoc = new XDocument();

                InXML = Convert.ToString(xmlText);

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["InXML"] = InXML;

                    //wb.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    var response = wb.UploadValues(url + service, "POST", data);
                    string responseInString = System.Text.Encoding.UTF8.GetString(response);

                    string responseInString1 = responseInString.Replace("&lt;", "\n<");
                    string responseInString2 = responseInString1.Replace("&gt;", ">");


                    xdoc = XDocument.Parse(responseInString2);


                    //string toFind1 = "ErrorCode=\"";
                    //string toFind2 = "\" Message";

                    //string str;
                    //string[] strArr;
                    //int i;

                    //str = responseInString2;
                    //char[] splitchar = { '\n' };
                    //strArr = str.Split(splitchar);
                    //for (i = 0; i <= strArr.Length - 1; i++)
                    //{
                    //    if (strArr[i].Contains("ErrorCode=\""))
                    //    {
                    //        int start = strArr[i].IndexOf(toFind1) + toFind1.Length;
                    //        int end = strArr[i].IndexOf(toFind2, start); //Start after the index of 'my' since 'is' appears twice
                    //        string ErrorCode = strArr[i].Substring(start, end - start);

                    //        if (ErrorCode != "")
                    //        {

                    //        }
                    //    }
                    //}
                   

                }
                saveGE();

                m_sResult = "SUCCESS";
               // saveGE();

                // RemoveLocalFile();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        private void saveGE()
        {
           AcsCmeSendToBrokerGE = m_oApp.GetEntityObject("ACSCMESendToBroker", RecordId);
           AcsCmeSendToBrokerGE.SetValue("XmlData", Convert.ToString(xmlText));
           AcsCmeSendToBrokerGE.SetValue("XmlResponse", xdoc);
            //AcsCmeSendToBrokerGE.Save();
            //if (!AcsCmeSendToBrokerGE.Save(false))
            //{
            //    m_sResult = "FAILED";
            //    throw new Exception("Problem Saving attachments Record:" + AcsCmeSendToBrokerGE.RecordID);

            //}
            //else
            //{
            //    AcsCmeSendToBrokerGE.Save(true);

            //    m_sResult = "SUCCESS";
            //}
        }
    }
}
