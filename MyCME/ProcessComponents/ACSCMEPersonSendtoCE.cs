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
        public int id_course;

        public List<attendee> attendees { get; set; }
    }

    [Serializable]
    public class attendee
    {
        public string license_professional;
        public string license;
        public string cebroker_state; //event name
        public string first_name; //event cme_program
        public string last_name; //if eventType != Live then Anytime
        public string date_completed;
        public List<partial_credit> partial_credits{ get; set; }
    }

    [Serializable]
    public class partial_credit
    {
        public string cd_profession; //locate BoardId from ACSCMEDataBrokerBoard
        public string cd_subject_area;
        public decimal partial_credit_hours;
    }

    public class ACSCMEPersonSendtoCE : IProcessComponent
    {
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyProperties m_oProps = new AptifyProperties();
        private DataAction m_oda;

        private string m_sResult = "SUCCESS";
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
          
        public string Run() 
        {

            try
            {
                m_sResult = "SUCCESS"; 
               

                long RecordId = 0;

                AcsCmeSendToBrokerGE = (AptifyGenericEntityBase)m_oProps.GetProperty("AcsCmeSendToBrokerGE");  //this is our object being passed in when we save an acs cme event record.
                RecordId = Convert.ToInt64(AcsCmeSendToBrokerGE.GetValue("Id"));
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
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
                return "FAILED";
            }
            return m_sResult;
        }
        private void RecordSearch()
        {
            searchRecordSql = "select ID, ACSCMEEventId, CMEDateGranted, CMEType1 FROM ACSPersonCME WHERE convert(date, CMEDateGranted, 120) >= convert(date, '" + _eventStartDate + "', 101) AND convert(date, CMEDateGranted, 120) <= convert(date, '" + _eventEndDate + "', 101) and PersonID = " + personId;
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

                //foreach (DataRow dr in _recordSearchDT.Rows)
                //{
                //    eventId = Convert.ToInt32(_recordSearchDT.Rows[0]["ACSCMEEventId"]);
                //    CreateXml(rosters);
                //}
                for (int x = 0; x < _recordSearchDT.Rows.Count; x++)
                {
                    eventId = Convert.ToInt32(_recordSearchDT.Rows[x]["ACSCMEEventId"]);
                    dateGranted = Convert.ToDateTime(_recordSearchDT.Rows[x]["CMEDateGranted"]);
                    cmeType1 = Convert.ToDecimal(_recordSearchDT.Rows[x]["CMEType1"]);
                    CreateXml(rosters);
                }
                    //Serializes the Courses, and closes the TextWriter.
                    serializer.Serialize(writer, rosters);
                writer.Close();
                //CreateAttachment();
                //CreateRecordSent();
                //MessageBox.Show("Selected Values" + data);

            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
        private void CreateXml(Rosters rosters) //Need EventId pulled in to get the event information for processing
        {

            try
            {
                roster roster = new roster();
                roster.attendees = new List<attendee>();
                attendee attendee = new attendee();
                partial_credit partial_credit = new partial_credit();
                attendee.partial_credits = new List<partial_credit>();
                


                EventGE = (AptifyGenericEntityBase)m_oApp.GetEntityObject("ACSCMEEvent", eventId);
                var cmeMaxCredits = Convert.ToDecimal(EventGE.GetValue("cme_max_credits"));
                rosters.roster.Add(new roster
                {
                    id_provider = rosters.id_parent_provider,
                    id_course = eventId,
                    attendees  = roster.attendees

                });
                //Create new element course
                roster.attendees.Add(new attendee
                {
                    license_professional = licenseeProfession,
                    license = licenseNumber,
                    cebroker_state = state,
                    first_name = firstName,
                    last_name = lastName,
                    date_completed = Convert.ToString(dateGranted)
                    
                  
                 });
                if (cmeType1 < cmeMaxCredits)
                {
                    roster.attendees.Add(new attendee
                    {
                        partial_credits = attendee.partial_credits
                    });

                }
                attendee.partial_credits.Add(new partial_credit
                {
                      cd_profession = "DN",
                      cd_subject_area = "GN",
                      partial_credit_hours = cmeType1
                   });

                //  CreatePartialCredit(attendees, cmeMaxCredits);
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
                //need to get the file that gets created and read it back into

                fileName = Path.GetFileName(saveLocation);
                data = File.ReadAllBytes(saveLocation);

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

                //SaveForm();

            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }
    }
}
