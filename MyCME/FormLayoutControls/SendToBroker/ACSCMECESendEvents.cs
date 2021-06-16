using Aptify.Framework.Application;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using Aptify.Framework.WindowsControls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ACSMyCMEFormDLLs.FormLayoutControls.SendToBroker
{


    [Serializable]
    [XmlRoot("courses")]
    public class Courses
    {
        [XmlAttribute] public int id_parent_provider;
        [XmlAttribute] public string upload_key;
        [XmlElement]
        public List<course> course { get; set; }
    }

    [Serializable]
    public class course
    {
        public int id_provider;
        public int provider_course_code; 
        public string nm_course; //event name
        public string ds_course; //event cme_program
        public string cd_course_type; //if eventType != Live then Anytime
        public string series;
        public string modular;
        public string concurrent;
        public string cd_delivery_method; //Education needs to define this for us and start tracking in the event
        public string course_process; //COURSEXML = new course; RESUBMITHRSXML = new effective date on an existing course
        public string dt_start;
        public string dt_end;
        //public DateTime dt_start; //cme_start_date goes here
        //public DateTime dt_end; //cme_end_date goes here unless null, then cme_start_date goes here
        public List<board> course_board { get; set; }
    }

    [Serializable]
    public class board
    {
        public int id_board; //locate BoardId from ACSCMEDataBrokerBoard
        public List<component> board_component { get; set; }
    }

    [Serializable]
    public class component
    {
        public string cd_subject_area; //locate SubjectAreaCode from ACSCMEDataBrokerBoardSubject
        public decimal am_app_hours; // CME_Max_Credits from ACSCMEEvent 
        public string cd_profession; //MD or DO depending on what doctor is
    }

    public class ACSCMECESendEvents : FormTemplateLayout 
    {
        private AptifyProperties m_oProps = new AptifyProperties();
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyGenericEntityBase AttachmentsGE;
        private AptifyGenericEntityBase BrokerDataGE;
        private AptifyLinkBox _senderIdLinkBox;
        private AptifyActiveButton _sendToBrokerBtn;
        private AptifyTextBox _eventStartDate;
        private AptifyTextBox _eventEndDate;
        private AptifyTextBox _status;
        private System.Windows.Forms.Form _parentForm;
        private DataAction m_oda = new DataAction();
        private DataGridView grdRecordSearch;
        private DataTable _recordSearchDT;
        private bool _boolAdded;
        DateTime CurrentDate = DateTime.Now;
        public CheckBox HeaderCheckBox { get; private set; }
        private AptifyGenericEntityBase EventGE;
        private string url = "";
        private string service = "";
        private static readonly HttpClient client = new HttpClient();
        public string InXML = "";
        byte[] data;
        CheckBox headerCheckBox = new CheckBox();
        DataGridViewCheckBoxCell recordCheckBox = new DataGridViewCheckBoxCell();
        XDocument xDoc = new XDocument();
        static string saveLocalPrefix = @"C:\Users\Public\Documents\";
        static string fileName = "XmlEventCourses" + DateTime.Now.ToString("yyyyMMdd_hhmm") + ".xml";
        string attachmentCatIdSql;
        string entityIdSql;
        string result = "FAILED";
        string saveLocation = saveLocalPrefix + fileName;
        string searchRecordSql; 
        string senderIdSql;
        string errorMessages;
        string hasErrors;
        string ErrorCode; 
        string status;
        string ErrorMes;
        int attachmentCatId;
        int eventId;
        string eId;
        int entityId;
        int senderId;
        long attachId;
        long recordId;
        long userId;
        long MRId;
        string sql;
        DataTable dt;

        public void Config()
        {
            try
            {
                m_oApp = ApplicationObject;
                this.m_oda = new Aptify.Framework.DataServices.DataAction(this.m_oApp.UserCredentials);
                userId = m_oda.UserCredentials.AptifyUserID;

                if (m_oda.UserCredentials.Server.ToLower() == "aptify")
                {

                }
                if (m_oda.UserCredentials.Server.ToLower() == "stagingaptify61")
                {
                    //url = "https://test.webservices.cebroker.com/";
                    //service = "CEBrokerWebService.asmx/UploadXMLString";
                }
                if (m_oda.UserCredentials.Server.ToLower() == "testaptify610")
                {
                    //url = "https://test.webservices.cebroker.com/";
                    //service = "CEBrokerWebService.asmx/UploadXMLString";
                }

            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
            }
        }

        protected override void OnFormTemplateLoaded(FormTemplateLoadedEventArgs e)
        {
            base.OnFormTemplateLoaded(e);
            try
            {
                if (e.Operation == FormTemplateLoadedOperation.LoadTemplate)
                {
                    Config();
                    BindControls();
                    getSenderId();
                    if (grdRecordSearch is null)
                    {
                        grdRecordSearch = CreateGrid();
                    }
                    if (status != "")
                    {
                        _sendToBrokerBtn.Enabled = false;
                    }
                    else
                    {
                        _sendToBrokerBtn.Enabled = true;
                    }
                }
                _parentForm = this.ParentForm;
                RecordSearch();
             
            }

            catch (Exception ex)
            {
                ExceptionManager.PublishAndDisplayException(this, ex);
            }
        }//End OnFormTemplateLoaded\


        protected virtual void BindControls() 
        {
            try
            {
                if (_sendToBrokerBtn == null || _sendToBrokerBtn.IsDisposed)
                {
                    _sendToBrokerBtn = GetFormComponentByLayoutKey(this, "ACSCMEEventsSendToBroker Records To Send.Active Button.1") as AptifyActiveButton;
                }
                //if (_senderIdLinkBox == null || _senderIdLinkBox.IsDisposed)
                //{
                //    _senderIdLinkBox = GetFormComponentByLayoutKey(this, "ACS.ACSCMEEventsSendToBroker.Form.SenderId") as AptifyLinkBox;
                //}
                if (_eventStartDate == null || _eventStartDate.IsDisposed)
                {
                    _eventStartDate = GetFormComponentByLayoutKey(this, "ACSCMEEventsSendToBroker Records To Send.EventStartDate") as AptifyTextBox;
                }
                if (_eventEndDate == null || _eventEndDate.IsDisposed)
                {
                    _eventEndDate = GetFormComponentByLayoutKey(this, "ACSCMEEventsSendToBroker Records To Send.EventEndDate") as AptifyTextBox;
                }
                if (_status == null || _status.IsDisposed)
                {
                    _status = GetFormComponentByLayoutKey(this, "ACSCMEEventsSendToBroker Records To Send.Status") as AptifyTextBox;
                }
                status = _status.Value.ToString();


                if (_sendToBrokerBtn != null)
                {
                    _sendToBrokerBtn.Click += _sendToBrokerBtn_Click;
                }
                if(_eventStartDate != null)
                {
                    _eventStartDate.ValueChanged += _eventStartDate_ValueChanged;
                }
                if (_eventEndDate != null)
                {
                    _eventEndDate.ValueChanged += _eventEndDate_ValueChanged;
                }
               
                recordId = FormTemplateContext.GE.RecordID;

                if (Convert.ToString(_eventStartDate.Value) == "")
                { 
                    _eventStartDate.Value = CurrentDate;
                }
                if (Convert.ToString(_eventEndDate.Value) == "")
                {
                    _eventEndDate.Value = CurrentDate;
                }

            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }//End Bind Controls
        private void getSenderId()
        {
            try
            {
                if (userId != 11)
                {
                    senderIdSql = "select e.linkedpersonid from vwUserEntityRelations uer join vwemployees e on e.id = uer.EntityRecordID join vwusers u on u.id = uer.userid where u.id = " + userId;
                    senderId = Convert.ToInt32(m_oda.ExecuteScalar(senderIdSql));

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
                entityIdSql = "select ID from Entities where name like 'ACSCMEEventsSendToBroker'";
                entityId = Convert.ToInt32(m_oda.ExecuteScalar(entityIdSql));
                attachmentCatIdSql = "select ID from vwAttachmentCategories where name like 'MyCMEXML'";
                attachmentCatId = Convert.ToInt32(m_oda.ExecuteScalar(attachmentCatIdSql));

                fileName = Path.GetFileName(saveLocation);
                data = File.ReadAllBytes(saveLocation);

                AttachmentsGE = m_oApp.GetEntityObject("Attachments", -1);
                AttachmentsGE.SetValue("Name", fileName);
                AttachmentsGE.SetValue("Description", "XMLCEData");
                AttachmentsGE.SetValue("EntityID", entityId);
                AttachmentsGE.SetValue("RecordID", recordId);
                AttachmentsGE.SetValue("CategoryID", attachmentCatId);
                AttachmentsGE.SetValue("LocalFileName", saveLocation);
                AttachmentsGE.SetValue("BlobData", data);

                if (!AttachmentsGE.Save(false))
                {
                    result = "FAILED";
                    throw new Exception("Problem Saving attachments Record:" + AttachmentsGE.RecordID);

                }
                else
                {
                    AttachmentsGE.Save(true);
                    result = "SUCCESS";
                    attachId = AttachmentsGE.RecordID;
                }
                if (result == "SUCCESS")
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
                //CreateRecordSent();
                SaveForm();

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
                //url = "https://test.webservices.cebroker.com/";
                //service = "CEBrokerWebService.asmx/UploadXMLString";
                String xmlText = File.ReadAllText(saveLocation);
 
                InXML = Convert.ToString(xmlText);

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["InXML"] = InXML;
                  
                    var response = wb.UploadValues(url + service, "POST", data);
                    string responseInString = System.Text.Encoding.UTF8.GetString(response);

                    string responseInString1 = responseInString.Replace("&lt;","\n<" );
                    string responseInString2 = responseInString1.Replace("&gt;", ">");

                    XDocument xdoc = new XDocument();
                    xdoc = XDocument.Parse(responseInString2);

                    
                    string toFind1 = "ErrorCode=\"";
                    string toFind2 = "\" Message";
                    string toFind3 = "Message=\"";
                    string toFind4 = "\"";
                    string toFind5 = "provider_course_code=\"";
                    string toFind6 = "\" ";
                    string str; 
                    string[] strArr;
                    int i;

                    str = responseInString2; 
                    char[] splitchar = { '\n' };
                    strArr = str.Split(splitchar);
                    errorMessages = "<table><tr><td>CE Broker Event Submission Errors For Record: " + recordId + "</td></tr></table>";
                    for (i = 0; i <= strArr.Length - 1; i++)
                    {
                        if (strArr[i].Contains("ErrorCode=\""))
                        {
                            int start = strArr[i].IndexOf(toFind1) + toFind1.Length;
                            int end = strArr[i].IndexOf(toFind2, start);
                            ErrorCode = strArr[i].Substring(start, end - start);

                            if (ErrorCode != "")
                            {
                                int eventStart = strArr[i].IndexOf(toFind5) + toFind5.Length;
                                int eventEnd = strArr[i].IndexOf(toFind6, eventStart);
                                //eventId = Convert.ToInt32(strArr[i].Substring(eventStart, eventEnd - eventStart));
                                eId = strArr[i].Substring(eventStart, eventEnd - eventStart);

                                int startmes = strArr[i].IndexOf(toFind3) + toFind3.Length;
                                int endmes = strArr[i].IndexOf(toFind4, startmes);
                                ErrorMes = strArr[i].Substring(startmes, endmes - startmes);

                                UpdateErrorMessage();
                                errorMessages = errorMessages + "<table><tr><td>Event:  " + eventId + "</td><td> Error Code:  " + ErrorCode + "</td><td> Error Message:  " + ErrorMes + "</td></tr></table>";
                                hasErrors = "TRUE";
                            }
                           
                        }
                    }
                    
                    this.FormTemplateContext.GE.SetValue("XmlResponse", xdoc);

                }
                if (hasErrors == "TRUE")
                {
                    createMessageRun();
                    this.FormTemplateContext.GE.SetValue("Status", "HAS ERRORS");
                }
                else
                {
                    this.FormTemplateContext.GE.SetValue("Status", "SUBMITTED");
                }

                this.FormTemplateContext.GE.SetValue("XmlData", Convert.ToString(xmlText));
                               
                this.FormTemplateContext.GE.Save();
                RemoveLocalFile();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        private void CreateRecordSent()
        {

            try
            {
                sql = "select * from acscmecebrokerdata where ACSCMEEventId = " + eventId + " and resubmitevent = 1";
                dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                //need to get the file that gets created and read it back into

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        BrokerDataGE = m_oApp.GetEntityObject("ACSCMECEBrokerData", Convert.ToInt64(dt.Rows[0]["ID"].ToString()));
                        BrokerDataGE.SetValue("ResubmitEvent", 0);
                        BrokerDataGE.SetValue("ErrorCode", ErrorCode);
                        //BrokerDataGE.SetValue("ReturnErrorDesc",errorMes);

                    } 
                }
                else
                {
                    BrokerDataGE = m_oApp.GetEntityObject("ACSCMECEBrokerData", -1);
                    BrokerDataGE.SetValue("ACSCMEEventId", eventId);
                    BrokerDataGE.SetValue("ACSCMESendToBrokerId", recordId);
                    BrokerDataGE.SetValue("ErrorCode", ErrorCode);
                   //BrokerDataGE.SetValue("ReturnErrorDesc", errorMes);
                }


                if (!BrokerDataGE.Save(false))
                {
                    result = "FAILED";
                    throw new Exception("Problem Saving broker data Record:" + BrokerDataGE.RecordID);

                }
                else
                {
                    BrokerDataGE.Save(true);
                    result = "SUCCESS";

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
                sql = "select * from acscmecebrokerdata where ACSCMEEventId = " + eventId;
                dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                var errorIdSql = "select ID from acscmecebrokererrors where errorcode = " + ErrorCode;
                int errorId = Convert.ToInt32(m_oda.ExecuteScalar(errorIdSql));

                //need to get the file that gets created and read it back into

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        BrokerDataGE = m_oApp.GetEntityObject("ACSCMECEBrokerData", Convert.ToInt64(dt.Rows[0]["ID"].ToString()));
                        BrokerDataGE.SetValue("ACSCMESendToBrokerId", recordId);
                        BrokerDataGE.SetValue("ResubmitEvent", 0);
                        BrokerDataGE.SetValue("ErrorCodeId", errorId);
                        BrokerDataGE.SetValue("ReturnErrorDesc", ErrorMes);
                    }
                }
                else
                {
                    BrokerDataGE = m_oApp.GetEntityObject("ACSCMECEBrokerData",-1);
                    BrokerDataGE.SetValue("ACSCMESendToBrokerId", recordId);
                    BrokerDataGE.SetValue("ResubmitEvent", 0);
                    BrokerDataGE.SetValue("ACSCMEEventId", eventId);
                    BrokerDataGE.SetValue("ErrorCodeId", errorId);
                    BrokerDataGE.SetValue("ReturnErrorDesc", ErrorMes);

                }
                

                if (!BrokerDataGE.Save(false))
                {
                    result = "FAILED";
                    throw new Exception("Problem Saving broker data Record:" + BrokerDataGE.RecordID);

                }
                else
                {
                    BrokerDataGE.Save(true);
                    result = "SUCCESS";

                }

            }

            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
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
                    // .SetValue("Comments", PersonID)
                    messageRunGe.SetValue("RecipientCount", 0);
                    messageRunGe.SetValue("SourceType", "StaticSingle");
                    messageRunGe.SetValue("IDString", "3096875");
                    messageRunGe.SetValue("HTMLBody", errorMessages);
                    messageRunGe.SetValue("Subject", "CE Broker Event Submission Errors for Record #: " + recordId);
                }



                if (messageRunGe.IsDirty)
                {
                    if (!messageRunGe.Save(false))
                    {
                        result = "FAILED";
                        throw new Exception("Problem Saving Course Record:" + messageRunGe.RecordID);
                       
                    }
                    else
                    {
                        messageRunGe.Save(true);
                        result = "SUCCESS";
                        MRId = messageRunGe.RecordID;
                    }
                }
            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
            }
        }

        public DataGridView CreateGrid()
        {
            try
            {

                DataGridView grdReturn;
                // Dim gridtop = lCompanyLinkbox.Top + lCompanyLinkbox.Height + 10 
                grdReturn = new DataGridView();
                grdReturn.Name = "grdRecordSearch";
                grdReturn.Size = new System.Drawing.Size(900, 400);
                grdReturn.Location = new System.Drawing.Point(0, 100);
                Controls.Add(grdReturn);
                grdReturn.Visible = true;
                return grdReturn;

            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
                return null;
            }

        }
        private void RecordSearch()
        {
            try
            {
                searchRecordSql = "select ID, Name, CME_Start_Date, CME_End_Date,CME_Program, CME_Max_Credits FROM ACSCMEEvent WHERE convert(date, cme_start_date, 120) >= convert(date, '" + _eventStartDate.Value + "', 101) AND convert(date, cme_start_date,120) <= convert(date, '" + _eventEndDate.Value + "', 101) AND ID NOT In(SELECT ACSCMEEventId FROM ACSCMECEBrokerData WHERE ReSubmitEvent = 0 and ACSCMEEventId is not null) and CME_Max_Credits > 0";
                _recordSearchDT = m_oda.GetDataTable(searchRecordSql);
                if (_recordSearchDT.Rows.Count > 0)
                {
                    grdRecordSearch.DataSource = _recordSearchDT;

                    _boolAdded = grdRecordSearch.Columns[0].CellType.ToString() == "System.Windows.Forms.DataGridViewCheckBoxCell";
                     
                    if (_boolAdded == false)
                    {
                        //Add a CheckBox Column to the DataGridView at the first position.
                        DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                        checkBoxColumn.HeaderText = "";
                        checkBoxColumn.Width = 30;
                        checkBoxColumn.Name = "checkBoxColumn";
                       

                        grdRecordSearch.Columns.Insert(0, checkBoxColumn);

                        //Make our columns read only
                        for (var i = 1; i <= grdRecordSearch.ColumnCount - 1; i++)
                        {
                            grdRecordSearch.Columns[i].ReadOnly = true;
                        }
                        grdRecordSearch.Columns[1].Width = 45;
                        grdRecordSearch.Columns[2].Width = 300;
                        //Create cell for our checkbox
                        Rectangle headerCell = grdRecordSearch.GetCellDisplayRectangle(0, -1, true);

                        //Position our header
                        headerCell.Offset(50, 4);
                        
                        ////Place the Header CheckBox in the Location of the Header Cell.
                        headerCheckBox.Location = headerCell.Location;
                       
                        headerCheckBox.Size = new Size(18, 18);
                        headerCheckBox.Text = "";
                        //Assign Click event to the Header CheckBox.
                        headerCheckBox.Click += new EventHandler(HeaderCheckBox_Clicked);
                        //Add Checkbox to grid
                        grdRecordSearch.Controls.Add(headerCheckBox);
                       
                        _boolAdded = true;

                        
                    }
                    grdRecordSearch.AllowUserToAddRows = false;
                    grdRecordSearch.Refresh();

                }
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);

            }

        }

        private void _sendToBrokerBtn_Click(object sender, EventArgs e)
        {
            if (recordId > 0)
            {
                // CreateXml();
                var answer = MessageBox.Show("Are you sure you wish to process the selected records?", "Submit XML", MessageBoxButtons.YesNo);

                switch (answer)
                {
                    case DialogResult.Yes:
                        // SaveForm();
                        findSelectedRecords();


                        break;
                }
            }
            else
            {
                var answer = MessageBox.Show("You must save this record in order to proceed", "Save?", MessageBoxButtons.YesNo);
                //var answer = MessageBox.Show("Please save this record in order to proceed", "Submit XML");


                switch (answer)
                {
                    case DialogResult.Yes:
                        // SaveForm();
                        FormTemplateContext.GE.Save();
                        FormTemplateContext.GE.Display();

                       // _parentForm.Close();
                        break;
                }
                
            }
            
        }//End BtnClick

        private void findSelectedRecords()
        {
            try
            {
                sql = "SELECT ProviderId, UploadKey, Url, Service FROM vwACSCMEDataBrokerReporter WHERE Active = 1";
                dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                // Creates an instance of the XmlSerializer class;
                // specifies the type of object to serialize.
                url = Convert.ToString(dt.Rows[0]["Url"]);
                service = Convert.ToString(dt.Rows[0]["Service"]);
                XmlSerializer serializer = new XmlSerializer(typeof(Courses));
                TextWriter writer = new StreamWriter(saveLocation);
                //Create Courses XML
                Courses courses = new Courses();
                courses.id_parent_provider = Convert.ToInt32(dt.Rows[0]["ProviderId"]);
                courses.upload_key = Convert.ToString(dt.Rows[0]["UploadKey"]); //need to get the upload_key number from the CE Broker

                courses.course = new List<course>();

                //create datatable of IDs to Insert into the 
                eventId = 0;
                foreach (DataGridViewRow row in grdRecordSearch.Rows)
                {
                    bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                    if (isSelected)
                    {
                        eventId = Convert.ToInt32(row.Cells["ID"].Value.ToString());
                        CreateXml(eventId, saveLocation, courses, courses.id_parent_provider);
                        CreateRecordSent();
                    }
                }

                //Serializes the Courses, and closes the TextWriter.
                serializer.Serialize(writer, courses);
                writer.Close();
                CreateAttachment();
                //CreateRecordSent();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        private void CreateXml(int eventId, string filename, Courses courses,  int providerid) //Need EventId pulled in to get the event information for processing
        {
                
            try
            {
                course course = new course();
                course.course_board = new List<board>();
                string courseType = "";
                string deliveryMethod = "";
                DateTime endDate;
                EventGE = m_oApp.GetEntityObject("ACSCMEEvent", eventId);
                var subTypeId = Convert.ToInt32(EventGE.GetValue("CMETypeId"));
                var cmeMaxCredits = Convert.ToDecimal(EventGE.GetValue("cme_max_credits"));
                string enddate = Convert.ToString(EventGE.GetValue("cme_end_date"));
                int eventProgramId = Convert.ToInt32(EventGE.GetValue("ProgramID"));
                int eventTypeId = Convert.ToInt32(EventGE.GetValue("EventType"));

                if (eventProgramId > 0)
                {
                    sql = "SELECT * FROM vwACSCMEEventDeliveryType WHERE ACSCMEProgramId = " + eventProgramId;
                    dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                    if (dt.Rows.Count > 0)
                    {
                        for (int x = 0; x < dt.Rows.Count; x++)
                        {
                            courseType = Convert.ToString(dt.Rows[x]["cd_course_type"]);
                            deliveryMethod = Convert.ToString(dt.Rows[x]["cd_delivery_method"]);
                        }
                    }
                    else
                    {
                        sql = "SELECT * FROM vwACSCMEEventDeliveryType WHERE ACSEventTypeId = " + eventTypeId;
                        dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache); 
                        if (dt.Rows.Count > 0)
                        {
                            for (int x = 0; x < dt.Rows.Count; x++)
                            {
                                courseType = Convert.ToString(dt.Rows[x]["cd_course_type"]);
                                deliveryMethod = Convert.ToString(dt.Rows[x]["cd_delivery_method"]);
                            }
                        }
                        else
                        {
                            courseType = "ANYTIME"; 
                            deliveryMethod = "CBT";   
                        }
                    }
                }
                 
                else
                {
                    sql = "SELECT * FROM vwACSCMEEventDeliveryType WHERE ACSEventTypeId = " + eventTypeId;
                    dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                    if (dt.Rows.Count > 0)
                    {
                        for (int x = 0; x < dt.Rows.Count; x++)
                        {
                            courseType = Convert.ToString(dt.Rows[x]["cd_course_type"]);
                            deliveryMethod = Convert.ToString(dt.Rows[x]["cd_delivery_method"]);
                        }
                    }
                    else
                    {
                        courseType = "ANYTIME";
                        deliveryMethod = "CBT";
                    }
                }
                
                 
                    //if (eventTypeId == 1)
                    //{
                    //    courseType = "LIVE"; //EventType from ACSCMEEvent 1 = Live
                    //    deliveryMethod = "CLASS"; //Education needs to decide how to define this as wee need to start tracking this in ACSCMEEvent
                    //}
                    //else
                    //{
                    //    courseType = "ANYTIME"; //EventType from ACSCMEEvent != 1
                    //    deliveryMethod = "CBT"; //Education needs to decide how to define this as wee need to start tracking this in ACSCMEEvent
                    //}

                    if (enddate.Length == 0)
                {
                    endDate = Convert.ToDateTime(EventGE.GetValue("cme_start_date"));
                }
                else
                {
                    endDate = Convert.ToDateTime(EventGE.GetValue("cme_end_date")); //If cme_end_date is null use cme_start_date
                }

                //Create new element course
                courses.course.Add(new course
                {
                    id_provider = providerid,
                    provider_course_code = eventId, //EventId from the ACSCMEEvent 
                    nm_course = Convert.ToString(EventGE.GetValue("Name")), //Name from the ACSCMEEvent
                    ds_course = Convert.ToString(EventGE.GetValue("Cme_Program")), //Cme_Program from the ACSCMEEvent
                    cd_course_type = courseType,
                    series = null, //used only for advertising with CE Broker
                    modular = null, //used only for advertising with CE Broker
                    concurrent = null, //used only for advertising with CE Broker
                    cd_delivery_method = deliveryMethod,
                    course_process = "CourseXML", //If this is not a new course being submitted to CE Broker RESUBMITHRSXML

                    dt_start = Convert.ToDateTime(EventGE.GetValue("cme_start_date")).ToString("MM/dd/yyyy"), //cme_start_date goes here
                    //dt_end = Convert.ToDateTime(endDate).ToString("MM/dd/yyyy"), //changed this to convert
                    course_board = course.course_board
                }); 

                CreateBoard(course, subTypeId, cmeMaxCredits);
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        private void CreateBoard(course course, int subTypeId, decimal cmeMaxCredits)
        {
            var sql = "SELECT BoardId, Name FROM vwACSCMEDataBrokerBoard WHERE ACSCMEDataBrokerReporter_Name = 'CE Broker' AND Active = 1";
            var dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);

            //start course_board for loop
            for (int x = 0; x < dt.Rows.Count; x++)
            {
                board board = new board();
                board.board_component = new List<component>();
                var exists = "n";
                int boardId = Convert.ToInt32(dt.Rows[x]["BoardId"]);
                var sqlSubjects = "SELECT * FROM vwACSCMEDataBrokerBoardSubject WHERE Active = 1 AND ACSCMEDataBrokerBoard_BoardId = " + boardId;
                var dtComponents = DataAction.GetDataTable(sqlSubjects, IAptifyDataAction.DSLCacheSetting.BypassCache);
                int boardSubTypeId = 0;

                if (boardId > 0)
                {
                    course.course_board.Add(new board
                    {
                        //int boardId = Convert.ToInt32(dt.Rows[x]["BoardId"]);
                        id_board = boardId, //this needs to be the BoardId from ACSSendCmeDataBrokerBoard
                        board_component = board.board_component
                    });

                    //start board_component for loop
                    for (int intx = 0; intx < dtComponents.Rows.Count; intx++)
                    {
                        component component = new component();
                        boardSubTypeId = Convert.ToInt32(dtComponents.Rows[intx]["ACSCMESubType_ID"]);
                        if (subTypeId == boardSubTypeId)
                        {
                            board.board_component.Add(new component
                            {
                                cd_subject_area = Convert.ToString(dtComponents.Rows[intx]["SubjectAreaCode"]), //this needs to be the SubjectAreaCode from ACSSendCmeDataBrokerBoardSubject
                                am_app_hours = Convert.ToDecimal(cmeMaxCredits), //cme_max_credits from ACSCMEEvent
                                cd_profession = Convert.ToString(dtComponents.Rows[intx]["ProfessionCode"]) //this needs to include both MD and DO professions from ACSSendCmeDataBrokerBoardSubject
                            });
                            exists = "y";
                        }
                    }
                    //end board_component loop

                    if (exists == "n")
                    {
                        component component = new component();
                        //start board_component for loop
                        for (int intx = 0; intx < dtComponents.Rows.Count; intx++)
                        {
                            boardSubTypeId = Convert.ToInt32(dtComponents.Rows[intx]["ACSCMESubType_ID"]);
                            if (boardSubTypeId == 32)
                            {
                                board.board_component.Add(new component
                                {
                                    cd_subject_area = Convert.ToString(dtComponents.Rows[intx]["SubjectAreaCode"]), //this needs to be the SubjectAreaCode from ACSSendCmeDataBrokerBoardSubject
                                    am_app_hours = Convert.ToDecimal(cmeMaxCredits), //cme_max_credits from ACSCMEEvent
                                    cd_profession = Convert.ToString(dtComponents.Rows[intx]["ProfessionCode"]) //this needs to include both MD and DO professions from ACSSendCmeDataBrokerBoardSubject
                                });
                            }
                        }
                    }
                }

            }
            //end Course_board loop

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
                   // MessageBox.Show("File Deleted");
                }
                MessageBox.Show("Process Completed."); 
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        private void HeaderCheckBox_Clicked(object sender, EventArgs e)
        { 
            
            //Necessary to end the edit mode of the Cell.
            grdRecordSearch.EndEdit();
            for (int i = 0; i < grdRecordSearch.RowCount; i++)
            {
                grdRecordSearch.Rows[i].Cells[0].Value = headerCheckBox.Checked;
            }

            //  findSelectedRecords();

        }

        private void _eventStartDate_ValueChanged(object sender, object OldValue, object NewValue)
        {
            RecordSearch();
        }
        private void _eventEndDate_ValueChanged(object sender, object OldValue, object NewValue)
        {
            RecordSearch();
        }
     
    }//End Class

}//End Namespace