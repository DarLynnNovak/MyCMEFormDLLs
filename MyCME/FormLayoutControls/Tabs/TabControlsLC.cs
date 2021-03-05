using Aptify.Framework.Application;
using Aptify.Framework.DataServices;
using Aptify.Framework.ExceptionManagement;
using Aptify.Framework.WindowsControls;
using System;


namespace ACSMyCMEFormDLLs.FormLayoutControls.Main
{
    public class TabControlsLC : FormTemplateLayout
    {

        private DataAction m_oda = new DataAction();
        private AptifyProperties m_oProps = new AptifyProperties();
        private AptifyApplication m_oApp = new AptifyApplication();

        private FormTemplateTab _xmlDataTab;
        private FormTemplateTab _xmlResponseTab;
        private FormTemplateTab _AttachmentsTab;

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

                if (_xmlDataTab == null || _xmlDataTab.IsDisposed)
                 {
                    _xmlDataTab = GetFormComponentByLayoutKey(this, "ACSCMEEventsSendToBroker Form - XML Data Tab") as FormTemplateTab;
                }
                if (_xmlResponseTab == null || _xmlResponseTab.IsDisposed)
                {
                    _xmlResponseTab = GetFormComponentByLayoutKey(this, "ACSCMEEventsSendToBroker Form - XML Response Tab") as FormTemplateTab;
                }

                if (_AttachmentsTab == null || _AttachmentsTab.IsDisposed)
                {
                    _AttachmentsTab = GetFormComponentByLayoutKey(this, "Attachments") as FormTemplateTab;
                }
                

                recordId = FormTemplateContext.GE.RecordID;
                if (userId != 11)
                {

                    if (_xmlDataTab != null)
                    {
                        _xmlDataTab.Hide();
                    }
                    if (_xmlResponseTab != null)
                    {
                        _xmlResponseTab.Hide();
                    }
                    if (_AttachmentsTab != null)
                    {
                        _AttachmentsTab.Hide();
                    }


                }
                else
                {
                    if (_xmlDataTab != null)
                    {
                        _xmlDataTab.Show();
                    }
                    if (_xmlResponseTab != null)
                    {
                        _xmlResponseTab.Show();
                    }
                    if (_AttachmentsTab != null)
                    {
                        _AttachmentsTab.Show();
                    }

                }
            }
            catch (Exception ex)
            { 
                ExceptionManager.Publish(ex);
            } 
        }//End Bind Controls 
     
       

        }//End Class

}//End Namespace