using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

public class ConversionRequest {
 public string Input,Output,Format="mp4";public int AudioKbps=192,Height,VideoQuality=1;
}
public class ConversionProgress {public int Percent;public string Message;}
public static class MediaConverter {
 public static readonly string[] Formats={"mp4","mkv","webm","mp3","m4a","wav","flac","ogg"};
 public static bool IsVideo(string format){return format=="mp4"||format=="mkv"||format=="webm";}
 public static string Arguments(ConversionRequest r,string temporary){
  if(!Formats.Contains(r.Format))throw new ArgumentException("Formato de saída inválido.");
  if(!new[]{128,192,256,320}.Contains(r.AudioKbps)||r.VideoQuality<0||r.VideoQuality>2||!new[]{0,480,720,1080,1440,2160}.Contains(r.Height))throw new ArgumentException("Qualidade inválida.");
  string options;
  if(IsVideo(r.Format)){
   int crf=r.Format=="webm"?new[]{24,32,40}[r.VideoQuality]:new[]{18,23,28}[r.VideoQuality];
   string scale=r.Height==0?"scale=-2:max(2\\,trunc(ih/2)*2)":"scale=-2:max(2\\,trunc(min(ih\\,"+r.Height+")/2)*2)";
   options="-map 0:v:0 -map 0:a:0? -vf "+Engine.Quote(scale)+" -pix_fmt yuv420p ";
   options+=r.Format=="webm"?"-c:v libvpx-vp9 -b:v 0 -crf "+crf+" -c:a libopus -b:a 160k ":"-c:v libx264 -preset fast -crf "+crf+" -c:a aac -b:a 192k ";
   if(r.Format=="mp4")options+="-movflags +faststart ";
  }else{
   options="-map 0:a:0 -vn ";
   switch(r.Format){case "mp3":options+="-c:a libmp3lame -b:a "+r.AudioKbps+"k ";break;case "m4a":options+="-c:a aac -b:a "+r.AudioKbps+"k ";break;case "ogg":options+="-c:a libvorbis -b:a "+r.AudioKbps+"k ";break;case "wav":options+="-c:a pcm_s16le ";break;default:options+="-c:a flac ";break;}
  }
  return "-hide_banner -nostdin -n -threads 2 -protocol_whitelist file,pipe -i "+Engine.Quote(Path.GetFullPath(r.Input))+" "+options+"-threads 2 -progress pipe:1 -nostats "+Engine.Quote(temporary);
 }
 public static Task Convert(ConversionRequest request,string tools,IProgress<ConversionProgress> progress,CancellationToken ct){return Task.Run(()=>Run(request,tools,progress,ct),ct);}
 static double Duration(string input,string tools,CancellationToken ct){string probe=Path.Combine(tools,"ffprobe.exe");if(!File.Exists(probe))return 0;try{string value="";using(var p=new Process()){p.StartInfo=new ProcessStartInfo(probe,"-v error -protocol_whitelist file,pipe -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -i "+Engine.Quote(input)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};p.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){if(e.Data!=null)value=e.Data;};p.ErrorDataReceived+=delegate{};p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();using(ct.Register(delegate{try{p.Kill();}catch{}})){if(!p.WaitForExit(10000)){p.Kill();return 0;}p.WaitForExit();}double n;return double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&n>0?n:0;}}catch{return 0;}}
 static void Run(ConversionRequest r,string tools,IProgress<ConversionProgress> progress,CancellationToken ct){
  ct.ThrowIfCancellationRequested();string input=Path.GetFullPath(r.Input),output=Path.GetFullPath(r.Output);if(!File.Exists(input))throw new FileNotFoundException("O arquivo original não foi encontrado.");if(string.Equals(input,output,StringComparison.OrdinalIgnoreCase))throw new Exception("Escolha um destino diferente do arquivo original.");if(File.Exists(output))throw new Exception("Já existe um arquivo nesse destino. Escolha outro nome.");if(!string.Equals(Path.GetExtension(output),"."+r.Format,StringComparison.OrdinalIgnoreCase))throw new Exception("A extensão do destino não corresponde ao formato escolhido.");
  string executable=Path.Combine(tools,"ffmpeg.exe");if(!File.Exists(executable))throw new FileNotFoundException("FFmpeg não encontrado. Coloque ffmpeg.exe na pasta tools do Nocturnal.");Directory.CreateDirectory(Path.GetDirectoryName(output));string temporary=Path.Combine(Path.GetDirectoryName(output),".nocturnal-"+Guid.NewGuid().ToString("N")+"."+r.Format);double duration=Duration(input,tools,ct);ct.ThrowIfCancellationRequested();var errors=new Queue<string>();
  try{using(var p=new Process()){p.StartInfo=new ProcessStartInfo(executable,Arguments(r,temporary)){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardErrorEncoding=Encoding.UTF8,StandardOutputEncoding=Encoding.UTF8};
   p.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null){lock(errors){errors.Enqueue(e.Data);while(errors.Count>24)errors.Dequeue();}}};
   p.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data==null||!e.Data.StartsWith("out_time_us="))return;double micros;if(double.TryParse(e.Data.Substring(12),NumberStyles.Float,CultureInfo.InvariantCulture,out micros)){double seconds=Math.Max(0,micros/1000000.0);if(progress!=null)progress.Report(new ConversionProgress{Percent=duration>0?(int)Math.Min(99,100*seconds/duration):0,Message="Convertendo — "+TimeSpan.FromSeconds(Math.Min(315360000,seconds)).ToString(@"hh\:mm\:ss")+" processados"});}};
   p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();using(ct.Register(delegate{try{p.Kill();}catch{}})){p.WaitForExit();}ct.ThrowIfCancellationRequested();if(p.ExitCode!=0){string detail;lock(errors)detail=string.Join(Environment.NewLine,errors.ToArray());throw new Exception("Não foi possível converter. Verifique se o arquivo contém o tipo de mídia escolhido.\n\n"+detail);}
  }
  ct.ThrowIfCancellationRequested();if(!File.Exists(temporary)||new FileInfo(temporary).Length==0)throw new Exception("O conversor não gerou um arquivo válido.");File.Move(temporary,output);if(progress!=null)progress.Report(new ConversionProgress{Percent=100,Message="Concluído — arquivo salvo."});
  }finally{if(File.Exists(temporary))File.Delete(temporary);}
 }
}

public class ConverterWindow:Form {
 string tools=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tools");TextBox input,output,detail;ComboBox format,quality,height;ProgressBar progress;Label status;Button convert,cancel,sourceButton,destinationButton;CancellationTokenSource cancellation;bool busy,closing,closeAllowed;Task shutdown;Task current=Task.FromResult(0);
 public ConverterWindow(string initial){Text="Nocturnal — Converter vídeo e áudio";ClientSize=new Size(640,390);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=true;StartPosition=FormStartPosition.CenterParent;TransferWindow.Style(this);
  Controls.Add(new Label{Text="Arquivo original:",Left=14,Top=17,Width=110});input=Box(125,14,405);Controls.Add(input);sourceButton=TransferWindow.Button("Escolher…",540,12,86);sourceButton.Click+=delegate{using(var d=new OpenFileDialog{Title="Escolher vídeo ou áudio",Filter="Vídeo e áudio|*.mp4;*.mkv;*.webm;*.avi;*.mov;*.m4v;*.mp3;*.m4a;*.aac;*.wav;*.flac;*.ogg;*.opus;*.wma;*.wmv;*.mpeg;*.mpg;*.ts|Todos os arquivos|*.*"})if(d.ShowDialog(this)==DialogResult.OK){input.Text=d.FileName;SuggestOutput();}};Controls.Add(sourceButton);
  Controls.Add(new Label{Text="Converter para:",Left=14,Top=62,Width=110});format=Combo(125,58,110);format.Items.AddRange(MediaConverter.Formats.Select(x=>(object)x.ToUpperInvariant()).ToArray());Controls.Add(format);Controls.Add(new Label{Text="Qualidade:",Left=250,Top=62,Width=80});quality=Combo(330,58,200);Controls.Add(quality);
  Controls.Add(new Label{Text="Resolução:",Left=14,Top=105,Width=110});height=Combo(125,101,200);height.Items.AddRange(new object[]{"Manter original","2160p / 4K","1440p","1080p","720p","480p"});height.SelectedIndex=0;Controls.Add(height);Controls.Add(new Label{Text="Não aumenta a resolução da origem.",Left=338,Top=105,Width=288});
  Controls.Add(new Label{Text="Salvar como:",Left=14,Top=148,Width=110});output=Box(125,144,405);Controls.Add(output);destinationButton=TransferWindow.Button("Escolher…",540,142,86);destinationButton.Click+=delegate{using(var d=new SaveFileDialog{FileName=Path.GetFileName(output.Text),InitialDirectory=Directory.Exists(Path.GetDirectoryName(output.Text))?Path.GetDirectoryName(output.Text):Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Filter=CurrentFormat().ToUpperInvariant()+"|*."+CurrentFormat(),DefaultExt=CurrentFormat()})if(d.ShowDialog(this)==DialogResult.OK)output.Text=d.FileName;};Controls.Add(destinationButton);
  Controls.Add(new Label{Text="O arquivo original é preservado. A conversão cria um arquivo novo.",Left=14,Top=189,Width=610});progress=new ProgressBar{Left=14,Top=219,Width=612,Height=15};Controls.Add(progress);status=new Label{Left=14,Top=245,Width=610,Height=23,Text="Escolha o arquivo e o formato de saída."};Controls.Add(status);detail=new TextBox{Left=14,Top=273,Width=612,Height=59,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.FromArgb(35,35,35),ForeColor=Color.WhiteSmoke};Controls.Add(detail);
  convert=TransferWindow.Button("Converter",404,346,106);convert.Click+=async delegate{current=StartConversion();await current;};Controls.Add(convert);cancel=TransferWindow.Button("Fechar",520,346,106);cancel.Click+=delegate{if(busy){cancellation.Cancel();cancel.Enabled=false;status.Text="Cancelando…";}else Close();};Controls.Add(cancel);
  format.SelectedIndexChanged+=delegate{quality.Items.Clear();bool video=MediaConverter.IsVideo(CurrentFormat());height.Enabled=video;if(video){quality.Items.AddRange(new object[]{"Alta","Equilibrada","Menor arquivo"});quality.SelectedIndex=1;quality.Enabled=true;}else if(CurrentFormat()=="wav"||CurrentFormat()=="flac"){quality.Items.Add("Sem perdas adicionais");quality.SelectedIndex=0;quality.Enabled=false;}else{quality.Items.AddRange(new object[]{"320 kbps","256 kbps","192 kbps","128 kbps"});quality.SelectedIndex=2;quality.Enabled=true;}SuggestOutput();};format.SelectedIndex=0;if(!string.IsNullOrEmpty(initial)){input.Text=initial;SuggestOutput();}FormClosing+=delegate(object sender,FormClosingEventArgs e){if(closeAllowed)return;e.Cancel=true;CloseSafely();};
 }
 public Task CloseSafely(){if(shutdown==null)shutdown=Shutdown();return shutdown;}
 async Task Shutdown(){closing=true;if(busy){status.Text="Cancelando…";cancellation.Cancel();await current;}await Task.Yield();closeAllowed=true;if(!IsDisposed&&!Disposing)Close();await Task.Yield();}
 static TextBox Box(int x,int y,int w){return new TextBox{Left=x,Top=y,Width=w,BackColor=Color.FromArgb(35,35,35),ForeColor=Color.WhiteSmoke};}
 static ComboBox Combo(int x,int y,int w){return new ComboBox{Left=x,Top=y,Width=w,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(40,40,40),ForeColor=Color.WhiteSmoke};}
 string CurrentFormat(){return format.SelectedItem==null?"mp4":format.SelectedItem.ToString().ToLowerInvariant();}
 void SuggestOutput(){if(string.IsNullOrWhiteSpace(input.Text))return;try{output.Text=Path.Combine(CategoryFolders.LoadRoot(),"Convertidos",Path.GetFileNameWithoutExtension(input.Text)+"_convertido."+CurrentFormat());}catch{}}
 async Task StartConversion(){if(busy)return;try{if(string.IsNullOrWhiteSpace(input.Text)||string.IsNullOrWhiteSpace(output.Text))throw new Exception("Escolha o arquivo original e o destino.");string f=CurrentFormat();var request=new ConversionRequest{Input=input.Text,Output=output.Text,Format=f,AudioKbps=quality.Enabled&&!MediaConverter.IsVideo(f)?new[]{320,256,192,128}[quality.SelectedIndex]:192,VideoQuality=MediaConverter.IsVideo(f)?quality.SelectedIndex:1,Height=MediaConverter.IsVideo(f)?new[]{0,2160,1440,1080,720,480}[height.SelectedIndex]:0};busy=true;input.Enabled=output.Enabled=sourceButton.Enabled=destinationButton.Enabled=format.Enabled=quality.Enabled=height.Enabled=convert.Enabled=false;cancel.Text="Cancelar";cancel.Enabled=true;detail.Clear();status.Text="Preparando conversão…";progress.Value=0;cancellation=new CancellationTokenSource();var updates=new Progress<ConversionProgress>(value=>{if(!IsDisposed&&busy){progress.Value=value.Percent;status.Text=value.Message;}});await MediaConverter.Convert(request,tools,updates,cancellation.Token);progress.Value=100;status.Text="Concluído — "+Path.GetFileName(request.Output);detail.Text=request.Output;
  }catch(OperationCanceledException){status.Text="Conversão cancelada. O original foi preservado.";}catch(Exception ex){status.Text="Não foi possível converter.";detail.Text=ex.Message;}finally{if(busy){busy=false;cancellation.Dispose();if(!closing){input.Enabled=output.Enabled=sourceButton.Enabled=destinationButton.Enabled=format.Enabled=convert.Enabled=true;quality.Enabled=CurrentFormat()!="wav"&&CurrentFormat()!="flac";height.Enabled=MediaConverter.IsVideo(CurrentFormat());cancel.Text="Fechar";cancel.Enabled=true;}}}}
}
