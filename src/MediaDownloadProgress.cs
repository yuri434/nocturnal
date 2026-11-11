using System;
using System.Globalization;
using System.Text.RegularExpressions;
public class MediaDownloadProgress {
 readonly Job job;readonly object gate=new object();string phase="Recebendo trecho";double processed;string speed="";
 public MediaDownloadProgress(Job value){job=value;}
 public void Line(string line){if(string.IsNullOrWhiteSpace(line))return;lock(gate){
  if(!Regex.IsMatch(line,@"^(frame|fps|stream_\d+_\d+_q|bitrate|total_size|out_time_us|out_time_ms|out_time|dup_frames|drop_frames|speed|progress)="))job.Error=line;
  if(line.StartsWith("[ExtractAudio]")||line.StartsWith("[VideoConvertor]")||line.StartsWith("[Merger]")||line.StartsWith("[Fixup")){phase=job.AudioOnly?"Convertendo áudio":"Finalizando vídeo";processed=0;speed="";job.Status=phase;return;}
  double micros;if(line.StartsWith("out_time_us=")&&double.TryParse(line.Substring(12),NumberStyles.Float,CultureInfo.InvariantCulture,out micros)){processed=Math.Max(0,micros/1000000);Render();return;}
  if(line.StartsWith("speed=")){speed=line.Substring(6).Trim();Render();return;}
  if(line.StartsWith("progress=")){Render();return;}
  var time=Regex.Match(line,@"\btime=(\d+):(\d+):(\d+(?:\.\d+)?)");if(time.Success){processed=int.Parse(time.Groups[1].Value)*3600+int.Parse(time.Groups[2].Value)*60+double.Parse(time.Groups[3].Value,CultureInfo.InvariantCulture);Render();return;}
  if(line.StartsWith("[download]")){if(line.Contains("Destination:")){phase=job.Clip?"Recebendo trecho":"Baixando mídia";processed=0;job.Status=phase;}else job.Status=line;return;}
  if(line.StartsWith("[youtube]")||line.StartsWith("[info]")){if(processed==0)job.Status="Preparando mídia";}
 }}
 void Render(){string time=TimeSpan.FromSeconds(Math.Min(processed,359999)).ToString(@"hh\:mm\:ss");job.Status=phase+" — "+time+" processados";if(job.Clip&&job.ClipEnd>job.ClipStart)job.Status+=" ("+Math.Min(99,processed/(job.ClipEnd-job.ClipStart)*100).ToString("0.0",CultureInfo.InvariantCulture)+"%)";if(speed.Length>0&&speed!="N/A")job.Status+=" — "+speed;}
 public static string Options(Job job){return " --retries 2 --fragment-retries 2 --extractor-retries 2 --abort-on-unavailable-fragments"+(job.Clip?" --downloader-args "+Engine.Quote("ffmpeg_i:-rw_timeout 20000000")+" --downloader-args "+Engine.Quote("ffmpeg_o:-progress pipe:2 -nostats")+" --postprocessor-args "+Engine.Quote("ffmpeg:-progress pipe:2 -nostats"):"");}
}

