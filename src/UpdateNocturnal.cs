using System;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
class UpdateNocturnal {
 [STAThread]static int Main(string[] args){bool quiet=args.Length>0&&args[0]=="/quiet";try{
  string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Nocturnal");if(args.Length>1)root=Path.GetFullPath(args[1]);string exe=Path.Combine(root,"Nocturnal.exe");
  Directory.CreateDirectory(root);
  if(Process.GetProcessesByName("Nocturnal").Length>0)throw new Exception("Feche o Nocturnal antes de atualizar. Seus downloads não serão interrompidos automaticamente.");
  string stamp=DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,6);string backup=Path.Combine(root,"backups",stamp);Directory.CreateDirectory(backup);
  string queue=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Nocturnal","queue.json");if(args.Length>1)queue=Path.Combine(Directory.GetParent(root).Parent.FullName,"Nocturnal","queue.json");if(File.Exists(queue))File.Copy(queue,Path.Combine(backup,"queue.json"));
  foreach(string name in new[]{"nocturnal.ico","nocturnal.png"}){string old=Path.Combine(root,name);if(File.Exists(old))File.Copy(old,Path.Combine(backup,name));}
  string temp=Path.Combine(root,"Nocturnal-update-"+stamp+".tmp");try{using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("app"))using(var output=File.Create(temp)){input.CopyTo(output);}if(File.Exists(exe))File.Replace(temp,exe,Path.Combine(backup,"Nocturnal.exe"));else File.Move(temp,exe);}finally{if(File.Exists(temp))File.Delete(temp);}
  using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("icon"))using(var output=File.Create(Path.Combine(root,"nocturnal.ico"))){input.CopyTo(output);}using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("photo"))using(var output=File.Create(Path.Combine(root,"nocturnal.png"))){input.CopyTo(output);}
  if(!quiet){MessageBox.Show("Nocturnal instalado.\n\nProgresso do recorte com tempo processado e limites de espera na conexão.\nA extensão existente continua usando nocturnal://.\n\nBackup salvo em:\n"+backup,"Nocturnal 0.25");Process.Start(exe);}Console.WriteLine("UPDATED "+exe+"\nBACKUP "+backup);return 0;
 }catch(Exception ex){if(!quiet)MessageBox.Show(ex.Message,"Instalação não concluída",MessageBoxButtons.OK,MessageBoxIcon.Error);Console.Error.WriteLine(ex.Message);return 1;}}
}









