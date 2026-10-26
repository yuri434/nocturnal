/* Plain source: no bundler, remote code or telemetry. */
globalThis.Nocturnal = (() => {
 const defaults={videoButtons:true,captureLinks:true};
 const extensions=/\.(?:mp4|mkv|webm|mov|avi|m4v|mp3|m4a|aac|wav|flac|ogg|opus|zip|rar|7z|tar|gz|bz2|xz|exe|msi|iso|img|apk|pdf|epub|docx?|xlsx?|pptx?|csv|torrent|srt|vtt|m3u8|mpd)$/i;
 function http(value,base){try{const u=new URL(value,base);return /^https?:$/.test(u.protocol)&&!u.username&&!u.password?u.href:'';}catch{return '';}}
 function downloadable(href,attribute){const url=http(href);return !!url&&(attribute||extensions.test(new URL(url).pathname));}
 return {defaults,http,downloadable,protocol:url=>'nocturnal://add?url='+encodeURIComponent(url)};
})();
