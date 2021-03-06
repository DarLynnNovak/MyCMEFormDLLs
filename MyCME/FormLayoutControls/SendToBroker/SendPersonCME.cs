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

namespace ACSMyCMEFormDLLs.FormLayoutControls.SendToBroker
{


    [Serializable]
    [XmlRoot("rosters")]
    public class Rosters
    {
        [XmlAttribute] public int id_parent_provider;
        [XmlAttribute] public string upload_key;
        [XmlElement]
        public List<attendees> attendees { get; set; }
    }

    [Serializable]
    public class attendees
    {
        public string license_professional;
        public string license; 
        public string cebroker_state; //event name
        public string first_name; //event cme_program
        public string last_name; //if eventType != Live then Anytime
        public string date_completed;
        public List<partial_credit> partial { get; set; }
    }

    [Serializable]
    public class partial_credit
    {
        public string cd_profession; //locate BoardId from ACSCMEDataBrokerBoard
        public string cd_subject_area;
        public decimal partial_credit_hours;
    }

  

    public class SendPersonCME : FormTemplateLayout
    {
        private AptifyProperties m_oProps = new AptifyProperties();
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyGenericEntityBase AttachmentsGE;
        private AptifyGenericEntityBase BrokerDataGE;
        private AptifyLinkBox _senderIdLinkBox;
        private AptifyActiveButton _sendToBrokerBtn;
        private AptifyTextBox _eventStartDate;
        private AptifyTextBox _eventEndDate;
        private DataAction m_oda = new DataAction();
        private DataGridView grdRecordSearch;
        private DataTable _recordSearchDT;
        private bool _boolAdded;
        private static readonly HttpClient client = new HttpClient();
        public string InXML = "";
        byte[] data;
        CheckBox headerCheckBox = new CheckBox();
        DataGridViewCheckBoxCell recordCheckBox = new DataGridViewCheckBoxCell();
        XDocument xDoc = new XDocument();
        static string saveLocalPrefix = "C:\\Users\\Public\\Documents\\";
        static string fileName = "XmlPersonCME" + DateTime.Now.ToString("yyyyMMdd_hhmm") + ".xml";
        //static string fileName = "XmlEventCourses20201105_1156.xml"; //changed for testing change back to abve
        string attachmentCatIdSql;
        string entityIdSql;
        int eventId;
        string result = "Failed";
        string saveLocation = saveLocalPrefix + fileName;
        string searchRecordSql;
        string senderIdSql;

        int attachmentCatId;
        int entityId;
        int num;
        int senderId;
        long attachId;
        long recordId;
        long userId;

        public CheckBox HeaderCheckBox { get; private set; }
        private AptifyGenericEntityBase ACSPersonCMEGE;
        private AptifyGenericEntityBase EventGE;
        private AptifyGenericEntityBase BoardGE;
        private AptifyGenericEntityBase ComponentGE;
        private string url = "";
        private string service = "";
        public void Config()
        {
            try
            {
                m_oApp = ApplicationObject;
                this.m_oda = new Aptify.Framework.DataServices.DataAction(this.m_oApp.UserCredentials);
                userId = m_oda.UserCredentials.AptifyUserID;

                //If m_oda.UserCredentials.Server.ToLower = "aptify" Then
                if (m_oda.UserCredentials.Server.ToLower() == "aptify")
                {

                }
                if (m_oda.UserCredentials.Server.ToLower() == "stagingaptify61")
                {

                }
                if (m_oda.UserCredentials.Server.ToLower() == "testaptify610")
                {
                    url = "https://test.webservices.cebroker.com/";
                    service = "CEBrokerWebService.asmx/UploadXMLString";
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
                }
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
                    _sendToBrokerBtn = GetFormComponentByLayoutKey(this, "ACS.ACSCMESendToBroker.Form.Active Button.1") as AptifyActiveButton;
                }
                if (_senderIdLinkBox == null || _senderIdLinkBox.IsDisposed)
                {
                    _senderIdLinkBox = GetFormComponentByLayoutKey(this, "ACS.ACSCMESendToBroker.Form.SenderId") as AptifyLinkBox;
                }
                if (_eventStartDate == null || _eventStartDate.IsDisposed)
                {
                    _eventStartDate = GetFormComponentByLayoutKey(this, "ACSCMESendToBroker.EventStartDate") as AptifyTextBox;
                }
                if (_eventEndDate == null || _eventEndDate.IsDisposed)
                {
                    _eventEndDate = GetFormComponentByLayoutKey(this, "ACSCMESendToBroker.EventEndDate") as AptifyTextBox;
                }

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
                AttachmentsGE.SetValue("RecordID", recordId);
                AttachmentsGE.SetValue("CategoryID", attachmentCatId);
                AttachmentsGE.SetValue("LocalFileName", saveLocation);
                AttachmentsGE.SetValue("BlobData", data);

                if (!AttachmentsGE.Save(false))
                {
                    result = "Error";
                    throw new Exception("Problem Saving attachments Record:" + AttachmentsGE.RecordID);

                }
                else
                {
                    AttachmentsGE.Save(true);
                    result = "Success";
                    attachId = AttachmentsGE.RecordID;
                }
                if (result == "Success")
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
                String xmlText = File.ReadAllText(saveLocation);
 
                InXML = Convert.ToString(xmlText);

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["InXML"] = InXML;

                    //wb.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    
                    var response = wb.UploadValues(url + service, "POST", data);
                    string responseInString = System.Text.Encoding.UTF8.GetString(response);

                    string responseInString1 = responseInString.Replace("&lt;","\n<" );
                    string responseInString2 = responseInString1.Replace("&gt;", ">");

                    XDocument xdoc = new XDocument();
                    xdoc = XDocument.Parse(responseInString2);

                    
                    string toFind1 = "ErrorCode=\"";
                    string toFind2 = "\" Message";
                    
                    string str; 
                    string[] strArr;
                    int i;

                    str = responseInString2;
                    char[] splitchar = { '\n' };
                    strArr = str.Split(splitchar);
                    for (i = 0; i <= strArr.Length - 1; i++)
                    {
                        if (strArr[i].Contains("ErrorCode=\""))
                        {
                            int start = strArr[i].IndexOf(toFind1) + toFind1.Length;
                            int end = strArr[i].IndexOf(toFind2, start); //Start after the index of 'my' since 'is' appears twice
                            string ErrorCode = strArr[i].Substring(start, end - start);

                            if (ErrorCode != "")
                            {
                                //MessageBox.Show(ErrorCode);
                            }
                        }
                    }
                    this.FormTemplateContext.GE.SetValue("XmlResponse", xdoc);

                }

                this.FormTemplateContext.GE.SetValue("XmlData", Convert.ToString(xmlText));              
                this.FormTemplateContext.GE.Save();
                // RemoveLocalFile();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
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
                grdReturn.Size = new System.Drawing.Size(800, 400);
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
                searchRecordSql = "select ID, PersonID, CMEDateGranted FROM ACSPersonCME WHERE convert(date, CMEDateGranted, 120) >= convert(date, '" + _eventStartDate.Value + "', 101) AND convert(date, CMEDateGranted, 120) <= convert(date, '" + _eventEndDate.Value + "', 101) and PersonID = " + userId;
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
            // CreateXml();
            var answer = MessageBox.Show("Are you sure you wish to process records the selected records?", "Submit XML", MessageBoxButtons.YesNo);

            switch (answer)
            {
                case DialogResult.Yes:
                   // SaveForm();
                    findSelectedRecords();


                    break;
            }
        }//End BtnClick

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

                rosters.attendees = new List<attendees>();

                //create datatable of IDs to Insert into the 
                eventId = 0;
                foreach (DataGridViewRow row in grdRecordSearch.Rows)
                {
                    bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                    if (isSelected)
                    {
                        //eventId += Convert.ToInt32(Environment.NewLine);
                        //eventId += Convert.ToInt32(row.Cells["ID"].Value.ToString());
                        eventId = Convert.ToInt32(row.Cells["ACSCMEEventID"].Value.ToString());
                        CreateXml(eventId, saveLocation, rosters, rosters.id_parent_provider);
                    }
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

        private void CreateXml(int eventId, string filename, Rosters rosters,  int providerid) //Need EventId pulled in to get the event information for processing
        {
                
            try
            {

                Rosters roster = new Rosters();
                roster.attendees = new List<attendees>();
                
                
                ACSPersonCMEGE = m_oApp.GetEntityObject("ACSPersonCME", userId);
                eventId = Convert.ToInt32(ACSPersonCMEGE.GetValue("ACSCMEEventId"));
                EventGE = m_oApp.GetEntityObject("ACSCMEEvent", eventId);
                var CMETypeOne = Convert.ToInt32(ACSPersonCMEGE.GetValue("CMEType1"));
                var cmeMaxCredits = Convert.ToDecimal(EventGE.GetValue("cme_max_credits"));
                //string enddate = Convert.ToString(ACSPersonCMEGE.GetValue("cme_end_date"));
                //int eventTypeId = Convert.ToInt32(ACSPersonCMEGE.GetValue("EventType"));
                
                //Create new element course
                rosters.attendees.Add(new attendees
                {
                    license_professional = "",
                    license = "",
                    cebroker_state = "",
                    first_name = "",
                    last_name = "",
                    date_completed = Convert.ToDateTime(ACSPersonCMEGE.GetValue("cmedategranted")).ToString("MM/dd/yyyy"),
                    partial = new List<partial_credit> ()
                });

              //  CreatePartialCredit(attendees, cmeMaxCredits);
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        }

        //private void CreatePartialCredit(attendees attendees, decimal cmeMaxCredits)
        //{
        //    var sql = "SELECT BoardId, Name FROM vwACSCMEDataBrokerBoard WHERE ACSCMEDataBrokerReporter_Name = 'CE Broker' AND Active = 1";
        //    var dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);

        //    //start course_board for loop
        //    for (int x = 0; x < dt.Rows.Count; x++)
        //    {
        //        partial partial_Credit = new partial();
               

        //    }
        //}

        private void CreateRecordSent()
        {

            try
            {
                var sql = "select * from acscmecebrokerdata where ACSCMEEventId = " + eventId + " and resubmitevent = 1";
                var dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                //need to get the file that gets created and read it back into

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        BrokerDataGE = m_oApp.GetEntityObject("ACSCMECEBrokerData", Convert.ToInt64(dt.Rows[0]["ID"].ToString()));
                        BrokerDataGE.SetValue("ResubmitEvent", 0);

                    }
                }
                else
                {
                    BrokerDataGE = m_oApp.GetEntityObject("ACSCMECEBrokerData", -1);
                    BrokerDataGE.SetValue("ACSCMEEventId", eventId);
                    BrokerDataGE.SetValue("ACSCMESendToBrokerId", recordId);
                }

                
                if (!BrokerDataGE.Save(false))
                {
                    result = "Error";
                    throw new Exception("Problem Saving broker data Record:" + BrokerDataGE.RecordID);

                }
                else
                {
                    BrokerDataGE.Save(true);
                    result = "Success";

                }

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
                   // MessageBox.Show("File Deleted");
                }
            }
            catch (Exception ex)
            {
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