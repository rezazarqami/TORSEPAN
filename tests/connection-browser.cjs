// Actual Blazor circuit: detach the WebSocket while backgrounded, then exercise UI after return.
const assert=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path');
const {chromium}=require(process.env.THEME_PLAYWRIGHT||'playwright');
(async()=>{
 const b=await chromium.launch({headless:true,executablePath:process.env.THEME_CHROMIUM,args:JSON.parse(process.env.THEME_CHROMIUM_ARGS||'[]')});
 try{
  const p=await b.newPage({viewport:{width:390,height:844}});let socket;
  await p.routeWebSocket(/\/_blazor(?:\?|$)/,ws=>{ws.connectToServer();socket=ws;});
  // Allows the recovery script to be exercised against an existing local preview binary too.
  const script=fs.readFileSync(path.join(__dirname,'../TORSEPAN.Panel/wwwroot/connection.js'),'utf8');
  await p.route(/\/connection(?:\.[^/]+)?\.js(?:\?|$)/,route=>route.fulfill({contentType:'text/javascript',body:script}));
  const {token}=await(await fetch('http://127.0.0.1:5169/api/auth/login',{method:'POST'})).json();
  await p.addInitScript(t=>localStorage.setItem('access_token',t),token);
  await p.goto('http://127.0.0.1:5170');await p.locator('.dashboard-tabs').waitFor();await p.locator('#torsepan-startup').waitFor({state:'detached'});
  const input=p.locator('.production-entry-card input');await input.fill('855');
  await p.evaluate(()=>window.__resumeDocument='original');
  for(const duration of [500,2500]){
   await p.evaluate(()=>{Object.defineProperty(document,'hidden',{configurable:true,get:()=>true});document.dispatchEvent(new Event('visibilitychange'));});
   assert(socket,'actual SignalR WebSocket must be established');await socket.close({code:1001,reason:'test background suspension'});
   await p.waitForFunction(()=>window.torsepanConnection.connected===false);await p.waitForTimeout(duration);
   await p.evaluate(()=>{Object.defineProperty(document,'hidden',{configurable:true,get:()=>false});document.dispatchEvent(new Event('visibilitychange'));window.dispatchEvent(new Event('focus'));});
   await p.waitForFunction(()=>window.torsepanConnection.connected===true,null,{timeout:12000});
   assert.equal(await p.evaluate(()=>window.__resumeDocument),'original','short background return must not reload the document');
   assert.equal(await input.inputValue(),'855','unsaved input must survive circuit reconnection');
   await p.locator('.mobile-nav-rail').click();await p.locator('.sidebar.mobile-open').waitFor({timeout:5000});
   await p.locator('.mobile-nav-rail').click();await p.locator('.sidebar.mobile-open').waitFor({state:'hidden'});
   console.log('PASS '+duration+'ms background: actual circuit reconnected, menu responds, input preserved, no page reload');
  }
 }finally{await b.close()}
})().catch(e=>{console.error(e);process.exitCode=1;});
