﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using System.Windows.Threading;
using System.Dynamic;
using System.Windows;
using System.Xml;

namespace AppXML.ViewModels
{
    public class WindowClassViewModel : Screen
    {

        private ButtonToWindowedClassViewModel _father;
        private ClassViewModel m_Class;
        private string m_name;
        public string name { get { return m_name; } set { m_name = value; } }
        public ClassViewModel Class { get { return m_Class; }
            set { m_Class = value; }
        }

        public bool validate()
        {
            return m_Class.validate(true);
        }

        public WindowClassViewModel(string className,string name,ButtonToWindowedClassViewModel father,XmlDocument doc)
        {
            m_name = name;
            _father = father;
            m_Class = new ClassViewModel(className, name, false, doc);
            validate();

            Models.CApp.addNewClass(m_Class);
        }
        public void Save()
        {
            
           // _father.ResumeClass = this.Class;
            bool ok = _father.validate();
            
            if (!ok)
            {
                DialogViewModel dvm = new DialogViewModel(null, "The form is not validated. Do you want to save it?", DialogViewModel.DialogType.YesNo);
                dynamic settings = new ExpandoObject();
                settings.WindowStyle = WindowStyle.ToolWindow;
                settings.ShowInTaskbar = true;
                settings.Title = "WARNING";

                new WindowManager().ShowDialog(dvm, null, settings);

                if (dvm.DialogResult == DialogViewModel.Result.OK)
                {
                    TryClose();
                }
            }
            else
            {
                TryClose();
            }

        }


    }
}
