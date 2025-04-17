using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Weaver_Diagnostic_Viewer
{
    public class PrimaryWindow : Window
    {
        internal MetadataStructs.Assembly[] Assemblies { get; init; }
        public PrimaryWindow()
        {
            
        }
    }
}
