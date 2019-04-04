using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management;
using System.Dynamic;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows;

namespace TaskManager
{
    internal class TaskManagerViewModel : ViewModelBase
    {
        private ObservableCollection<TaskItem> _taskList;
        public ObservableCollection<TaskItem> TaskList { get => _taskList; set => Set(ref _taskList, value); }

        private string _newTaskRequest;
        public string NewTaskRequest { get => _newTaskRequest; set => Set(ref _newTaskRequest, value); }

        private TaskItem _selectedItem;
        public TaskItem SelectedItem { get => _selectedItem; set => Set(ref _selectedItem, value); }

        public TaskManagerViewModel()
        {
            TaskList = new ObservableCollection<TaskItem>();
            var processes = Process.GetProcesses();
            foreach (var t in processes)
            {
                var newTask = new TaskItem
                {
                    Name = t.ProcessName.ToString(),
                    PID = t.Id
                };
                newTask.UserName = GetProcessExtraInformation(newTask.PID);
                try
                {
                    newTask.CPU = $"{t.TotalProcessorTime.ToString()[0]}{t.TotalProcessorTime.ToString()[1]}";
                    newTask.Description = t.MainModule.FileVersionInfo.FileDescription;
                }
                catch (Exception)
                {
                    
                }
                newTask.Memory = (Convert.ToDouble(t.PrivateMemorySize64) / 1024) + " K";
                TaskList.Add(newTask);
            }

        }
        public string GetProcessExtraInformation(int processId)
        {
            
            var query = "Select * From Win32_Process Where ProcessID = " + processId;
            var searcher = new ManagementObjectSearcher(query);
            var processList = searcher.Get();

        
            dynamic response = new ExpandoObject();
            response.Description = "";
            response.Username = "Unknown";

            foreach (var o in processList)
            {
                var obj = (ManagementObject) o;

                object[] argList = { string.Empty, string.Empty };
                var returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    
                   return response.Username = argList[0];


                }
            }

            return "N/A";
        }

        private RelayCommand _endTaskCommand;
        public RelayCommand EndTaskCommand =>
            _endTaskCommand ?? (_endTaskCommand = new RelayCommand(
                () =>
                {
                    var request = Process.GetProcessById(SelectedItem.PID);
                    request.Kill();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TaskList.Remove(SelectedItem);
                    });
                    SelectedItem = null;
                }
            ));

        private RelayCommand _startTaskCommand;
        public RelayCommand StartTaskCommand =>
            _startTaskCommand ?? (_startTaskCommand = new RelayCommand(
                () =>
                {
                    if (!String.IsNullOrEmpty(NewTaskRequest))
                    {
                        Process.Start(NewTaskRequest);
                        var request = Process.GetProcessesByName(NewTaskRequest);
                        NewTaskRequest = "";
                        if (request != null)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                TaskList.Add(SelectedItem);
                            });
                        }
                    }
                    else
                        MessageBox.Show("Check the task name");
                    SelectedItem = null;
                }
            ));
    }

}
