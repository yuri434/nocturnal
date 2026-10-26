using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class BandwidthGate {
 readonly SemaphoreSlim gate=new SemaphoreSlim(1,1);
 public async Task Wait(int bytes,int kbps,CancellationToken ct){if(kbps<=0)return;await gate.WaitAsync(ct);try{await Task.Delay((int)Math.Ceiling(bytes*1000.0/(kbps*1024.0)),ct);}finally{gate.Release();}}
}

public class QueueOptions:Form {
 NumericUpDown count;CheckBox stop;
 public int Maximum{get{return (int)count.Value;}} public bool Stopped{get{return stop.Checked;}}
 static string Config{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Nocturnal","queue-limit.txt");}}
 public static int LoadMaximum(){int n;try{if(int.TryParse(File.ReadAllText(Config),out n))return Math.Max(1,Math.Min(8,n));}catch{}return 2;}
 public static void Store(int n){Directory.CreateDirectory(Path.GetDirectoryName(Config));File.WriteAllText(Config,n.ToString());}
 public QueueOptions(int n,bool stopped){Text="Nocturnal / Opções da fila";ClientSize=new Size(440,205);TransferWindow.Style(this);StartPosition=FormStartPosition.CenterParent;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
  Controls.Add(new Label{Text="Downloads simultâneos (1 a 8)",Left=20,Top=22,Width=285});count=new NumericUpDown{BackColor=Color.FromArgb(48,48,48),ForeColor=Color.WhiteSmoke,Left=315,Top=19,Width=90,Minimum=1,Maximum=8,Value=n};Controls.Add(count);
  stop=new CheckBox{Text="Parar fila e pausar downloads ativos",Left=20,Top=67,Width=390,Checked=stopped};Controls.Add(stop);
  Controls.Add(new Label{Text="Reduzir o limite não interrompe os downloads ativos.\nDesmarque Parar fila para permitir novos downloads.",Left=20,Top=104,Width=400,Height=40});var ok=TransferWindow.Button("Aplicar",305,161,100);ok.DialogResult=DialogResult.OK;Controls.Add(ok);AcceptButton=ok;
 }
}

public class TransferWindow:Form {
 Job job;Action toggle,save;Label info,source,connectionLabel;TextBox errorDetails;ProgressBar progress;DataGridView parts;Button pause,details;bool expanded;double mediaPercent;System.Windows.Forms.Timer timer;long previous;Stopwatch clock=Stopwatch.StartNew();
 public static void Style(Control c){if(c is Form)AppBrand.Apply((Form)c);c.BackColor=Color.FromArgb(48,48,48);c.ForeColor=Color.WhiteSmoke;c.Font=new Font("Segoe UI",9);}
 public static Button Button(string text,int x,int y,int w){return new Button{Text=text,Left=x,Top=y,Width=w,Height=28,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(55,55,55),ForeColor=Color.WhiteSmoke};}
 public static string Bytes(double n){if(n<0)return "—";string[] u={"B","KB","MB","GB","TB"};int i=0;while(n>=1024&&i<4){n/=1024;i++;}return n.ToString("0.##")+" "+u[i];}
 public TransferWindow(Job j,Action action,Action persist){job=j;toggle=action;save=persist;previous=j.Done;Text=j.Name+" — Nocturnal";ClientSize=new Size(550,267);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=true;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;Style(this);
  var tabs=new TabControl{Dock=DockStyle.Top,Height=190};var main=new TabPage("Progresso do download");Style(main);source=new Label{Dock=DockStyle.Top,Height=26,Padding=new Padding(9,5,9,0),AutoEllipsis=true,Text=j.Url};info=new Label{Dock=DockStyle.Fill,Padding=new Padding(9,6,9,4)};main.Controls.Add(info);main.Controls.Add(source);tabs.TabPages.Add(main);
  var limit=new TabPage("Limitar velocidade");Style(limit);limit.Controls.Add(new Label{Text="Limite por download em KB/s (0 = sem limite)",Left=12,Top=12,Width=500});var number=new NumericUpDown{BackColor=Color.FromArgb(40,40,40),ForeColor=Color.WhiteSmoke,Left=12,Top=40,Width=150,Maximum=1048576,Value=Math.Max(0,Math.Min(1048576,j.LimitKBps))};limit.Controls.Add(number);var apply=Button("Aplicar",178,38,100);apply.Click+=delegate{job.LimitKBps=(int)number.Value;save();};limit.Controls.Add(apply);limit.Controls.Add(new Label{Text="Arquivos: aplicado durante a transferência.\nVídeo/MP3: aplicado no próximo início ou retomada.\nO limite vale para este download, não para a soma de todos.",Left=12,Top=82,Width=510,Height=65});tabs.TabPages.Add(limit);
  var errors=new TabPage("Detalhes / erro");Style(errors);errorDetails=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.FromArgb(40,40,40),ForeColor=Color.WhiteSmoke,BorderStyle=BorderStyle.None};errors.Controls.Add(errorDetails);tabs.TabPages.Add(errors);Controls.Add(tabs);
  progress=new ProgressBar{Left=12,Top=200,Width=526,Height=14};Controls.Add(progress);
  details=Button("Mostrar detalhes >>",12,226,155);details.Click+=delegate{expanded=!expanded;parts.Visible=connectionLabel.Visible=expanded;ClientSize=new Size(550,expanded?445:267);details.Text=expanded?"<< Ocultar detalhes":"Mostrar detalhes >>";UpdateInfo();};Controls.Add(details);
  pause=Button("Pausar",315,226,105);pause.Click+=delegate{toggle();UpdateInfo();};Controls.Add(pause);var close=Button("Fechar",432,226,106);close.Click+=delegate{Close();};Controls.Add(close);
  connectionLabel=new Label{Text="Posição e progresso do download por conexão",Left=12,Top=273,Width=526,Height=20,Visible=false};Controls.Add(connectionLabel);parts=new DataGridView{Left=12,Top=298,Width=526,Height=133,Visible=false,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,BackgroundColor=Color.FromArgb(45,45,45),AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,EnableHeadersVisualStyles=false};parts.DefaultCellStyle.BackColor=Color.FromArgb(55,55,55);parts.DefaultCellStyle.ForeColor=Color.WhiteSmoke;parts.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(30,30,30);parts.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;parts.Columns.Add("n","Nº");parts.Columns.Add("bytes","Baixado");parts.Columns.Add("state","Estado");parts.Columns[0].FillWeight=30;Controls.Add(parts);
  timer=new System.Windows.Forms.Timer{Interval=500};timer.Tick+=delegate{UpdateInfo();};timer.Start();FormClosed+=delegate{timer.Dispose();};UpdateInfo();
 }
 void UpdateInfo(){string diagnostic=string.IsNullOrWhiteSpace(job.Error)?"Nenhum erro registrado neste download.":job.Error;if(errorDetails.Text!=diagnostic)errorDetails.Text=diagnostic;double seconds=Math.Max(.1,clock.Elapsed.TotalSeconds),rate=job.Running?Math.Max(0,job.Done-previous)/seconds:0;previous=job.Done;clock.Restart();string eta=rate>0&&job.Size>job.Done?TimeSpan.FromSeconds(Math.Min(315360000,(job.Size-job.Done)/rate)).ToString(@"hh\:mm\:ss"):"—";string state=job.Status.StartsWith("[download]")?"Recebendo mídia":job.Status;
  string size=job.Size>0?Bytes(job.Size):"Ainda não informado",downloaded=Bytes(job.Done),speed=Bytes(rate)+"/s";double percentage=job.Size>0?Math.Max(0,Math.Min(100,100.0*job.Done/job.Size)):0;
  if(job.Video){var match=System.Text.RegularExpressions.Regex.Match(job.Status,@"(\d+(?:\.\d+)?)%");double n;if(match.Success&&double.TryParse(match.Groups[1].Value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out n))mediaPercent=Math.Min(100,Math.Max(0,n));percentage=mediaPercent;downloaded=mediaPercent.ToString("0.0")+" % da etapa de mídia";var total=System.Text.RegularExpressions.Regex.Match(job.Status,@"\bof\s+~?\s*(\d+(?:\.\d+)?)\s*(KiB|MiB|GiB|TiB|B)");if(total.Success)size=total.Groups[1].Value+" "+total.Groups[2].Value;var transfer=System.Text.RegularExpressions.Regex.Match(job.Status,@"\bat\s+(\S+/s)");speed=job.Running&&transfer.Success?transfer.Groups[1].Value:"—";var remaining=System.Text.RegularExpressions.Regex.Match(job.Status,@"\bETA\s+(\S+)");eta=job.Running&&remaining.Success?remaining.Groups[1].Value:"—";}
  if(job.Status=="Concluído"){percentage=100;if(job.Video)downloaded="Concluído";}info.Text="Estado:                       "+state+"\nTamanho:                   "+size+"\nBaixado:                     "+downloaded+"\nVelocidade:                 "+speed+"\nTempo restante:          "+eta+"\nCapacidade de continuar:   "+(job.Video?"Gerenciada pelo motor de mídia":job.Ranges?"Sim":"Não confirmada");progress.Value=(int)percentage;Text=percentage.ToString("0")+"% "+job.Name+" — Nocturnal";pause.Text=job.Running?"Pausar":"Continuar";pause.Enabled=job.Status!="Concluído"&&job.Status!="Pausando";
  if(!expanded)return;parts.Rows.Clear();if(job.Video){parts.Rows.Add("—","—","Motor de vídeo/áudio (yt-dlp)");return;}int count=Math.Max(1,Math.Min(8,job.Parts));for(int i=0;i<count;i++){long bytes=job.ConnectionDone!=null&&i<job.ConnectionDone.Length?job.ConnectionDone[i]:0;if(!job.Running&&job.Status!="Concluído"){try{string f=job.Path+".fastget."+i;if(File.Exists(f))bytes=new FileInfo(f).Length;}catch{}}string status=job.Status=="Concluído"?"Concluída":!job.Running?state:job.ConnectionState[i]??"Aguardando";parts.Rows.Add(i+1,Bytes(bytes),status);}
 }
}
