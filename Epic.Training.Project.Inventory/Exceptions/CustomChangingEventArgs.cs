using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epic.Training.Project.Inventory.Exceptions
{
    class CustomChangingEventArgs : PropertyChangingEventArgs
    {
        public CustomChangingEventArgs(string propertyName) : base(propertyName)
        {
        }
    }
}
