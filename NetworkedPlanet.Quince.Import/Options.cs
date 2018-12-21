using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkedPlanet.Quince.Import
{
    internal class Options
    {
        public string RepoDirectory { get; set; }
        public string ImportFile { get; set; }
        public Uri GraphUri { get; set; }
    }
}
