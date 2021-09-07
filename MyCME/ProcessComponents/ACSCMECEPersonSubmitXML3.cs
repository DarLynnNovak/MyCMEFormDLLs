using Aptify.Framework.Application;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.BusinessLogic.ProcessPipeline;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Net;
using System.Xml.Linq;
using System.Net.Http;
using System.IO;

namespace ACSMyCMEFormDLLs.ProcessComponents
{
    public class ACSCMECEPersonSubmitXML3 : IProcessComponent
    {
        private AptifyApplication m_oApp = new AptifyApplication();
        AptifyGenericEntityBase AcsCmeSendToBrokerGE;
        AptifyGenericEntityBase AcsCmePersonSendToBrokerGE;
        private AptifyProperties m_oProps = new AptifyProperties();
        private DataAction m_oda;
        private AptifyGenericEntityBase AttachmentsGE;
        private string m_sResult = "SUCCESS";
        public string InXML = "";
        private string url = "";
        private string service = "";
        string errorMessages;
        string successMessages;
        string hasErrors;
        string responseInString2;
        string ErrorCode;
        string ErrorMes;
        string result = "FAILED";
        string personEmail;
        string submissionState;
        string cmdCredSql;
        long ceBrokerLearnerId;
        long RecordId;
        long MRId;
        long attachId;
        int eventId;
        byte[] data;
        decimal partCred;
        decimal fullCred;
        decimal creditsClaimedAmount;
        long PersonId;
        int errorId;
        DataAction da = new DataAction();
        DateTime Time = DateTime.Now;
        DateTime CurrentDate = DateTime.Now;
        Rosters rosters = new Rosters();
        XDocument xdoc = new XDocument();
        String xmlText;
        static string saveLocalPrefix = @"C:\Users\Public\Documents\";
        static string fileName = "XmlPersonCMEResp" + DateTime.Now.ToString("yyyyMMdd_hhmm") + ".xml";
        string saveLocation = saveLocalPrefix + fileName;
        string attachmentCatIdSql;
        string entityIdSql;
        int attachmentCatId;
        int entityId;


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

            try
            {

                m_oProps.GetProperty("XmlData");
                SaveForm();
               
            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
                m_sResult = "FAILED";
            }

            return m_sResult;
        }



        private void SaveForm()
        {
            try
            {
                 
                xmlText = Convert.ToString(m_oProps.GetProperty("XmlData"));
                RecordId = Convert.ToInt32(m_oProps.GetProperty("RecordId")); 
                AcsCmeSendToBrokerGE = m_oApp.GetEntityObject("ACSCMESendToBroker", RecordId);
                PersonId = Convert.ToInt32(AcsCmeSendToBrokerGE.GetValue("PersonId"));
                submissionState = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("State"));
                personEmail = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("Email"));

                url = Convert.ToString(m_oProps.GetProperty("url"));
                service = Convert.ToString(m_oProps.GetProperty("service"));
                InXML = Convert.ToString(xmlText);

                
                    //adding in saving credit to the learner events being saved

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["InXML"] = InXML;
                    var response = wb.UploadValues(url + service, "POST", data);
                    string responseInString = System.Text.Encoding.UTF8.GetString(response);
                    string responseInString1 = responseInString.Replace("&lt;", "\n<");
                    responseInString2 = responseInString1.Replace("&gt;", ">");
                    xdoc = XDocument.Parse(responseInString2);


                    string toFind1 = "ErrorCode=\"";
                    string toFind2 = "\"";
                    string toFind3 = "message=\""; 
                    string toFind4 = "\"";
                    string toFind5 = "provider_course_code=\"";
                    string toFind6 = "\"";
                    string str;
                    string[] strArr;
                    int i;

                    str = responseInString2;
                    char[] splitchar = { '\n' };
                    strArr = str.Split(splitchar);
                    errorMessages = "";
                   // errorMessages = "<table><tr><td>CE Broker Person Submission Errors For Record: " + RecordId + "</td></tr></table>";
              
                    for (i = 0; i <= strArr.Length - 1; i++)
                    {
                        if (strArr[i].Contains("provider_course_code=\""))
                        {
                            int eventStart = strArr[i].IndexOf(toFind5) + toFind5.Length;
                            int eventEnd = strArr[i].IndexOf(toFind6, eventStart);
                            eventId = Convert.ToInt32(strArr[i].Substring(eventStart, eventEnd - eventStart));
                            if (eventId > 0)
                            {
                                checkPartialCredit();
                                UpdateErrorMessage();
                            }

                        }

                        if (strArr[i].Contains("ErrorCode=\""))
                        {

                            int start = strArr[i].IndexOf(toFind1) + toFind1.Length;
                            int end = strArr[i].IndexOf(toFind2, start);
                            ErrorCode = strArr[i].Substring(start, end - start);

                            if (ErrorCode != "")
                            {
                                var errorIdSql = "select ID from acscmecebrokererrors where errorcode = " + ErrorCode;
                                errorId = Convert.ToInt32(m_oda.ExecuteScalar(errorIdSql));

                                int startmes = strArr[i].IndexOf(toFind3) + toFind3.Length;
                                int endmes = strArr[i].IndexOf(toFind4, startmes);
                                ErrorMes = strArr[i].Substring(startmes, endmes - startmes);
                                checkPartialCredit();
                                
                                errorMessages = errorMessages + "<table><tr><td>Event:  " + eventId + "</td><td> Error Code:  " + ErrorCode + "</td><td> Error Message:  " + ErrorMes + "</td></tr></table>";
                                hasErrors = "TRUE";
                                UpdateErrorMessage();
                            }
                            if (ErrorCode == "")
                            {
                                hasErrors = "FALSE";
                                UpdateErrorMessage();
                            }

                        }

                    }
                    xdoc.Save(saveLocation);
                    CreateAttachment();
                    saveStatus();
                    saveGE();


                } 

                // RemoveLocalFile();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        private void CreateAttachment()
        {
            try
            {
                entityIdSql = "select ID from Entities where name like 'ACSCMESendToBroker'";
                entityId = Convert.ToInt32(m_oda.ExecuteScalar(entityIdSql));
                attachmentCatIdSql = "select ID from vwAttachmentCategories where name like 'MyCMEXML'";
                attachmentCatId = Convert.ToInt32(m_oda.ExecuteScalar(attachmentCatIdSql));

                fileName = Path.GetFileName(saveLocation);
                data = File.ReadAllBytes(saveLocation);
                //xmlText = System.Text.Encoding.UTF8.GetString(data);
                //InXML = xmlText;
                //m_oProps.SetProperty("XmlData", InXML);

                AttachmentsGE = m_oApp.GetEntityObject("Attachments", -1);
                AttachmentsGE.SetValue("Name", fileName);
                AttachmentsGE.SetValue("Description", "XMLCEData");
                AttachmentsGE.SetValue("EntityID", entityId);
                AttachmentsGE.SetValue("RecordID", RecordId);
                AttachmentsGE.SetValue("CategoryID", attachmentCatId);
                AttachmentsGE.SetValue("LocalFileName", saveLocation);
                AttachmentsGE.SetValue("BlobData", data);

                if (!AttachmentsGE.Save(false))
                {
                    m_sResult = "FAILED";
                    throw new Exception("Problem Saving attachments Record:" + AttachmentsGE.RecordID);
                }
                else
                {
                    AttachmentsGE.Save(true);
                    m_sResult = "SUCCESS";
                    attachId = AttachmentsGE.RecordID;
                }
                if (m_sResult == "SUCCESS")
                {
                    SaveAttachmentBlob();
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        public void SaveAttachmentBlob()
        {
            try
            {
                var dp = new IDataParameter[2];
                dp[0] = m_oda.GetDataParameter("@ID", SqlDbType.BigInt, attachId);
                dp[1] = m_oda.GetDataParameter("@BLOBData", SqlDbType.Image, data.Length, data);
                m_oda.ExecuteNonQueryParametrized("Aptify.dbo.spInsertAttachmentBlob", CommandType.StoredProcedure, dp);

                saveGE();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        private void checkPartialCredit()
        {
            try
            {
                string partCredStart = "<partial_credit_hours>";
                string partCredEnd = "</partial_credit_hours>";


                string credStr;
                string[] credStrArr;
                int a;

                credStr = InXML;
                char[] splitchar1 = { '\n' };
                credStrArr = credStr.Split(splitchar1);
                for (a = 0; a <= credStrArr.Length - 1; a++)
                {
                    if (credStrArr[a].Contains(partCredStart))
                    {
                        int partialCredStart = credStrArr[a].IndexOf(partCredStart) + partCredStart.Length;
                        int partialCredEnd = credStrArr[a].IndexOf(partCredEnd, partialCredStart);
                        partCred = Convert.ToDecimal(credStrArr[a].Substring(partialCredStart, partialCredEnd - partialCredStart));
                        //UpdateErrorMessage();
                    }

                }
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }

        }
        private void UpdateErrorMessage()
        {

            try
            {
                var sql = "select * from ACSCMEPersonCEBrokerSubmissions where ACSCMEEventId = " + eventId + " and PersonId = " + PersonId + " and ACSCMESendToBrokerId = " + RecordId;
                var dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                if (partCred > 0)
                {
                    creditsClaimedAmount = partCred;
                }
                else
                {
                    cmdCredSql = "select CME_Max_Credits from acscmeevent where id =" + eventId;
                    fullCred = Convert.ToDecimal(m_oda.ExecuteScalar(cmdCredSql));
                    creditsClaimedAmount = fullCred;
                }

                //need to get the file that gets created and read it back into

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        

                        if (hasErrors == "TRUE")
                        {
                            AcsCmePersonSendToBrokerGE = m_oApp.GetEntityObject("ACSCMEPersonCEBrokerSubmissions", Convert.ToInt64(dt.Rows[0]["ID"].ToString()));
                            //AcsCmePersonSendToBrokerGE.SetValue("ACSCMESendToBrokerId", RecordId);
                            AcsCmePersonSendToBrokerGE.SetValue("ErrorCode", ErrorCode);
                            AcsCmePersonSendToBrokerGE.SetValue("ReturnErrorDesc", ErrorMes);
                            AcsCmePersonSendToBrokerGE.SetValue("Status", "HAS ERRORS");
                            // AcsCmePersonSendToBrokerGE.SetValue("CreditsClaimedAmount", 0.00);
                            creditsClaimedAmount = 0;

                        }
                        if (hasErrors == "FALSE")
                        {
                            AcsCmePersonSendToBrokerGE = m_oApp.GetEntityObject("ACSCMEPersonCEBrokerSubmissions", Convert.ToInt64(dt.Rows[0]["ID"].ToString()));
                            AcsCmePersonSendToBrokerGE.SetValue("ErrorCode", "");
                            AcsCmePersonSendToBrokerGE.SetValue("ReturnErrorDesc", "");
                            AcsCmePersonSendToBrokerGE.SetValue("Status", "SUBMITTED");
                            
                        }
                    }
                }
                else
                {

                    if (hasErrors == "TRUE")
                    {
                        AcsCmePersonSendToBrokerGE = m_oApp.GetEntityObject("ACSCMEPersonCEBrokerSubmissions", -1);
                        AcsCmePersonSendToBrokerGE.SetValue("ACSCMESendToBrokerId", RecordId);
                        AcsCmePersonSendToBrokerGE.SetValue("PersonId", PersonId);
                        AcsCmePersonSendToBrokerGE.SetValue("ACSCMEEventId", eventId);
                        AcsCmePersonSendToBrokerGE.SetValue("ErrorCode", ErrorCode);
                        AcsCmePersonSendToBrokerGE.SetValue("ReturnErrorDesc", ErrorMes);
                        AcsCmePersonSendToBrokerGE.SetValue("Status", "HAS ERRORS");
                        creditsClaimedAmount = 0;

                    }
                    if (hasErrors == "FALSE")
                    {
                        AcsCmePersonSendToBrokerGE = m_oApp.GetEntityObject("ACSCMEPersonCEBrokerSubmissions", -1);
                        AcsCmePersonSendToBrokerGE.SetValue("ACSCMESendToBrokerId", RecordId);
                        AcsCmePersonSendToBrokerGE.SetValue("PersonId", PersonId);
                        AcsCmePersonSendToBrokerGE.SetValue("ACSCMEEventId", eventId);
                        AcsCmePersonSendToBrokerGE.SetValue("ErrorCode", "");
                        AcsCmePersonSendToBrokerGE.SetValue("ReturnErrorDesc", "");
                        AcsCmePersonSendToBrokerGE.SetValue("Status", "SUBMITTED");
                        
                    }

                       
                }
                AcsCmePersonSendToBrokerGE.SetValue("CreditsClaimedAmount", creditsClaimedAmount);

                if (!AcsCmePersonSendToBrokerGE.Save(false))
                {
                    result = "FAILED";
                    throw new Exception("Problem Saving data Record:" + AcsCmePersonSendToBrokerGE.RecordID);

                }
                else
                {
                    AcsCmePersonSendToBrokerGE.Save(true);
                    result = "SUCCESS";

                }
                ceBrokerLearnerId = AcsCmePersonSendToBrokerGE.RecordID;

            }

            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        private void saveStatus()
        {
            if (hasErrors == "TRUE")
            {

               
                //successMessages = "<table><tr><td>Your ACS CME credit submission to " + submissionState + " via CE Broker was <b>not successful</b>. Please contact the MyCME Team at mycme@facs.org or 866-918-479 to resolve any errors and resubmit.</td></tr></table>";
                successMessages = "<table><tr><td>A portion of your ACS CME credit submission to " + submissionState + " via CE Broker was <b>not successful</b>. Please contact the MyCME Team at mycme@facs.org or 866-918-4799, and they will work to identify and resolve the issue.  Please do not attempt to resend.</td></tr></table>";
                createMessageRun();
                createMessageRunToLearner();
            }
            else
            {
                
                successMessages = "<table><tr><td>Your ACS CME credit submission to " + submissionState + " via CE Broker was <b>successful</b>. Please contact the MyCME Team at mycme@facs.org or 866-918-479 with any questions. </td></tr></table>";
                createMessageRunToLearner();
            }

        }
        private void createMessageRun()
        {

            try
            {

                AptifyGenericEntityBase messageRunGe;
                messageRunGe = m_oApp.GetEntityObject("Message Runs", -1); 
                {

                    messageRunGe.SetValue("MessageSystemID", 6);
                    messageRunGe.SetValue("MessageSourceID", 2);
                    messageRunGe.SetValue("MessageTemplateID", m_oProps.GetProperty("MessageTemplateId"));
                    messageRunGe.SetValue("ApprovalStatus", "Approved");
                    messageRunGe.SetValue("Status", "Pending");
                    messageRunGe.SetValue("ScheduledStartDate", CurrentDate);
                    messageRunGe.SetValue("Priority", "Normal");
                    messageRunGe.SetValue("ToType", "Static");
                    messageRunGe.SetValue("ToValue", "dnovak@facs.org");
                    messageRunGe.SetValue("CCType", "Static");
                    messageRunGe.SetValue("CCValue", "mycme@facs.org");
                    messageRunGe.SetValue("RecipientCount", 0);
                    messageRunGe.SetValue("SourceType", "StaticSingle");
                    messageRunGe.SetValue("IDString", "3096875");
                    messageRunGe.SetValue("HTMLBody", errorMessages);
                    messageRunGe.SetValue("Subject", "CE Broker Person Submission Errors for Record #: " + RecordId);
                }



                if (messageRunGe.IsDirty)
                {
                    if (!messageRunGe.Save(false))
                    {

                        m_sResult = "FAILED";
                        throw new Exception("Problem Saving Course Record:" + messageRunGe.RecordID);
                    }
                    else
                    {
                        messageRunGe.Save(true);
                        m_sResult = "SUCCESS";
                        MRId = messageRunGe.RecordID;
                    }
                }
            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
            }
        }
        private void createMessageRunToLearner()
        {

            try
            {

                AptifyGenericEntityBase messageRunGe;
                messageRunGe = m_oApp.GetEntityObject("Message Runs", -1);
                {

                    messageRunGe.SetValue("MessageSystemID", 6);
                    messageRunGe.SetValue("MessageSourceID", 2);
                    messageRunGe.SetValue("MessageTemplateID", m_oProps.GetProperty("MessageTemplateId"));
                    messageRunGe.SetValue("ApprovalStatus", "Approved");
                    messageRunGe.SetValue("Status", "Pending");
                    messageRunGe.SetValue("ScheduledStartDate", CurrentDate);
                    messageRunGe.SetValue("Priority", "Normal");
                    messageRunGe.SetValue("ToType", "Static");
                    messageRunGe.SetValue("ToValue",personEmail);
                    messageRunGe.SetValue("CCType", "Static");
                    messageRunGe.SetValue("CCValue", "mycme@facs.org");
                    messageRunGe.SetValue("RecipientCount", 0);
                    messageRunGe.SetValue("SourceType", "StaticSingle");
                    messageRunGe.SetValue("IDString",PersonId);
                    messageRunGe.SetValue("HTMLBody", successMessages);
                    messageRunGe.SetValue("Subject", "CE Broker Submission");
                }



                if (messageRunGe.IsDirty)
                {
                    if (!messageRunGe.Save(false))
                    {

                        m_sResult = "FAILED";
                        throw new Exception("Problem Saving Course Record:" + messageRunGe.RecordID);
                    }
                    else
                    {
                        messageRunGe.Save(true);
                        m_sResult = "SUCCESS";
                        MRId = messageRunGe.RecordID;
                    }
                }
            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
            }
        }
        private void saveGE()
        {
            AcsCmeSendToBrokerGE = m_oApp.GetEntityObject("ACSCMESendToBroker", RecordId);
            if (hasErrors == "TRUE")
            {
                AcsCmeSendToBrokerGE.SetValue("Status", "HAS ERRORS");
            }
            else
            {
                AcsCmeSendToBrokerGE.SetValue("Status", "SUBMITTED");
            }
            AcsCmeSendToBrokerGE.SetValue("XmlResponse", xdoc);

            if (!AcsCmeSendToBrokerGE.Save(false))
            {
                m_sResult = "FAILED";
                throw new Exception("Problem Saving attachments Record:" + AcsCmeSendToBrokerGE.RecordID);

            }
            else
            {
                AcsCmeSendToBrokerGE.Save(true);

                m_sResult = "SUCCESS";
                //CreateRecordSent();
            }

        }
        

    }

}
