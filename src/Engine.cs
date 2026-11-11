using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: AssemblyVersion("0.0.0.0")]
public class Job
{
 public int LimitKBps;
 public bool ForceSingle;
 public bool Clip; public double ClipStart,ClipEnd;
 [ScriptIgnore] public long[] ConnectionDone = new long[8];
 [ScriptIgnore] public string[] ConnectionState = new string[8];
 [ScriptIgnore] public BandwidthGate Gate = new BandwidthGate();
	public string Url;

	public string Path;

	public string Name;

	public string Status = "Na fila";

	public string Error = "";

	public string Validator;

	public int MaxHeight;

	public bool AudioOnly;

	public int AudioKbps = 320;

	public bool Video;

	public bool Ranges;

	public long Size;

	public long Done;

	public int Parts = 1;

	[ScriptIgnore]
	public CancellationTokenSource Cancel;

	[ScriptIgnore]
	public Process Process;

	[ScriptIgnore]
	public bool Running;
}
public static class ClipRange {
 public static double Parse(string text){
  var parts=(text??"").Trim().Replace(',', '.').Split(':');if(parts.Length<1||parts.Length>3)throw new Exception("Use segundos, MM:SS ou HH:MM:SS.");double total=0;for(int i=0;i<parts.Length;i++){double value;if(!System.Text.RegularExpressions.Regex.IsMatch(parts[i],i==parts.Length-1?@"^\d+(?:\.\d{1,3})?$":@"^\d+$")||!double.TryParse(parts[i],System.Globalization.NumberStyles.AllowDecimalPoint,System.Globalization.CultureInfo.InvariantCulture,out value)||value<0||(parts.Length>1&&i>0&&value>=60))throw new Exception("Tempo inválido. Use MM:SS ou HH:MM:SS.");total=total*60+value;}if(total>359999)throw new Exception("O tempo máximo é 99:59:59.");return total;
 }
 public static void Validate(Job j){if(!j.Clip)return;if(!j.Video||double.IsNaN(j.ClipStart)||double.IsInfinity(j.ClipStart)||double.IsNaN(j.ClipEnd)||double.IsInfinity(j.ClipEnd)||j.ClipStart<0||j.ClipEnd<=j.ClipStart||j.ClipEnd>359999)throw new Exception("O fim do trecho precisa ser maior que o início. Escolha Vídeo MP4 ou Áudio MP3.");}
 public static string Options(Job j){Validate(j);return !j.Clip?"":" --download-sections "+Engine.Quote("*"+j.ClipStart.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+"-"+j.ClipEnd.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture))+" --force-keyframes-at-cuts";}
}
public class Engine
{
	public static readonly HttpClient Http = Create();

	private static HttpClient Create()
	{
		HttpClientHandler httpClientHandler = new HttpClientHandler();
		httpClientHandler.AutomaticDecompression = DecompressionMethods.None;
		HttpClient httpClient = new HttpClient(httpClientHandler);
		httpClient.Timeout = TimeSpan.FromHours(24.0);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Nocturnal/0.3");
		return httpClient;
	}

	public static void Validate(string url)
	{
		Uri result;
		if (!Uri.TryCreate(url, UriKind.Absolute, out result) || (result.Scheme != "http" && result.Scheme != "https"))
		{
			throw new Exception("Informe um link HTTP ou HTTPS válido.");
		}
	}

	public static bool IsVideoPage(string url)
	{
		Uri result;
		if (!Uri.TryCreate(url, UriKind.Absolute, out result))
		{
			return false;
		}
		string h = result.Host.ToLowerInvariant();
		if (new string[8] { ".mp4", ".mkv", ".webm", ".mov", ".avi", ".m4v", ".m3u8", ".mpd" }.Contains(Path.GetExtension(result.AbsolutePath).ToLowerInvariant()))
		{
			return true;
		}
		return new string[9] { "x.com", "twitter.com", "youtube.com", "youtu.be", "vimeo.com", "tiktok.com", "instagram.com", "facebook.com", "twitch.tv" }.Any((string x) => h == x || h.EndsWith("." + x));
	}

	public static async Task Download(Job j, CancellationToken ct)
 {
  bool retry=false;try { await DownloadCore(j,ct); }
  catch(RangeRejectedException) {retry=true;}
  if(retry){ct.ThrowIfCancellationRequested();j.ForceSingle=true;j.Status="Tentando conexão única";await DownloadCore(j,ct);}
 }
 private static async Task<HttpResponseMessage> Probe(string url,CancellationToken ct)
 {
  using(var req=new HttpRequestMessage(HttpMethod.Get,url)){
   req.Headers.Range=new RangeHeaderValue(0L,0L);
   var response=await Http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead,ct);
   int code=(int)response.StatusCode;
   if(code!=400&&code!=405&&code!=416)return response;
   response.Dispose();
  }
  using(var req=new HttpRequestMessage(HttpMethod.Get,url))return await Http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead,ct);
 }
 private static async Task DownloadCore(Job j, CancellationToken ct)
 {
		Validate(j.Url);
		Directory.CreateDirectory(Path.GetDirectoryName(j.Path));
		using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, j.Url))
		{
			using (HttpResponseMessage res = await Probe(j.Url,ct))
			{
				DownloadErrors.EnsureSuccess(res);
				MediaTypeHeaderValue contentType = res.Content.Headers.ContentType;
				if (contentType != null && (contentType.MediaType == "text/html" || contentType.MediaType == "application/xhtml+xml"))
				{
					throw new Exception("Este endereço retornou uma página HTML, não um arquivo. Use o link direto do arquivo ou selecione Vídeo MP4/Áudio MP3 para uma página de mídia compatível.");
				}
				ContentRangeHeaderValue contentRange = res.Content.Headers.ContentRange;
				bool flag = res.StatusCode == HttpStatusCode.PartialContent && contentRange != null && contentRange.From == 0 && contentRange.To == 0 && contentRange.Length.HasValue;
				long num = (flag ? contentRange.Length.Value : (res.Content.Headers.ContentLength ?? 0));
				string text = ((res.Headers.ETag != null && !res.Headers.ETag.IsWeak) ? res.Headers.ETag.ToString() : (res.Content.Headers.LastModified.HasValue ? res.Content.Headers.LastModified.Value.ToString("R") : null));
				if (j.Size != num || j.Validator != text || text == null || j.ForceSingle)
				{
					for (int i = 0; i < 8; i++)
					{
						string path = j.Path + ".fastget." + i;
						if (File.Exists(path))
						{
							File.Delete(path);
						}
					}
				}
				j.Ranges = flag && text != null && !j.ForceSingle;
				j.Size = num;
				j.Validator = text;
				j.Parts = ((!j.Ranges || num <= 4194304) ? 1 : 8);
			}
		}
		j.Done = 0L;
 j.ConnectionDone = new long[8]; j.ConnectionState = new string[8]; j.Gate = new BandwidthGate();
		for (int i = 0; i < j.Parts; i++)
		{
			string path = j.Path + ".fastget." + i;
			if (File.Exists(path) && j.Ranges)
			{
				j.Done += new FileInfo(path).Length;
			}
		}
		List<Task> tasks = new List<Task>();
		for (int i = 0; i < j.Parts; i++)
		{
			int p = i;
			tasks.Add(Segment(j, p, ct));
		}
		try
		{
			await Task.WhenAll(tasks);
		}
		catch
		{
			throw;
		}
		ct.ThrowIfCancellationRequested();
		j.Status = "Finalizando";
		string staging = j.Path + ".fastget.complete";
		using (FileStream output = new FileStream(staging, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			for (int k = 0; k < j.Parts; k++)
			{
				using (FileStream input = File.OpenRead(j.Path + ".fastget." + k))
				{
					await input.CopyToAsync(output, 65536, ct);
				}
			}
			if (j.Size > 0 && output.Length != j.Size)
			{
				throw new Exception("O tamanho recebido difere do esperado.");
			}
		}
		if (File.Exists(j.Path))
		{
			throw new Exception("O arquivo de destino já existe. Escolha outro nome.");
		}
		File.Move(staging, j.Path);
		for (int i = 0; i < j.Parts; i++)
		{
			File.Delete(j.Path + ".fastget." + i);
		}
	}

	private static async Task Segment(Job j, int p, CancellationToken ct)
	{
		long span = j.Size / j.Parts;
		long start = p * span;
		long end = ((p == j.Parts - 1) ? (j.Size - 1) : (start + span - 1));
		string f = j.Path + ".fastget." + p;
 j.ConnectionState[p] = "Conectando";
		long have = ((j.Ranges && File.Exists(f)) ? new FileInfo(f).Length : 0);
		j.ConnectionDone[p] = have;
		if (j.Ranges && have > end - start + 1)
		{
			throw new Exception("Parte local inválida. Remova o download e adicione novamente.");
		}
		if (j.Ranges && have == end - start + 1)
		{
 j.ConnectionState[p] = "Concluída";
			return;
		}
		using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, j.Url))
		{
			if (j.Ranges)
			{
				req.Headers.Range = new RangeHeaderValue(start + have, end);
				req.Headers.TryAddWithoutValidation("If-Range", j.Validator);
			}
			using (HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
			{
				if(j.Ranges && res.StatusCode==HttpStatusCode.RequestedRangeNotSatisfiable)throw new RangeRejectedException();
 DownloadErrors.EnsureSuccess(res);
				if (j.Ranges)
				{
					ContentRangeHeaderValue contentRange = res.Content.Headers.ContentRange;
					if (res.StatusCode != HttpStatusCode.PartialContent || contentRange == null || contentRange.From != start + have || contentRange.To != end || contentRange.Length != j.Size)
					{
						throw new RangeRejectedException();
					}
				}
				using (Stream stream = await res.Content.ReadAsStreamAsync())
				{
					using (FileStream output = new FileStream(f, (have > 0) ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read))
					{
						j.ConnectionState[p] = "Recebendo";
		byte[] b = new byte[65536];
						long received = have;
						while (true)
						{
							int num2;
							int num = (num2 = await stream.ReadAsync(b, 0, b.Length, ct));
							// Removed redundant decompiler assignment.
							int n = num2;
							if (num <= 0)
							{
								break;
							}
							received += n;
							if (j.Ranges && received > end - start + 1)
							{
								throw new Exception("Resposta maior que o intervalo solicitado.");
							}
							await j.Gate.Wait(n, j.LimitKBps, ct);
			await output.WriteAsync(b, 0, n, ct);
 Interlocked.Add(ref j.ConnectionDone[p], n);
							Interlocked.Add(ref j.Done, n);
						}
						if (j.Ranges && received != end - start + 1)
						{
							throw new Exception("Conexão interrompida. Use Continuar para retomar.");
						}
					}
				}
			}
		}
	}

	public static string FormatSelector(int height)
	{
		if (!new int[7] { 0, 360, 480, 720, 1080, 1440, 2160 }.Contains(height))
		{
			throw new ArgumentException("Qualidade inválida.");
		}
		return (height == 0) ? "bv*+ba/b" : ("bv*[height<=?" + height + "]+ba/b[height<=?" + height + "]");
	}

	public static string Quote(string s)
	{
		return "\"" + s.Replace("\\", "\\").Replace("\"", "\\\"") + "\"";
	}

	public static async Task Video(Job j, CancellationToken ct)
	{
		ClipRange.Validate(j);
        j.Path = Path.ChangeExtension(j.Path, j.AudioOnly ? "mp3" : "mp4");
		try
		{
			await VideoAttempt(j, ct, "");
			return;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			ct.ThrowIfCancellationRequested();
			string text = new Uri(j.Url).Host.ToLowerInvariant();
			if (text != "x.com" && !text.EndsWith(".x.com") && text != "twitter.com" && !text.EndsWith(".twitter.com"))
			{
				throw;
			}
		}
		j.Status = "Tentando outra conexão com o X";
		await VideoAttempt(j, ct, " --extractor-args twitter:api=syndication");
	}

	private static async Task VideoAttempt(Job j, CancellationToken ct, string extra)
	{
		string tools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");
		ProcessStartInfo info = new ProcessStartInfo(Path.Combine(tools, "yt-dlp.exe"))
		{
			Arguments = "--ignore-config --no-playlist --newline --no-colors --continue --windows-filenames --no-overwrites --socket-timeout 20 --js-runtimes " + Quote("deno:" + Path.Combine(tools, "deno.exe")) + " --ffmpeg-location " + Quote(tools) + " -N 8 " + (j.LimitKBps > 0 ? " --limit-rate " + j.LimitKBps + "K " : "") + MediaOptions(j) + MediaDownloadProgress.Options(j) + ClipRange.Options(j) + " -o " + Quote(OutputTemplate(j)) + extra + " -- " + Quote(j.Url),
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};
		Process proc = new Process
		{
			StartInfo = info
		};
		try
		{
			j.Process = proc;
			string last = "";var mediaProgress=new MediaDownloadProgress(j);var diagnostic=new Queue<string>();object diagnosticGate=new object();
			proc.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
			{
				if (e.Data != null)
				{
					mediaProgress.Line(e.Data);
				}
			};
			proc.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
			{
				if (e.Data != null)
				{
					mediaProgress.Line(e.Data);if(!Regex.IsMatch(e.Data,@"^(frame|fps|stream_\d+_\d+_q|bitrate|total_size|out_time_us|out_time_ms|out_time|dup_frames|drop_frames|speed|progress)=")){lock(diagnosticGate){diagnostic.Enqueue(e.Data);while(diagnostic.Count>12)diagnostic.Dequeue();last=string.Join(Environment.NewLine,diagnostic.ToArray());}}
				}
			};
			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			using (ct.Register(delegate
			{
				try
				{
					ProcessStartInfo startInfo = new ProcessStartInfo("taskkill.exe", "/PID " + proc.Id + " /T /F")
					{
						UseShellExecute = false,
						CreateNoWindow = true
					};
					using (Process process = Process.Start(startInfo))
					{
						process.WaitForExit(5000);
					}
				}
				catch
				{
					try
					{
						proc.Kill();
					}
					catch
					{
					}
				}
			}))
			{
				await Task.Run(delegate
				{
					proc.WaitForExit();
				});
				ct.ThrowIfCancellationRequested();
				if (proc.ExitCode != 0)
				{
					throw new Exception((last.Length > 0) ? last : "Não foi possível baixar este vídeo.");
				}
				if (!File.Exists(j.Path))
				{
					throw new Exception("A conversão não gerou o arquivo " + (j.AudioOnly ? "MP3" : "MP4") + " esperado.");
				}
			}
			j.Process = null;
		}
		finally
		{
			if (proc != null)
			{
				if(object.ReferenceEquals(j.Process,proc))j.Process=null;((IDisposable)proc).Dispose();
			}
		}
	}

	public static string MediaOptions(Job j)
	{
		if (!j.AudioOnly)
		{
			return "-f " + Quote(FormatSelector(j.MaxHeight)) + " --merge-output-format mp4 --recode-video mp4";
		}
		int num = ((j.AudioKbps == 0) ? 320 : j.AudioKbps);
		if (num != 128 && num != 192 && num != 256 && num != 320)
		{
			throw new ArgumentException("Taxa de áudio inválida.");
		}
		return "-f \"ba/b\" --extract-audio --audio-format mp3 --audio-quality " + num + "K";
	}

	public static string OutputTemplate(Job j)
	{
		return Path.ChangeExtension(j.Path, null).Replace("%", "%%") + ".%(ext)s";
	}
}
public class Program
{
	[DllImport("user32.dll")]
	private static extern bool SetProcessDPIAware();

	[STAThread]
	public static void Main(string[] args)
	{
		try
		{
			SetProcessDPIAware();
		}
		catch
		{
		}
		AppBrand.Initialize();
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
		ServicePointManager.DefaultConnectionLimit = 32;
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		string text = "";
		if (args.Length > 0)
		{
			string[] array = new string[2] { "nocturnal://add?url=", "fastget://add?url=" };
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (args[0].StartsWith(text2))
				{
					text = Uri.UnescapeDataString(args[0].Substring(text2.Length));
					try
					{
						Engine.Validate(text);
					}
					catch
					{
						text = "";
					}
					break;
				}
			}
		}
		bool createdNew;
		using (new Mutex(true, "Local\\Nocturnal.v03", out createdNew))
		{
			if (!createdNew)
			{
				if (text.Length > 0)
				{
					string text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nocturnal", "incoming");
					Directory.CreateDirectory(text3);
					File.WriteAllText(Path.Combine(text3, string.Concat(Guid.NewGuid(), ".txt")), text);
				}
			}
			else
			{
				Application.Run(new MainForm(text));
			}
		}
	}
}




