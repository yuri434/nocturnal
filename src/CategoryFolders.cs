using System;
using System.IO;
public static class CategoryFolders {
 static string Config {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Nocturnal","download-folder.txt");}}
 public static string LoadRoot(){try{string path=File.ReadAllText(Config).Trim();if(Path.IsPathRooted(path))return path;}catch{}return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads","Nocturnal");}
 public static void SaveRoot(string path){Directory.CreateDirectory(Path.GetDirectoryName(Config));File.WriteAllText(Config,path);}
 public static string Category(string filename,bool video,bool audio){if(audio)return "Música";if(video)return "Vídeos";string ext=Path.GetExtension(filename).ToLowerInvariant();
  if(Has(".mp3 .m4a .aac .flac .wav .ogg .opus .wma .aiff",ext))return "Música";
  if(Has(".mp4 .mkv .webm .mov .avi .m4v .wmv .mpeg .mpg .ts",ext))return "Vídeos";
  if(Has(".zip .rar .7z .tar .gz .bz2 .xz .zst",ext))return "Compactados";
  if(Has(".exe .msi .msix .msixbundle .appx .apk .dmg .deb .rpm",ext))return "Programas";
  if(Has(".pdf .txt .doc .docx .xls .xlsx .ppt .pptx .odt .ods .csv .epub .rtf .html .htm .md",ext))return "Documentos";
  return "Outros";
 }
 static bool Has(string list,string ext){return ext.Length>0&&Array.IndexOf(list.Split(' '),ext)>=0;}
 public static string Destination(string root,string filename,bool video,bool audio){return Path.Combine(root,Category(filename,video,audio));}
}
