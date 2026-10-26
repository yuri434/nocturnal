using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
public static class AppBrand {
 [System.Runtime.InteropServices.DllImport("shell32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern int SetCurrentProcessExplicitAppUserModelID(string id);
 public static void Initialize(){SetCurrentProcessExplicitAppUserModelID("Nocturnal.DownloadManager");}
 static readonly Icon logo=Read();
 static Icon Read(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("logo")){return stream==null?SystemIcons.Application:new Icon(stream,new Size(32,32));}}
 public static void Apply(Form form){form.Icon=logo;}
}
