﻿using System;
using System.Net;
using Caliburn.Micro;
using Herd;

namespace Badger.ViewModels
{
    public class HerdAgentViewModel : PropertyChangedBase
    {
        private HerdAgentInfo m_herdAgentInfo;


        public HerdAgentViewModel(HerdAgentInfo info)
        {
            m_herdAgentInfo = info;
        }

        //Interfaces to HerdAgentInfo object's properties. This avoids references to Caliburn from the Herd
        public IPEndPoint IpAddress
        {
            get { return m_herdAgentInfo.ipAddress; }
            set { m_herdAgentInfo.ipAddress = value; }
        }

        public string IpAddressString { get { return m_herdAgentInfo.ipAddressString; } set { } }

        public DateTime lastACK
        {
            get { return m_herdAgentInfo.lastACK; }
            set { m_herdAgentInfo.lastACK = value; }
        }

        public int NumProcessors { get { return m_herdAgentInfo.NumProcessors; } }

        public string ProcessorArchitecture { get { return m_herdAgentInfo.ProcessorArchitecture; } }

        public string FormatedProcessorInfo
        {
            get
            {
                int cpus = m_herdAgentInfo.NumProcessors;
                string info = m_herdAgentInfo.ProcessorArchitecture + ", ";
                info += (cpus > 1) ? cpus + " CPUs" : cpus + " CPU";
                return info;
            }
        }

        public string ProcessorLoad { get { return m_herdAgentInfo.ProcessorLoad.ToString("0.00") + "%"; } }

        public string Memory
        {
            get
            {
                double totalMem = m_herdAgentInfo.Memory / 1024 / 1024;
                return totalMem.ToString("0.") + " MB";
            }
        }

        public bool IsAvailable { get { return m_herdAgentInfo.IsAvailable; } }

        public string Version { get { return m_herdAgentInfo.Version; } }

        public string Status
        {
            get { return m_herdAgentInfo.State; }
            set { m_herdAgentInfo.State = value; NotifyOfPropertyChange(() => Status); }
        }
    }
}
