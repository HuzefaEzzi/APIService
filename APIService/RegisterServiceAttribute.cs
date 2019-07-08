using System;

namespace APIService
{
    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    sealed class RegisterServiceAttribute : Attribute
    {
        // See the attribute guidelines at 
        readonly string name;

        // This is a positional argument
        public RegisterServiceAttribute(string name)
        {
            this.name = name;

            // TODO: Implement code here

        }

        public string Name
        {
            get { return name; }
        }

    }
}
