﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Fomin05
{
    internal class ProcessesListViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Process> _processes;
        private readonly Action<bool> _showLoaderAction;
        private readonly Thread _updateThread;
        private RelayCommand _endTaskCommand;

        public bool IsItemSelected => SelectedProcess != null;

        private Process _selectedProcess;

        public Process SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                _selectedProcess = value;
                OnPropertyChanged();
                OnPropertyChanged("IsItemSelected");
            }
        }

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            private set
            {
                _processes = value;
                OnPropertyChanged();
            }
        }

        internal ProcessesListViewModel(Action<bool> showLoaderAction)
        {
            _showLoaderAction = showLoaderAction;
            _updateThread = new Thread(UpdateUsers);
            InitializeUsers();
            _updateThread.Start();
        }

        public RelayCommand EndTaskCommand
        {
            get { return _endTaskCommand ?? (_endTaskCommand = new RelayCommand(EndTaskImpl)); }
        }

        private async void EndTaskImpl(object o)
        {
            try
            {
                await Task.Run(() =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(delegate
                    {
                        System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(SelectedProcess.Id);
                        process.Kill();
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void UpdateUsers()
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(delegate
                    {
                        try
                        {
                            List<Process> toRemove = new List<Process>(Processes.Where(proc => !ProcessDb.Processes.ContainsKey(proc.Id)));
                            foreach (Process proc in toRemove)
                            {
                                Processes.Remove(proc);
                            }
                            List<Process> toAdd = new List<Process>(ProcessDb.Processes.Values.Where(proc => !Processes.Contains(proc)));
                            foreach (Process proc in toAdd)
                            {
                                Processes.Add(proc);
                            }
                        }
                        catch (NullReferenceException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                        catch (ArgumentNullException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    });
                });
                Thread.Sleep(4000);
            }
        }

        private async void InitializeUsers()
        {
            _showLoaderAction.Invoke(true);
            await Task.Run(() =>
            {
                do
                {
                    Processes = new ObservableCollection<Process>(ProcessDb.Processes.Values);
                    Thread.Sleep(5000);
                } while (Processes.Count == 0);

            });
            _showLoaderAction.Invoke(false);
        }

        internal void Close()
        {
            _updateThread.Join(3000);
        }

        #region Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
