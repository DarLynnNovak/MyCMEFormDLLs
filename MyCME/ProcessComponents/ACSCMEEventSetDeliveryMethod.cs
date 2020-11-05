using Aptify.Framework.Application;
using Aptify.Framework.BusinessLogic.GenericEntity;
using Aptify.Framework.BusinessLogic.ProcessPipeline;
using Aptify.Framework.DataServices;
using System;

namespace ACSMyCMEFormDLLs.ProcessComponents
{
    public class ACSCMEEventSetDeliveryMethod : IProcessComponent
    {
        private AptifyApplication m_oApp = new AptifyApplication();
        private AptifyProperties m_oProps = new AptifyProperties();
        private DataAction m_oda;

        private string m_sResult = "SUCCESS";
        AptifyGenericEntity AcsCmeEventGE;
        AptifyGenericEntityBase EventGE;
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
                DataAction da = new DataAction();
                DateTime Time = DateTime.Now;
                long EventTypeId = 0;
                long RecordId = 0;

                AcsCmeEventGE = (AptifyGenericEntity)m_oProps.GetProperty("AcsCmeEventGE");  //this is our object being passed in when we save an acs cme event record.
                RecordId = Convert.ToInt64(AcsCmeEventGE.GetValue("Id"));
                EventTypeId = Convert.ToInt64(AcsCmeEventGE.GetValue("EventType"));
                EventGE = m_oApp.GetEntityObject("ACSCMEEvent", RecordId);

                if (EventTypeId == 1 ) //Live events
                {
                    EventGE.SetValue("BrokerDeliveryMethodType", "LIVE");
                }
                if (EventTypeId == 2) //Enduring events
                {
                    EventGE.SetValue("BrokerDeliveryMethodType", "ANYTIME");
                }
                if (EventTypeId == 13) //Other events
                { 
                    EventGE.SetValue("BrokerDeliveryMethodType", "ANYTIME");
                } 

                if (Convert.ToString(AcsCmeEventGE.GetValue("CME_Program")) == "%Ground Roun%")
                {
                    EventGE.SetValue("BrokerDeliveryMethodType", "LIVE");
                }

                if (EventGE.IsDirty)
                {
                    if (!EventGE.Save(false))
                    {
                        m_sResult = "FAILED";
                        throw new Exception("Problem Saving Event Record:" + EventGE.RecordID);
                        
                    }
                    else 
                    {
                        EventGE.Save(true);
                        m_sResult = "SUCCESS";
                    }

                }
               

            }
            catch (Exception ex)
            {
                Aptify.Framework.ExceptionManagement.ExceptionManager.Publish(ex);
                return "FAILED";
            }
            return m_sResult;
        }

    }
}
