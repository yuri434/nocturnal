using System;
using System.Net;
using System.Net.Http;
using System.IO;
public class RangeRejectedException:Exception{public RangeRejectedException():base("O servidor rejeitou o intervalo solicitado.") {}}
public static class DownloadErrors {
 public static string Describe(Exception error,bool media){string detail=error.Message;for(Exception inner=error.InnerException;inner!=null;inner=inner.InnerException)if(!detail.Contains(inner.Message))detail+="\n"+inner.Message;
 if(error is HttpRequestException)return "Não foi possível concluir a conexão HTTP.\n"+detail+"\nVerifique o link e tente novamente. Links temporários ou que exigem a sessão do navegador podem não funcionar fora dele.";
 if(error is UnauthorizedAccessException)return "O Windows não permitiu gravar na pasta escolhida. Escolha outra pasta.\n"+detail;
 if(error is IOException)return "Falha ao ler a conexão ou gravar o arquivo. Verifique a conexão, espaço em disco e a pasta de destino.\n"+detail;
 return detail;
 }
 public static void EnsureSuccess(HttpResponseMessage response){if(response.IsSuccessStatusCode)return;int code=(int)response.StatusCode;string reason=code==401||code==403?"O site recusou o acesso. O link pode exigir login, sessão do navegador ou ter expirado.":code==404?"O arquivo não foi encontrado. Copie novamente o link direto.":code==429?"O servidor limitou as requisições. Aguarde e tente novamente.":code>=500?"O servidor está indisponível. Tente novamente mais tarde.":"O servidor não aceitou a solicitação.";throw new Exception("HTTP "+code+": "+reason);}
}
