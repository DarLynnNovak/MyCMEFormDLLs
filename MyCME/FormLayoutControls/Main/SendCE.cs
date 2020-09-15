using Aptify.Framework.Application;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using Aptify.Framework.WindowsControls;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.WindowsControls;
using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;
using System.Web;
using System.Data;
using System.Drawing;
 

namespace ACSMyCMEFormDLLs.FormLayoutControls.Main
{ 
   public class SendCE : FormTemplateLayout
    {

        private DataAction m_oda = new DataAction();
        private AptifyProperties m_oProps = new AptifyProperties();
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyGenericEntityBase AttachmentsGE;
        private AptifyLinkBox _senderIdLinkBox;
        private AptifyActiveButton _sendToBrokerBtn;
        private AptifyTextBox _eventStartDate;
        private AptifyTextBox _eventEndDate;
        private DataGridView grdRecordSearch;
        private DataTable _recordSearchDT;
        private bool _boolAdded;
        private long lGridID = -1;
        XDocument xDoc = new XDocument();
        //string saveLocation = "C:\\Code CSharp\\ACSMyCMEFormDLLs\\MyCME\\FormLayoutControls\\XML\\XMLCEData.xml";
        string saveLocation = "XMLCEData.xml";
        string attachmentCatIdSql;
        int attachmentCatId;
        string entityIdSql;
        int entityId;
        string senderIdSql;
        string searchRecordSql;
        int senderId;
        long attachId;
        long userId;
        long recordId;
        string filename = null;
        string result = "Failed";
        byte[] data;

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
                    getSenderId();
                    //if (grdRecordSearch is null)
                    //{
                    //    grdRecordSearch = CreateGrid();
                    //}


                }

                _sendToBrokerBtn.Click += _sendToBrokerBtn_Click;
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


                _sendToBrokerBtn.Click += _sendToBrokerBtn_Click;
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
        void _sendToBrokerBtn_Click(object sender, System.EventArgs e)
        {
            CreateXml();
            MessageBox.Show("Hello Julie ");
        }//End BtnClick

        private void CreateXml()
        {
            try
            {
                xDoc = new XDocument(
                 new XDeclaration("1.0", "utf-8", "yes"),
                 new XElement("Courses",
                     new XAttribute("id_parent_provider", "2"),
                     new XAttribute("upload_key", "1234"),
                     new XElement("Course")
                     )
                 );
                MessageBox.Show(Convert.ToString(xDoc));
                xDoc.Save(saveLocation);

                CreateAttachment();
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

                filename = Path.GetFileName(saveLocation);
                data = File.ReadAllBytes(saveLocation);

                AttachmentsGE = m_oApp.GetEntityObject("Attachments", -1);
                AttachmentsGE.SetValue("Name", filename);
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

        private void SaveForm()
        {
            try
            {
                this.FormTemplateContext.GE.SetValue("XmlData", Convert.ToString(xDoc));
                this.FormTemplateContext.GE.Save();
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
        //public DataGridView CreateGrid()
        //{
        //    try
        //    {

        //        DataGridView grdReturn;
        //        // Dim gridtop = lCompanyLinkbox.Top + lCompanyLinkbox.Height + 10 
        //        grdReturn = new DataGridView();
        //        grdReturn.Name = "grdRecordSearch";
        //        grdReturn.Size = new System.Drawing.Size(500, 150);
        //        grdReturn.Location = new System.Drawing.Point(50, 55);
        //        Controls.Add(grdReturn);
        //        grdReturn.Visible = true;
        //        return grdReturn;

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        return null;
        //    }

        //}
        //private void RecordSearch()
        //{
        //    try
        //    {
        //        searchRecordSql = "ID, Name, CME_Start_Date, CME_End_Date,CME_Program, CME_Max_Credits FROM ACSCMEEvent WHERE convert(date, cme_start_date, 120) >= convert(date, " + _eventStartDate.Value + ", 101) AND convert(date, cme_start_date,120) <= convert(date, " + _eventEndDate.Value + ", 101) AND ID NOT In(SELECT ACSCMEEventId FROM ACSCMECEBrokerData WHERE ReSubmitEvent = 0)";
        //        _recordSearchDT = m_oda.GetDataTable(searchRecordSql);
        //        if (_recordSearchDT.Rows.Count > 0)
        //        {
        //            grdRecordSearch.DataSource = _recordSearchDT;

        //            _boolAdded = grdRecordSearch.Columns[0].CellType.ToString() == "System.Windows.Forms.DataGridViewCheckBoxCell";

        //            if (_boolAdded == false)
        //            {
        //                var addColumn = new DataGridViewCheckBoxColumn
        //                {
        //                    HeaderText = "",
        //                    Name = "grdChecked",
        //                    Width = 21
        //                };
        //                grdRecordSearch.Columns.Insert(0, addColumn);

        //                for (var i = 1; i <= grdRecordSearch.ColumnCount - 1; i++)
        //                {
        //                    grdRecordSearch.Columns[i].ReadOnly = true;
        //                }

        //                _boolAdded = true;
        //            }
        //            grdRecordSearch.AllowUserToAddRows = false;
        //            grdRecordSearch.Refresh();

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);

        //    }
        //}
    }//End Class

}//End Namespace