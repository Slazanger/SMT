// THIS SOFTWARE COMES "AS IS", WITH NO WARRANTIES.  THIS
// MEANS NO EXPRESS, IMPLIED OR STATUTORY WARRANTY, INCLUDING
// WITHOUT LIMITATION, WARRANTIES OF MERCHANTABILITY OR FITNESS
// FOR A PARTICULAR PURPOSE OR ANY WARRANTY OF TITLE OR
// NON-INFRINGEMENT.
//
// MICROSOFT WILL NOT BE LIABLE FOR ANY DAMAGES RELATED TO
// THE SOFTWARE, INCLUDING DIRECT, INDIRECT, SPECIAL,
// CONSEQUENTIAL OR INCIDENTAL DAMAGES, TO THE MAXIMUM EXTENT
// THE LAW PERMITS, NO MATTER WHAT LEGAL THEORY IT IS
// BASED ON.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace WpfHelpers.WpfDataManipulation.Commands
{
    /// <summary>
    /// A map that exposes commands in a WPF binding friendly manner
    /// </summary>
    [TypeDescriptionProvider(typeof(CommandMapDescriptionProvider))]
    public class CommandMap
    {
        /// <summary>
        /// Store the commands
        /// </summary>
        private Dictionary<string, ICommand> _commands;

        /// <summary>
        /// Expose the dictionary of commands
        /// </summary>
        protected Dictionary<string, ICommand> Commands
        {
            get
            {
                if (null == _commands)
                    _commands = new Dictionary<string, ICommand>();

                return _commands;
            }
        }

        /// <summary>
        /// Add a named command to the command map
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <param name="executeMethod">The method to execute</param>
        public void AddCommand(string commandName, Action<object> executeMethod)
        {
            Commands[commandName] = new DelegateCommand(executeMethod);
        }

        /// <summary>
        /// Add a named command to the command map
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        /// <param name="executeMethod">The method to execute</param>
        /// <param name="canExecuteMethod">The method to execute to check if the command can be executed</param>
        public void AddCommand(string commandName, Action<object> executeMethod, Predicate<object> canExecuteMethod)
        {
            Commands[commandName] = new DelegateCommand(executeMethod, canExecuteMethod);
        }

        /// <summary>
        /// Remove a command from the command map
        /// </summary>
        /// <param name="commandName">The name of the command</param>
        public void RemoveCommand(string commandName)
        {
            Commands.Remove(commandName);
        }

        /// <summary>
        /// Expose the dictionary entries of a CommandMap as properties
        /// </summary>
        private class CommandMapDescriptionProvider : TypeDescriptionProvider
        {
            /// <summary>
            /// Standard constructor
            /// </summary>
            public CommandMapDescriptionProvider()
                : this(TypeDescriptor.GetProvider(typeof(CommandMap)))
            {
            }

            /// <summary>
            /// Construct the provider based on a parent provider
            /// </summary>
            /// <param name="parent"></param>
            public CommandMapDescriptionProvider(TypeDescriptionProvider parent)
                : base(parent)
            {
            }

            /// <summary>
            /// Get the type descriptor for a given object instance
            /// </summary>
            /// <param name="objectType">The type of object for which a type descriptor is requested</param>
            /// <param name="instance">The instance of the object</param>
            /// <returns>A custom type descriptor</returns>
            public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
            {
                return new CommandMapDescriptor(base.GetTypeDescriptor(objectType, instance), instance as CommandMap);
            }
        }

        /// <summary>
        /// This class is responsible for providing custom properties to WPF - in this instance
        /// allowing you to bind to commands by name
        /// </summary>
        private class CommandMapDescriptor : CustomTypeDescriptor
        {
            private CommandMap _map;

            /// <summary>
            /// Store the command map for later
            /// </summary>
            /// <param name="descriptor"></param>
            /// <param name="map"></param>
            public CommandMapDescriptor(ICustomTypeDescriptor descriptor, CommandMap map)
                : base(descriptor)
            {
                _map = map;
            }

            /// <summary>
            /// Get the properties for this command map
            /// </summary>
            /// <returns>A collection of synthesized property descriptors</returns>
            public override PropertyDescriptorCollection GetProperties()
            {
                //TODO: See about caching these properties (need the _map to be observable so can respond to add/remove)
                PropertyDescriptor[] props = new PropertyDescriptor[_map.Commands.Count];

                int pos = 0;

                foreach (KeyValuePair<string, ICommand> command in _map.Commands)
                    props[pos++] = new CommandPropertyDescriptor(command);

                return new PropertyDescriptorCollection(props);
            }
        }

        /// <summary>
        /// A property descriptor which exposes an ICommand instance
        /// </summary>
        private class CommandPropertyDescriptor : PropertyDescriptor
        {
            /// <summary>
            /// Store the command which will be executed
            /// </summary>
            private ICommand _command;

            /// <summary>
            /// Construct the descriptor
            /// </summary>
            /// <param name="command"></param>
            public CommandPropertyDescriptor(KeyValuePair<string, ICommand> command)
                : base(command.Key, null)
            {
                _command = command.Value;
            }

            /// <summary>
            /// Not needed
            /// </summary>
            public override Type ComponentType
            {
                get { throw new NotImplementedException(); }
            }

            /// <summary>
            /// Always read only in this case
            /// </summary>
            public override bool IsReadOnly
            {
                get { return true; }
            }

            /// <summary>
            /// Get the type of the property
            /// </summary>
            public override Type PropertyType
            {
                get { return typeof(ICommand); }
            }

            /// <summary>
            /// Nope, it's read only
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public override bool CanResetValue(object component)
            {
                return false;
            }

            /// <summary>
            /// Get the ICommand from the parent command map
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public override object GetValue(object component)
            {
                CommandMap map = component as CommandMap;

                if (null == map)
                    throw new ArgumentException("component is not a CommandMap instance", "component");

                return map.Commands[this.Name];
            }

            /// <summary>
            /// Not needed
            /// </summary>
            /// <param name="component"></param>
            public override void ResetValue(object component)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Not needed
            /// </summary>
            /// <param name="component"></param>
            /// <param name="value"></param>
            public override void SetValue(object component, object value)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Not needed
            /// </summary>
            /// <param name="component"></param>
            /// <returns></returns>
            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        /// <summary>
        /// Implements ICommand in a delegate friendly way
        /// </summary>
        private class DelegateCommand : ICommand
        {
            private Predicate<object> _canExecuteMethod;

            private Action<object> _executeMethod;

            /// <summary>
            /// Create a command that can always be executed
            /// </summary>
            /// <param name="executeMethod">The method to execute when the command is called</param>
            public DelegateCommand(Action<object> executeMethod) : this(executeMethod, null) { }

            /// <summary>
            /// Create a delegate command which executes the canExecuteMethod before executing the executeMethod
            /// </summary>
            /// <param name="executeMethod"></param>
            /// <param name="canExecuteMethod"></param>
            public DelegateCommand(Action<object> executeMethod, Predicate<object> canExecuteMethod)
            {
                if (null == executeMethod)
                    throw new ArgumentNullException("executeMethod");

                this._executeMethod = executeMethod;
                this._canExecuteMethod = canExecuteMethod;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public bool CanExecute(object parameter)
            {
                return (null == _canExecuteMethod) ? true : _canExecuteMethod(parameter);
            }

            public void Execute(object parameter)
            {
                _executeMethod(parameter);
            }
        }
    }
}