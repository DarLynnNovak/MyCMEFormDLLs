using Aptify.Framework.Application;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using Aptify.Framework.WindowsControls;
using System;


namespace ACSMyCMEFormDLLs.FormLayoutControls.Main
{
    public class MainFormLC : FormTemplateLayout
    {

        private DataAction m_oda = new DataAction();
        private AptifyProperties m_oProps = new AptifyProperties();
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyLinkBox _senderIdLinkBox;
        private FormTemplateTab _tabs;
        private FormTemplateTab _tabs2;


        long userId;
        long recordId;
        int senderId;
        string senderIdSql;
        

        public void Config()
        {
            try
            {
                m_oApp = ApplicationObject;
                m_oda = new Aptify.Framework.DataServices.DataAction(this.m_oApp.UserCredentials);
                userId = m_oda.UserCredentials.AptifyUserID;
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
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
                }
            }

            catch (Exception ex)
            { 
                ExceptionManager.Publish(ex);
            }
        }//End OnFormTemplateLoaded\


        protected virtual void BindControls()
        {
            try
            {

                if (_tabs == null || _tabs.IsDisposed)
                 {
                    _tabs = GetFormComponentByLayoutKey(this, "ACS.ACSCMEEventsSendToBroker.Tabs") as FormTemplateTab;
                }
                if (_tabs2 == null || _tabs2.IsDisposed)
                {
                    _tabs2 = GetFormComponentByLayoutKey(this, "ACS.ACSCMEEventsSendToBroker.Tabs2") as FormTemplateTab;
                }
                if (_senderIdLinkBox == null || _senderIdLinkBox.IsDisposed)
                {
                    _senderIdLinkBox = GetFormComponent(this, "ACS.ACSCMEEventsSendToBroker.SenderId") as AptifyLinkBox;
                }

                //if (_tabs != null)
                //{
                //    _tabs.Load += _tabs_Load;
                //}

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
                    if(_tabs != null)
                    {
                        _tabs.Visible = false;
                    }
                    if (_tabs2 != null)
                    { 
                        _tabs2.Visible = true;
                    } 
                    if (Convert.ToInt32(_senderIdLinkBox.Value) <= 0)
                    {
                        //_senderIdLinkBox.Value = senderId;
                        FormTemplateContext.GE.SetValue("SenderId", senderId);
                    }
                    

                }
                else
                {
                    //  _senderIdLinkBox.Value = 03096875;
                    if (Convert.ToInt32(_senderIdLinkBox.Value) <= 0)
                    {

                        FormTemplateContext.GE.SetValue("SenderId", 03096875);
                    }
                    if (_tabs != null)
                    {
                        _tabs.Visible = true;
                    }
                    if (_tabs2 != null)
                    {
                        _tabs2.Visible = false;
                    }
                }
                //this.FormTemplateContext.GE.Save();
            }
            catch (Exception ex)
            {
                ExceptionManager.Publish(ex);
            }
        } //End Sender Id

       

        }//End Class

}//End Namespace