using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace APIService
{
    [RunInstaller(true)]
    public partial class WebAPISelfHosting : System.Configuration.Install.Installer
    {
        public WebAPISelfHosting()
        {
            InitializeComponent();
        }
    }
}
