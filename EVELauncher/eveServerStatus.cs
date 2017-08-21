using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVELauncher
{
    public class eveServerStatus
    {
        public eveServerStatus_eveApi eveApi = new eveServerStatus_eveApi();
    }
    public class eveServerStatus_xml
    {
        public string version { get; set; }
        public string encoding { get; set; }
    }
    public class eveServerStatus_eveApi
    {
        public string version { get; set; }
        public string currentTime { get; set; }
        public eveServerStatus_result result = new eveServerStatus_result();
        public string cachedUntil { get; set; }
    }
    public class eveServerStatus_result
    {
        public string serverOpen { get; set; }
        public string onlinePlayers { get; set; }
    }
}
