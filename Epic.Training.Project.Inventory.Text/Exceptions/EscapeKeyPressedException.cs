using System;

namespace Epic.Training.Project.Inventory.Text.Exceptions
{
    class EscapeKeyPressedException : Exception
    {
        public EscapeKeyPressedException()
        { }

        public EscapeKeyPressedException(string message) 
            : base(message)
        {   
        }
    }
}
