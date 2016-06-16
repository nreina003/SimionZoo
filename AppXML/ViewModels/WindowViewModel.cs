﻿using AppXML.Models;
using AppXML.Data;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows;
using Caliburn.Micro;
using System.Windows.Forms;
using System.IO;
using System.Dynamic;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.IO.Pipes;
using System.Xml.Linq;
using System.Linq;
using System.Xml.XPath;
using System.Windows.Media;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;
using Herd;
using AppXML.ViewModels;
using System.Threading.Tasks.Dataflow;

namespace AppXML.ViewModels
{
    public interface IValidable
    {
        bool validate();
    }
    public interface IGetXml
    {
        List<XmlNode> getXmlNode();
    }
    public static class ParallelAsync
    {
        public static bool ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body)
        {
            Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
            return true;
        }
    }

    public class WindowViewModel : PropertyChangedBase
    {
        public double ControlHeight
        {
            get
            {
                return (int)System.Windows.SystemParameters.PrimaryScreenHeight;
            }
            set { }
        }
        public double ControlWidth
        {
            get
            {
                return System.Windows.SystemParameters.PrimaryScreenWidth;
            }
            set { }
        }

        private CNode _rootnode;
        private ObservableCollection<BranchViewModel> _branches;
        private XmlDocument _doc;
        //private RightTreeViewModel _graf;
        public ObservableCollection<ValidableAndNodeViewModel> Branch { get { return _branches[0].Class.AllItems; } set { } }

        private ExperimentQueueViewModel m_experimentQueueViewModel = new ExperimentQueueViewModel();
        public ExperimentQueueViewModel experimentQueueViewModel { get { return m_experimentQueueViewModel; }
            set { m_experimentQueueViewModel = value;
            NotifyOfPropertyChange(() => experimentQueueViewModel);
            }
        }
        private ShepherdViewModel m_shepherdViewModel = new ShepherdViewModel();
        public ShepherdViewModel shepherdViewModel { get { return m_shepherdViewModel; } set { } }

        private bool m_bIsExperimentQueueNotEmpty = false;
        public bool bIsExperimentQueueNotEmpty
        {
            get { return m_bIsExperimentQueueNotEmpty; }
            set { m_bIsExperimentQueueNotEmpty = value;
            NotifyOfPropertyChange(() => bIsExperimentQueueNotEmpty);
            }
        }
        private void checkStackEmpty()
        {
            bool wasEmpty = !m_bIsExperimentQueueNotEmpty;
            if (wasEmpty != m_experimentQueueViewModel.isEmpty())
            {
                m_bIsExperimentQueueNotEmpty = !m_experimentQueueViewModel.isEmpty();
                NotifyOfPropertyChange(() => bIsExperimentQueueNotEmpty);
            }
        }

        private bool m_bIsExperimentRunning = false;
        public bool bIsExperimentRunning
        {
            get { return m_bIsExperimentRunning; }
            set{m_bIsExperimentRunning= value;
            NotifyOfPropertyChange(()=>bIsExperimentRunning);
            NotifyOfPropertyChange(()=>bIsExperimentNotRunning);} }
        public bool bIsExperimentNotRunning
        {
            get { return !m_bIsExperimentRunning; }
            set { }
        }

        private ObservableCollection<string> _apps = new ObservableCollection<string>();
        public ObservableCollection<string> Apps { get { return _apps; } set { } }
      
        private string[] apps;
        private string selectedApp;

        public string SelectedApp { get { return selectedApp; } 
            set 
            {
                CNode.cleanAll();
                CApp.cleanApp();

                int index = _apps.IndexOf(value);
                if (index == -1)
                    return;
                selectedApp = value;
                CApp.IsInitializing = true;
                _rootnode = Utility.getRootNode(apps[index]);
                _branches = _rootnode.children;
                _doc = (this._rootnode as CApp).document;
                CApp.IsInitializing = false;
                NotifyOfPropertyChange(() => Branch);
                NotifyOfPropertyChange(() => rootnode);
            } 
        }
       
        public void Change(object sender)
        {
            var x = sender as System.Windows.Controls.TreeView;
            var y = x.SelectedItem;
        }
        public WindowViewModel()
        {
           
             
             //_windowManager = windowManager;
            CApp.IsInitializing = true;
            apps = Directory.GetFiles("..\\config\\apps");
            getAppsNames();
            selectedApp = Apps[0];
            _rootnode = Utility.getRootNode(apps[0]);
            _branches = _rootnode.children;
            _doc = (this._rootnode as CApp).document;
            CApp.IsInitializing = false;
            m_experimentQueueViewModel.setParent(this);
        }
        private void getAppsNames()
        {
            foreach(string app in apps)
            {
                char[] spliter = "\\".ToCharArray();
                string[] tmp = app.Split(spliter);
                tmp = tmp[tmp.Length - 1].Split('.');
                string name =tmp[0];
                _apps.Add(name);
            
            }
        }

        public ObservableCollection<BranchViewModel> Branches { get { return _branches; } set { } }
        public CNode rootnode
        {
            get
            {
                return _rootnode;
            }
            set
            {
                _rootnode = value;
            }
        }

        public void saveExperimentInEditor()
        {
            if (!validate())
                return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Experiment | *.experiment";
            sfd.InitialDirectory = "../experiments";
            string CombinedPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "../experiments");
            if (!Directory.Exists(CombinedPath))
                System.IO.Directory.CreateDirectory(CombinedPath);
            sfd.InitialDirectory = System.IO.Path.GetFullPath(CombinedPath); 
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _doc.Save(sfd.FileName);
            }
        }
        
        private bool validate()
        {
            _doc.RemoveAll();
            XmlNode rootNode = _doc.CreateElement(_rootnode.name);
            foreach (BranchViewModel branch in _branches)
            {
                if (!branch.validate())
                {
                    DialogViewModel dvm = new DialogViewModel(null, "Error validating the form. Please check form", DialogViewModel.DialogType.Info);
                    dynamic settings = new ExpandoObject();
                    settings.WindowStyle = WindowStyle.ThreeDBorderWindow;
                    settings.ShowInTaskbar = true;
                    settings.Title = "ERROR";

                    new WindowManager().ShowDialog(dvm, null, settings);

                    return false;
                }
                else
                {
                    rootNode.AppendChild(branch.getXmlNode()[0]);
                }
            }
            _doc.AppendChild(rootNode);
            return true;
        }
       
        public void loadExperiment()
        {
            
            string fileDoc = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Experiment | *.experiment";
            ofd.InitialDirectory = Path.Combine(Path.GetDirectoryName(Directory.GetCurrentDirectory()),"experiments");
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fileDoc = ofd.FileName;
            }
            else
                return;
            
            XmlDocument loadedDocument = new XmlDocument();
            loadedDocument.Load(fileDoc);

            loadExperimentInEditor(loadedDocument);
            //Graf = null;
            
           
           
        }
       
        public void loadExperimentInEditor(XmlDocument experimentXML)
        {
            _doc.RemoveAll();

            //update the app if we need to
            XmlNode experimentNode = experimentXML.FirstChild;
            if (!experimentNode.Name.Equals(selectedApp))
            {
                SelectedApp = experimentNode.Name;
                NotifyOfPropertyChange(() => SelectedApp);

            }
            foreach (BranchViewModel branch in _branches)
            {
                //we have to find the correct data input for every branch we have in the form. If we do not have data we do nothing
                if (experimentNode.HasChildNodes)
                {
                    foreach (XmlNode dataNode in experimentNode.ChildNodes)
                    {
                        if (dataNode.Name == branch.Name)
                        {
                           Utility.fillTheClass(branch.Class, dataNode);
                        }
                    }
                }
            }

        }

        private void setAsNull()
        {
            foreach(BranchViewModel branch in _branches)
            {
                branch.setAsNull();
            }
        }

        public void clearExperimentQueue()
        {
            if (m_experimentQueueViewModel!=null)
            {
                m_experimentQueueViewModel.clear();
                NotifyOfPropertyChange(() => experimentQueueViewModel);
            }
        }
        public void modifySelectedExperiment()
        {
            if (!validate())
                return;
            if (m_experimentQueueViewModel!= null)
            {
                XmlDocument document = new XmlDocument();
                XmlNode newRoot = document.ImportNode(_doc.DocumentElement, true);
                document.AppendChild(newRoot);

                m_experimentQueueViewModel.modifySelectedExperiment(document);
                NotifyOfPropertyChange(() => experimentQueueViewModel);
            }
        }
        public void removeSelectedExperiments()
        {
            if (m_experimentQueueViewModel != null)
            {
                m_experimentQueueViewModel.removeSelectedExperiments();
                NotifyOfPropertyChange(() => experimentQueueViewModel);
                checkStackEmpty();
            }

        }
        public void addExperiment()
        {
            if (!validate())
                return;
            
            XmlDocument document = new XmlDocument();

            XmlNode newRoot = document.ImportNode(_doc.DocumentElement, true);
            document.AppendChild(newRoot);
            //document.Save("copia.tree");
            AppXML.ViewModels.ExperimentViewModel experiment = new AppXML.ViewModels.ExperimentViewModel("Experiment", document);
            m_experimentQueueViewModel.addExperiment(experiment);
            NotifyOfPropertyChange(() => experimentQueueViewModel);
            checkStackEmpty();
        }
        public void runExperiments()
        {
            bool bSuccesfulSave= experimentQueueViewModel.save();


            if (bSuccesfulSave)
            {
                bIsExperimentRunning = true;

                runExperimentQueue();

                bIsExperimentRunning = false;
            }
               
        }

        private CancellationTokenSource m_cancelTokenSource = new CancellationTokenSource();
        
        public void stopExperiments()
        {
            if (m_cancelTokenSource != null)
                m_cancelTokenSource.Cancel();
            experimentQueueViewModel.resetState();
        }

        void runExperimentQueue()
        {
            Task.Factory.StartNew(() =>
                {
                    if (shepherdViewModel.herdAgentList.Count > 0)
                    {
                        //RUN REMOTELY
                        runExperimentQueueRemotely();
                    }
                    else
                    {
                        //RUN LOCALLY
                    }
                });
        }




        

        private async void runExperimentQueueRemotely()
        {
            List<HerdAgentViewModel> freeHerdAgents= new List<HerdAgentViewModel>();
            List<HerdAgentViewModel> usedHerdAgents= new List<HerdAgentViewModel>();
            List<ExperimentViewModel> pendingExperiments = new List<ExperimentViewModel>();
            List<ExperimentViewModel> assignedExperiments = new List<ExperimentViewModel>();
            List<Badger> badgers = new List<Badger>();


            //get experiment list
            experimentQueueViewModel.getEnqueuedExperimentList(ref pendingExperiments);
            experimentQueueViewModel.enableEdition(false);

            List<Task<Badger>> badgerList = new List<Task<Badger>>();
            //get available herd agents list. Inside the loop to update the list
            shepherdViewModel.getAvailableHerdAgents(ref freeHerdAgents);

            //assign experiments to free agents
            Badger.assignExperiments(ref pendingExperiments, ref freeHerdAgents
                , ref badgers, m_cancelTokenSource.Token);
            try
            {
                while (pendingExperiments.Count>0 && !m_cancelTokenSource.IsCancellationRequested)
                {
                    foreach(Badger badger in badgers)
                    {
                        badgerList.Add( badger.sendJobAndMonitor(experimentQueueViewModel.name));
                    }

                    //wait for the first agent to finish and give it something to do
                    Task<Badger> finishedTask= await Task.WhenAny(badgerList);
                    Badger finishedTaskResult = await finishedTask;
                    badgerList.Remove(finishedTask);
                    
                    if (finishedTaskResult.failedExperiments.Count>0)
                    {
                        foreach (ExperimentViewModel exp in finishedTaskResult.failedExperiments)
                            pendingExperiments.Add(exp);
                    }

                    //get available herd agents list. Inside the loop to update the list
                    shepherdViewModel.getAvailableHerdAgents(ref freeHerdAgents);
                    //just in case the freed agent hasn't still been discovered by the shepherd
                    if (!freeHerdAgents.Contains(finishedTaskResult.herdAgent))
                        freeHerdAgents.Add(finishedTaskResult.herdAgent);

                    //assign experiments to free agents
                    Badger.assignExperiments(ref pendingExperiments, ref freeHerdAgents
                        , ref badgers, m_cancelTokenSource.Token);
                }
                Task.WhenAll(badgerList).Wait();
            }
            catch (Exception ex)
            {
                int a= 6;
            }
            experimentQueueViewModel.enableEdition(true);
        }

        
     
        public void loadExperimentQueue()
        {
            string fileDoc = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Experiment batch | *.exp-batch";
            ofd.InitialDirectory = Path.Combine(Path.GetDirectoryName(Directory.GetCurrentDirectory()), "experiments");
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                fileDoc = ofd.FileName;
            }
            else
                return;
            //this doesn't seem to work
            //Cursor.Current = Cursors.WaitCursor;
            //System.Windows.Forms.Application.DoEvents();

            //LOAD THE EXPERIMENT BATCH IN THE QUEUE
            XmlDocument batchDoc = new XmlDocument();
            batchDoc.Load(fileDoc);
            XmlElement fileRoot = batchDoc.DocumentElement;
            if (fileRoot.Name != "Experiments")
                return;

            foreach (XmlElement element in fileRoot.ChildNodes)
            {
                try
                {
                    string expName = element.Name;
                    string path = element.Attributes["Path"].Value;
                    if (File.Exists(path))
                    {
                        XmlDocument expDocument = new XmlDocument();
                        expDocument.Load(path);
                        m_experimentQueueViewModel.addExperiment(element.Name, expDocument);
                        checkStackEmpty();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }

            m_experimentQueueViewModel.markModified(true);

        }

        public void saveExperimentQueue()
        {
            m_experimentQueueViewModel.save();
         }
       
    }
}
