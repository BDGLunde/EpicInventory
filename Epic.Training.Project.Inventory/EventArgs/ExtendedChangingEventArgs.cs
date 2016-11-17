using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epic.Training.Project.Inventory.EventArgs
{
    public class ExtendedChangingEventArgs : PropertyChangingEventArgs
    {
        private string _proposedName;
        public string ProposedName
        {
            get
            {
                return _proposedName;
            }
        }

        
        public ExtendedChangingEventArgs(string propertyName, string proposedName) : base(propertyName)
        {
            _proposedName = proposedName;       
        }
    }
}
