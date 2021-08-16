using Aptify.Framework.Application;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Net;
using System.Collections.Specialized;
using System.Xml;
using Aptify.Framework.BusinessLogic.ProcessPipeline;

namespace ACSMyCMEFormDLLs.ProcessComponents
{
    [Serializable]
    [XmlRoot("rosters")]
    public class Rosters
    {
        [XmlAttribute] public int id_parent_provider;
        [XmlAttribute] public string upload_key;
        [XmlElement]
        public List<roster> roster { get;set; }
    }

    [Serializable]
    public class roster
    {
        public int id_provider;
        public int provider_course_code;

        public List<attendee> attendees { get; set; }
    }

    [Serializable]
    public class attendee
    {
        public string licensee_profession;
        public string license;
        public string cebroker_state; 
        public string first_name; 
        public string last_name; 
        public string date_completed;
        public List<partial_credit> partial_credits{ get; set; }
    }

    [Serializable]
    public class partial_credit
    {
        public string cd_profession; 
        public string cd_subject_area;
        public decimal partial_credit_hours;
    }

    public class ACSCMECEPersonBuildXML : IProcessComponent
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
        static string saveLocalPrefix = @"C:\Users\Public\Documents\";
        static string fileName = "XmlPersonCME" + DateTime.Now.ToString("yyyyMMdd_hhmm") + ".xml";
        DateTime dateGranted; 
        string saveLocation = saveLocalPrefix + fileName;
        string searchRecordSql;
        string searchBoardRecordSql;
        string searchBoardSubjectRecordSql;
        string searchBoardTranscriptRecordSql;
        private DataTable searchBoardTranscriptRecord;
        int boardId;
        string _eventStartDate;
        string _eventEndDate;
        string firstName;
        string lastName;
        string licenseNumber;
        string licenseeProfession;
        string state;
        string cdProfession;
        string cdSubjectArea;
        long personId;
        int eventId;
        int eventIdHolder;
        int childEventId;
        int parentId = 0;
        int thisCmeTypeId;
        decimal cmeType1;
        decimal totalCmeType1 = 0;
        decimal thisCmeType1 = 0;
        int attachmentCatId;
        int entityId;
        long attachId;
        long RecordId;
        string attachmentCatIdSql;
        string entityIdSql;
        byte[] data;
        private DataTable _recordSearchDT;
        private DataTable _recordBoardSubjectSearchDT;
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

        public void Config(AptifyApplication ApplicationObject)
        {
            try
            {
                m_oApp = ApplicationObject;
            }
            catch (Exception ex) 
            {
                ExceptionManager.Publish(ex);
            }
        }

        public AptifyProperties Properties
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
                m_sResult = "SUCCESS";
                RecordId = Convert.ToInt64(m_oProps.GetProperty("RecordId"));

                if (Convert.ToString(RecordId) != "")
                {
                    AcsCmeSendToBrokerGE = m_oApp.GetEntityObject("ACSCMESendToBroker", RecordId);
                }
                else
                {
                    AcsCmeSendToBrokerGE = (AptifyGenericEntityBase)m_oProps.GetProperty("AcsCmeSendToBrokerGE");  //this is our object being passed in when we save an acs cme event record.
                    RecordId  = Convert.ToInt64(AcsCmeSendToBrokerGE.GetValue("Id"));
                }

                personId = Convert.ToInt64(AcsCmeSendToBrokerGE.GetValue("PersonId"));
                firstName = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("PersonId_FirstName"));
                lastName = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("PersonId_LastName"));
                licenseNumber = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("LicenseNumber"));
                licenseeProfession = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("LicenseeProfession"));
                state = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("State"));
                _eventStartDate = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("EventStartDate"));
                _eventEndDate = Convert.ToString(AcsCmeSendToBrokerGE.GetValue("EventEndDate"));

                RecordSearch();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
                return "FAILED";
            }

            return m_sResult;
        }

        private void RecordSearch()
        {
            searchRecordSql = "select ID, ACSCMEEventId, CMEDateGranted, CMEType1 FROM ACSPersonCME WHERE acscmeeventid not in (Select ID from acscmeevent where name like ('%Claimed CME Credit%')) AND convert(date, CMEDateGranted, 120) >= convert(date, '" + _eventStartDate + "', 101) AND convert(date, CMEDateGranted, 120) <= convert(date, '" + _eventEndDate + "', 101) and PersonID = " + personId + " and CMEType1 > 0";
            _recordSearchDT = da.GetDataTable(searchRecordSql);
            if (_recordSearchDT.Rows.Count > 0) 
            {
                findSelectedRecords();
            }
        }
        private void findSelectedRecords()
        {
            try
            {
                var sql = "SELECT ProviderId, UploadKey FROM vwACSCMEDataBrokerReporter WHERE Active = 1";
                var dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                // Creates an instance of the XmlSerializer class;
                // specifies the type of object to serialize.
                XmlSerializer serializer = new XmlSerializer(typeof(Rosters));
                TextWriter writer = new StreamWriter(saveLocation);
                //Create Roster XML
                Rosters rosters = new Rosters();
                rosters.id_parent_provider = Convert.ToInt32(dt.Rows[0]["ProviderId"]);
                rosters.upload_key = Convert.ToString(dt.Rows[0]["UploadKey"]); //need to get the upload_key number from the CE Broker
                rosters.roster = new List<roster>();
                searchBoardRecordSql = "select distinct ACSCMEDataBrokerBoard_BoardId from vwACSCMEDataBrokerBoardSubject where ProfessionCode = '" + licenseeProfession + "' and ACSCMEDataBrokerBoard_AuthorizedState = '" + state + "'";
                boardId = Convert.ToInt32(m_oda.ExecuteScalar(searchBoardRecordSql));
                for (int x = 0; x < _recordSearchDT.Rows.Count; x++)
                {
                    eventId = Convert.ToInt32(_recordSearchDT.Rows[x]["ACSCMEEventId"]);
                    eventIdHolder = Convert.ToInt32(_recordSearchDT.Rows[x]["ACSCMEEventId"]);
                    dateGranted = Convert.ToDateTime(_recordSearchDT.Rows[x]["CMEDateGranted"]);
                    cmeType1 = Convert.ToDecimal(_recordSearchDT.Rows[x]["CMEType1"]);
                    totalCmeType1 = cmeType1;
                    thisCmeType1 = 0;
                    //loop through all records to see if child records exists and process them first
                    for (int y = 0; y < _recordSearchDT.Rows.Count; y++)
                    {
                        childEventId = Convert.ToInt32(_recordSearchDT.Rows[y]["ACSCMEEventId"]);
                        EventGE = (AptifyGenericEntityBase)m_oApp.GetEntityObject("ACSCMEEvent", childEventId);
                        parentId = Convert.ToInt32(EventGE.GetValue("ParentId"));
                        thisCmeTypeId = Convert.ToInt32(EventGE.GetValue("CmeTypeID"));
                        if (parentId == eventId)
                        {
                            searchBoardSubjectRecordSql = "select * from vwACSCMEDataBrokerBoardSubject where ACSCMEDataBrokerBoard_BoardId = " + boardId + " and ACSCMESubType_ID = " + thisCmeTypeId + " and Active = 'True'";
                            _recordBoardSubjectSearchDT = da.GetDataTable(searchBoardSubjectRecordSql);
                            if (_recordBoardSubjectSearchDT.Rows.Count > 0)
                            {
                                eventId = childEventId;
                                thisCmeType1 = Convert.ToDecimal(_recordSearchDT.Rows[y]["CMEType1"]);
                                totalCmeType1 -= thisCmeType1;
                                CreateXml(rosters);
                                if (totalCmeType1 <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if (totalCmeType1 > 0)
                    {
                        eventId = eventIdHolder;
                        EventGE = (AptifyGenericEntityBase)m_oApp.GetEntityObject("ACSCMEEvent", eventId);
                        thisCmeTypeId = Convert.ToInt32(EventGE.GetValue("CmeTypeID"));
                        if (thisCmeTypeId == 32 || thisCmeTypeId < 1)
                        {
                            thisCmeTypeId = 32;
                            thisCmeType1 = totalCmeType1;
                            CreateXml(rosters);
                        }
                    }
                }
                //Serializes the Courses, and closes the TextWriter.
                serializer.Serialize(writer, rosters);
                writer.Close();
                CreateAttachment();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        
        private void CreateXml(Rosters rosters) 
        {
            try
            {
                roster roster = new roster();
                roster.attendees = new List<attendee>();
                attendee attendee = new attendee();
                partial_credit partial_credit = new partial_credit(); 
                attendee.partial_credits = new List<partial_credit>();

                var exists = "n";
                var cmeMaxCredits = Convert.ToDecimal(EventGE.GetValue("cme_max_credits"));


                rosters.roster.Add(new roster
                {
                    id_provider = rosters.id_parent_provider,
                    provider_course_code = eventId,
                    attendees  = roster.attendees

                });

                if (thisCmeType1 < cmeMaxCredits)
                {                 
                    exists = "y";
                }

                searchBoardSubjectRecordSql = "select * from vwACSCMEDataBrokerBoardSubject where ACSCMEDataBrokerBoard_BoardId = " + boardId + " and ACSCMESubType_ID = " + thisCmeTypeId + " and Active = 'True'";
                _recordBoardSubjectSearchDT = da.GetDataTable(searchBoardSubjectRecordSql);

                if (_recordBoardSubjectSearchDT.Rows.Count > 0)
                {
                    for (int x = 0; x < _recordBoardSubjectSearchDT.Rows.Count; x++)
                    {
                        cdProfession = Convert.ToString(_recordBoardSubjectSearchDT.Rows[x]["ProfessionCode"]);
                        cdSubjectArea = Convert.ToString(_recordBoardSubjectSearchDT.Rows[x]["SubjectAreaCode"]);
                    }
                }
                else if (Convert.ToInt32(EventGE.GetValue("CmeTypeID")) == 32 || Convert.ToInt32(EventGE.GetValue("CmeTypeID")) < 1)
                {
                    searchBoardTranscriptRecordSql = "select * from vwACSCMEDataBrokerBoardSubject where ACSCMEDataBrokerBoard_BoardId = " + boardId + " and ACSCMESubType_Name = 'Transcript' and Active = 'True'";
                    searchBoardTranscriptRecord = da.GetDataTable(searchBoardTranscriptRecordSql);
                    if (searchBoardTranscriptRecord.Rows.Count > 0)
                    {
                        for (int x = 0; x < searchBoardTranscriptRecord.Rows.Count; x++)
                        {
                            cdProfession = Convert.ToString(searchBoardTranscriptRecord.Rows[x]["ProfessionCode"]);
                            cdSubjectArea = Convert.ToString(searchBoardTranscriptRecord.Rows[x]["SubjectAreaCode"]);
                        }
                    }
                }

                if (exists == "y")
                {
                    roster.attendees.Add(new attendee
                    {
                        licensee_profession = licenseeProfession,
                        license = licenseNumber,
                        cebroker_state = state,
                        first_name = firstName,
                        last_name = lastName,
                        date_completed = Convert.ToDateTime(dateGranted).ToString("MM/dd/yyyy"),
                        partial_credits = attendee.partial_credits
                    });
                    //roster.attendees.Add(new attendee
                    //{
                    //    partial_credits = attendee.partial_credits

                    //});
                    attendee.partial_credits.Add(new partial_credit
                    {
                        //cd_profession = licenseeProfession,
                        cd_profession = cdProfession,
                        cd_subject_area = cdSubjectArea,
                        partial_credit_hours = thisCmeType1
                    });
                }
                else
                {
                    //Create new element course
                    roster.attendees.Add(new attendee
                    {
                        licensee_profession = licenseeProfession,
                        license = licenseNumber,
                        cebroker_state = state,
                        first_name = firstName,
                        last_name = lastName,
                        date_completed = Convert.ToDateTime(dateGranted).ToString("MM/dd/yyyy")
                    });
                }
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
                xmlText = System.Text.Encoding.UTF8.GetString(data); 
                InXML = xmlText;
                m_oProps.SetProperty("XmlData",InXML);

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
       
        private void SaveForm()
        {
            try
            {
                //string newfilename;
                //newfilename = servicesurl + "/Acs/api/Attachment/GetAttachment?attachmentId=" + attachId;
                //xmlText = File.ReadAllText(saveLocation);
                InXML = xmlText;
                using (var wb = new WebClient())
                {
                    // xmlText = wb.DownloadString(newfilename);
                    //InXML = Convert.ToString(xmlText);
                    var xmlData = new NameValueCollection();
                    xmlData["InXML"] = InXML;

                    //wb.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    var response = wb.UploadValues(url + service, "POST", xmlData);
                    string responseInString = System.Text.Encoding.UTF8.GetString(response);

                    string responseInString1 = responseInString.Replace("&lt;", "\n<");
                    string responseInString2 = responseInString1.Replace("&gt;", ">");

                    xdoc = XDocument.Parse(responseInString2);
                }

                saveGE();
                // m_sResult = "SUCCESS";
                // RemoveLocalFile();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        
        private void RemoveLocalFile()
        {
            try
            {
                string FileToDelete;

                FileToDelete = saveLocation;

                if (System.IO.File.Exists(FileToDelete) == true)
                {
                    System.IO.File.Delete(FileToDelete);
                }
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
            // AcsCmeSendToBrokerGE.SetValue("XmlResponse", xdoc);
            AcsCmeSendToBrokerGE.SetValue("DateSent", Time);
            AcsCmeSendToBrokerGE.Save();

            if (!AcsCmeSendToBrokerGE.Save(false))
            {
                m_sResult = "FAILED";
                throw new Exception("Problem Saving AcsCmeSendToBrokerGE Record:" + AcsCmeSendToBrokerGE.RecordID);

            }
            else
            {
                AcsCmeSendToBrokerGE.Save(true);
                m_sResult = "SUCCESS";

            }
        }
    }
}
