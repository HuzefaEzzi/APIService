using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace APIService
{
    public class ValuesController:ApiController
    {
        public string GetString(Int32 id)
        {
            return "this is a string";
        }
    }
}
