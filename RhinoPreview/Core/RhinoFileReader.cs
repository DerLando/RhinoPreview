using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.FileIO;

namespace RhinoPreview.Core
{
    public static class RhinoFileReader
    {
        public static File3dm ReadFile(string filename)
        {
            return Rhino.FileIO.File3dm.Read(filename);
        }


    }
}
