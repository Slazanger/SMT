using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace WpfHelpers.WpfDataManipulation.Commands.Async
{
    public class NotifyTaskCompletionBase : INotifyPropertyChanged
    {
        public NotifyTaskCompletionBase(Task task)
        {
            Task = task;
            TaskCompletion = WatchTaskAsync(task);
        }

        public Task Task { get; protected set; }

        public bool IsCanceled { get { return Task.IsCanceled; } }
        public bool IsFaulted { get { return Task.IsFaulted; } }
        public TaskStatus Status { get { return Task.Status; } }
        public bool IsCompleted { get { return Task.IsCompleted; } }
        public bool IsNotCompleted { get { return !Task.IsCompleted; } }
        public bool IsSuccessfullyCompleted
        {
            get
            {
                return Task.Status ==
                       TaskStatus.RanToCompletion;
            }
        }

        public Task TaskCompletion { get; protected set; }
        public AggregateException Exception { get { return Task.Exception; } }
        public Exception InnerException
        {
            get
            {
                return (Exception == null) ?
                    null : Exception.InnerException;
            }
        }
        public string ErrorMessage
        {
            get
            {
                return (InnerException == null) ?
                    null : InnerException.Message;
            }
        }


        protected async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }
            var propertyChanged = PropertyChanged;

            if (propertyChanged == null)
                return;
            propertyChanged(this, new PropertyChangedEventArgs("Status"));
            propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
            propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));
            if (task.IsCanceled)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
            }
            else if (task.IsFaulted)
            {
                propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
                propertyChanged(this, new PropertyChangedEventArgs("Exception"));
                propertyChanged(this, new PropertyChangedEventArgs("InnerException"));
                propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
            }
            else
            {
                propertyChanged(this, new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
                propertyChanged(this, new PropertyChangedEventArgs("Result"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public sealed class NotifyTaskCompletion<TResult> : NotifyTaskCompletionBase
    {
        public NotifyTaskCompletion(Task<TResult> task) : base(task)
        {
        }
        
        public TResult Result
        {
            get
            {
                return (Task.Status == TaskStatus.RanToCompletion) ?
                    ((Task<TResult>)Task).Result : default(TResult);
            }
        }
    }
}