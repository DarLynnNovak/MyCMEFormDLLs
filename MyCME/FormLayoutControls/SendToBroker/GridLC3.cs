using Aptify.Framework.Application;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using Aptify.Framework.WindowsControls;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
using Aptify.Framework.AttributeManagement;
using System.Diagnostics.PerformanceData;
using System.Collections.Generic;
using System.Windows.Forms.Integration;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

namespace ACSMyCMEFormDLLs.FormLayoutControls.SendToBroker
{
    [XmlRoot("Courses")]
    public class Courses
    {
        [XmlElement]
        public int id_parent_provider;
        [XmlElement]
        public int upload_key;      
        public List<ChildCourse> ChildCourses { get; set; }

    }

    public class ChildCourse : Courses
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
        public DateTime dt_start; //cme_start_date goes here
        public DateTime dt_end; //cme_end_date goes here unless null, then cme_start_date goes here
        public string enddate;
        public List<Board> Boards { get; set; }
    }

    public class Board
    {
        public int id_board; //locate BoardId from ACSCMEDataBrokerBoard 
        public List<Component> Components { get; set; }
    }

    public class Component
    {
        public string cd_subject_area; //locate SubjectAreaCode from ACSCMEDataBrokerBoardSubject
        public decimal am_app_hours; // CME_Max_Credits from ACSCMEEvent 
        public string cd_profession; //MD or DO depending on what doctor is
    }

    class GridLC3 : FormTemplateLayout
    {

        public CheckBox HeaderCheckBox { get; private set; }
        public int providerId = 17271;
        private AptifyGenericEntityBase EventGE;
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyLinkBox _senderIdLinkBox;
        private AptifyActiveButton _sendToBrokerBtn;
        private AptifyTextBox _eventStartDate;
        private AptifyTextBox _eventEndDate;
        private DataAction m_oda = new DataAction();
        private DataGridView grdRecordSearch;
        private DataTable _recordSearchDT;
        private bool _boolAdded;

        byte[] data;
        CheckBox headerCheckBox = new CheckBox();
        DataGridViewCheckBoxCell recordCheckBox = new DataGridViewCheckBoxCell();
        XDocument xDoc = new XDocument();
        static string saveLocalPrefix = "C:\\Users\\Public\\Documents\\";
        static string fileName = "XmlEventCourses" + DateTime.Now.ToString("yyyyMMdd_hhmm") + ".xml";
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
        long subTypeId; 
        decimal cmeMaxCredits;
        XmlSerializer serializer;
        TextWriter writer;
        string cdcourse_type;
        string cddelivery_method;
        string enddate;
        int eventTypeId;
        DateTime dtend;
        int idboard;
        //course cs = new course();
        public void Config()
        {
            try
            {
                m_oApp = ApplicationObject;
                this.m_oda = new Aptify.Framework.DataServices.DataAction(this.m_oApp.UserCredentials);
                userId = m_oda.UserCredentials.AptifyUserID;
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
                if (_eventStartDate != null)
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
        private void _sendToBrokerBtn_Click(object sender, EventArgs e)
        {
            // CreateXml();
            var answer = MessageBox.Show("Are you sure you wish to process records the selected records?", "Submit XML", MessageBoxButtons.YesNo);

            switch (answer)
            {
                case DialogResult.Yes:

                    findSelectedRecords();

                    //xDoc.Save(saveLocation);

                    //CreateAttachment();
                    break;
            }
        }//End BtnClick
        private void _eventStartDate_ValueChanged(object sender, object OldValue, object NewValue)
        {
            RecordSearch();
        }
        private void _eventEndDate_ValueChanged(object sender, object OldValue, object NewValue)
        {
            RecordSearch();
        }
        public void RecordSearch()
        {
            try
            {
                searchRecordSql = "select ID, Name, CME_Start_Date, CME_End_Date,CME_Program, CME_Max_Credits FROM ACSCMEEvent WHERE convert(date, cme_start_date, 120) >= convert(date, '" + _eventStartDate.Value + "', 101) AND convert(date, cme_start_date,120) <= convert(date, '" + _eventEndDate.Value + "', 101) AND ID NOT In(SELECT ACSCMEEventId FROM ACSCMECEBrokerData WHERE ReSubmitEvent = 0)";
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

        public void findSelectedRecords()
        {
            try
            { 
                serializer = new XmlSerializer(typeof(Courses));
                writer = new StreamWriter(saveLocation);
                
                Courses courses = new Courses();
                Board board = new Board();
                courses.id_parent_provider = providerId;
                courses.upload_key = 12345; //need to get the upload_key number from the CE Broker
                eventId = 0;
                courses.ChildCourses = new List<ChildCourse>();
                ChildCourse child = new ChildCourse();
                child.Boards = new List<Board>();
                Component component = new Component();
                board.Components = new List<Component>();

                foreach (DataGridViewRow row in grdRecordSearch.Rows)
                {
                    bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                    if (isSelected)
                    {
                        eventId = Convert.ToInt32(row.Cells["ID"].Value.ToString());
                        EventGE = m_oApp.GetEntityObject("ACSCMEEvent", eventId);
                        // createChildElement(); 
                        subTypeId = Convert.ToInt32(EventGE.GetValue("CMETypeId"));
                        cmeMaxCredits = Convert.ToDecimal(EventGE.GetValue("cme_max_credits"));
                        enddate = Convert.ToString(EventGE.GetValue("cme_end_date"));
                        //Create new element course 
                        eventTypeId = Convert.ToInt32(EventGE.GetValue("EventType"));
                        if (eventTypeId == 1)
                        {
                            cdcourse_type = "LIVE"; //EventType from ACSCMEEvent 1 = Live
                            cddelivery_method = "CLASS"; //Education needs to decide how to define this as wee need to start tracking this in ACSCMEEvent
                        }
                        else
                        {
                            cdcourse_type = "ANYTIME";//EventType from ACSCMEEvent != 1
                            cddelivery_method = "HOMESTUDY"; //Education needs to decide how to define this as wee need to start tracking this in ACSCMEEvent
                        }

                        if (enddate.Length == 0)
                        {
                            dtend = Convert.ToDateTime(EventGE.GetValue("cme_start_date"));
                        }
                        else
                        {
                            dtend = Convert.ToDateTime(EventGE.GetValue("cme_end_date")); //If cme_end_date is null use cme_start_date
                        }
                       
                        courses.ChildCourses.Add(new ChildCourse
                        {
                            id_provider = providerId,
                            provider_course_code = eventId, //EventId from the ACSCMEEvent 
                            nm_course = Convert.ToString(EventGE.GetValue("Name")), //Name from the ACSCMEEvent
                            ds_course = Convert.ToString(EventGE.GetValue("Cme_Program")), //Cme_Program from the ACSCMEEvent
                            cd_course_type = cdcourse_type,
                            series = null,
                            modular = null,
                            concurrent = null,
                            cd_delivery_method = cddelivery_method,
                            course_process = "CourseXML", //If this is not a new course being submitted to CE Broker RESUBMITHRSXML
                            dt_start = Convert.ToDateTime(EventGE.GetValue("cme_start_date")), //cme_start_date goes here
                            enddate = Convert.ToString(EventGE.GetValue("cme_end_date")),
                            dt_end = dtend,
                            Boards = child.Boards

                        }); //end coursechild add

                        var sql = "SELECT BoardId, Name FROM vwACSCMEDataBrokerBoard WHERE ACSCMEDataBrokerReporter_Name = 'CE Broker' AND Active = 1";
                        var dt = DataAction.GetDataTable(sql, IAptifyDataAction.DSLCacheSetting.BypassCache);
                        for (int x = 0; x < dt.Rows.Count; x++)
                        {
                            idboard = Convert.ToInt32(dt.Rows[x]["BoardId"]); //this needs to be the BoardId from ACSSendCmeDataBrokerBoard
                            var sqlSubjects = "SELECT * FROM vwACSCMEDataBrokerBoardSubject WHERE Active = 1 AND ACSCMEDataBrokerBoard_BoardId = " + idboard;
                            var dtComponents = DataAction.GetDataTable(sqlSubjects, IAptifyDataAction.DSLCacheSetting.BypassCache);

                            child.Boards.Add(new Board
                            {
                                id_board = idboard,
                                Components = board.Components

                            }); //end Board add

                            for (int intx = 0; intx < dtComponents.Rows.Count; intx++)
                            {
                                int boardSubTypeId = Convert.ToInt32(dtComponents.Rows[intx]["ACSCMESubType_ID"]);
                                if (subTypeId == boardSubTypeId)
                                {
                                    
                                    board.Components.Add(new Component
                                        { 
                                        cd_subject_area = Convert.ToString(dtComponents.Rows[intx]["SubjectAreaCode"]), //this needs to be the SubjectAreaCode from ACSSendCmeDataBrokerBoardSubject
                                        am_app_hours = Convert.ToDecimal(cmeMaxCredits), //cme_max_credits from ACSCMEEvent
                                        cd_profession = Convert.ToString(dtComponents.Rows[intx]["ProfessionCode"]), //this needs to include both MD and DO professions from ACSSendCmeDataBrokerBoardSubject
                                        });
                                }
                            }
                        }

                    } //end if selected

                }
               
                serializer.Serialize(writer, courses);  
                writer.Close();
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

    }

}
