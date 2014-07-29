using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole.Models
{
    public class KindleDocument
    {
        [DisplayName("Website URL")]
        public string URL { get; set; }
        [DisplayName("Email Address")]
        public string EmailAddress { get; set; }
    }
}
