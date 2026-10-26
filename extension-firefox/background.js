'use strict';
async function send(value){const url=Nocturnal.http(value);if(!url)return {ok:false};try{const token=crypto.randomUUID();await browser.storage.session.set({[token]:{url,created:Date.now()}});await browser.tabs.create({url:browser.runtime.getURL('handoff.html')+'#'+token});return {ok:true};}catch(error){console.error('Nocturnal: envio indisponível',error);return {ok:false};}}
let menuSetup=Promise.resolve();
function setupMenu(){menuSetup=menuSetup.catch(()=>{}).then(async()=>{await browser.contextMenus.removeAll();browser.contextMenus.create({id:'nocturnal-send',title:'Baixar com Nocturnal',contexts:['link','video','audio','page']});});return menuSetup;}
setupMenu();browser.runtime.onInstalled.addListener(setupMenu);browser.runtime.onStartup.addListener(setupMenu);
browser.contextMenus.onClicked.addListener((info,tab)=>{if(info.menuItemId!=='nocturnal-send')return;const page=Nocturnal.http((tab&&tab.url)||info.pageUrl);const source=Nocturnal.http(info.linkUrl)||Nocturnal.http(info.srcUrl);send(source||page);});
browser.runtime.onMessage.addListener((message,sender)=>{if(sender.id!==browser.runtime.id)return;if(!sender.tab&&!(sender.url||'').startsWith(browser.runtime.getURL('popup.html')))return;if(message&&message.type==='nocturnal.download')return send(message.url);});
browser.tabs.onRemoved.addListener(async()=>{const values=await browser.storage.session.get(null);const expired=Object.keys(values).filter(key=>values[key]&&Date.now()-values[key].created>3600000);if(expired.length)await browser.storage.session.remove(expired);});
