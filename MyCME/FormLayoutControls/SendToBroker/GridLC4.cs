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

namespace ACSMyCMEFormDLLs.FormLayoutControls.SendToBroker
{
    public enum PersonGender
    {
        Male,
        Female
    }

    public class Person
    {
        public Person() // Default constructor must be available
        {
        }

        public Person(string name, DateTime dob, PersonGender gender)
        {
            _name = name;
            _dateOfBirth = dob;
            _gender = gender;
        }
        [XmlElement("Name")]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [XmlElement("DateOfBirth")]
        public DateTime DateOfBirth
        {
            get { return _dateOfBirth; }
            set { _dateOfBirth = value; }
        }

        [XmlElement("Gender")]
        public PersonGender Gender
        {
            get { return _gender; }
            set { _gender = value; }
        }

        private string _name;

        private DateTime _dateOfBirth;

        private PersonGender _gender;

    }

    //public class board
    //{
    //    public int id_board; //locate BoardId from ACSCMEDataBrokerBoard
    //    public component[] board_component;
    //}

    //public class component
    //{
    //    public string cd_subject_area; //locate SubjectAreaCode from ACSCMEDataBrokerBoardSubject
    //    public decimal am_app_hours; // CME_Max_Credits from ACSCMEEvent 
    //    public string cd_profession; //MD or DO depending on what doctor is
    //}

    class GridLC4 : FormTemplateLayout
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

                Person joe = new Person("Joe", new DateTime(1970, 5, 12), PersonGender.Male);
                Person mary = new Person("Mary", new DateTime(1972, 3, 6), PersonGender.Female);

                writer = new StreamWriter(saveLocation);
                XmlSerializer serializer = new XmlSerializer(typeof(Person));

                using (Stream output = Console.OpenStandardOutput())
                {
                    serializer.Serialize(output, joe);
                    serializer.Serialize(output, mary);
                    MessageBox.Show("Selected Values" + output);
                    serializer.Serialize(writer, output);
                    writer.Close();
                }

               
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
