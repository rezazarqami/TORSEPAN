const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.MESSAGES_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),out=process.env.MESSAGES_PREVIEW_DIR,bootstrap=process.env.MESSAGES_BOOTSTRAP;
const server=http.createServer((req,res)=>{
 const url=new URL(req.url,'http://localhost');let file;
 if(url.pathname==='/bootstrap.css')file=bootstrap;
 else if(url.pathname.startsWith('/preview/')){
  const html=fs.readFileSync(path.join(out,path.basename(url.pathname)),'utf8');
  res.setHeader('Content-Type','text/html; charset=utf-8');res.end(`<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">${url.searchParams.has('no-reset')?'':'<link rel="stylesheet" href="/bootstrap.css">'}<link rel="stylesheet" href="/TORSEPAN.Panel/wwwroot/app.css"><link rel="stylesheet" href="/TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css"></head><body><div class="layout"><div class="main"><header class="topbar" style="color:white;display:flex;align-items:center;justify-content:center">سامانه مدیریت تولید · TORSEPAN</header><main class="content">${html}</main></div></div><script src="/TORSEPAN.Panel/wwwroot/workshop-messages.js"></script></body></html>`);return;
 }else if(url.pathname.startsWith('/images/'))file=path.join(root,'TORSEPAN.Panel/wwwroot',url.pathname);
 else file=path.join(root,url.pathname);
 try{res.setHeader('Content-Type',({'.css':'text/css','.js':'application/javascript','.webp':'image/webp','.woff2':'font/woff2'})[path.extname(file)]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404);res.end();}
});
(async()=>{
 await new Promise(resolve=>server.listen(5192,'127.0.0.1',resolve));const browser=await chromium.launch({headless:true}),page=await browser.newPage();
 for(const width of [360,384,412,1024])for(const noReset of [false,true])for(const name of ['announcements','contacts','chat','broadcast']){
  await page.setViewportSize({width,height:820});await page.goto(`http://127.0.0.1:5192/preview/${name}.html${noReset?'?no-reset=1':''}`);await page.evaluate(()=>document.fonts.ready);
  const bounds=await page.evaluate(()=>({width:document.documentElement.scrollWidth,viewport:innerWidth}));assert(bounds.width<=bounds.viewport,`${name} ${width}: ${JSON.stringify(bounds)}`);
  const hero=await page.locator('.messages-hero').boundingBox(),workspace=await page.locator('.messages-workspace').boundingBox();
  assert(hero.x>=0&&hero.x+hero.width<=width&&Math.abs(hero.width-workspace.width)<=1&&Math.abs(hero.x-workspace.x)<=1,'hero must fit and align with page');
  assert(await page.locator('.messages-hero img').evaluate(e=>e.complete&&e.naturalWidth>0),'logo must load');
  if(width<650&&['chat','broadcast'].includes(name))assert(!(await page.locator('.conversation-list').isVisible()),'mobile open chat must hide contact list');
  await page.screenshot({path:path.join(out,`${name}-${width}${noReset?'-no-reset':''}.png`),fullPage:true});console.log('PASS message layout',name,width,noReset);
 }
 await page.setViewportSize({width:384,height:820});await page.goto('http://127.0.0.1:5192/preview/chat.html');
 const ids=await page.evaluate(()=>{
  const el=document.querySelector('.chat-thread');for(let i=0;i<30;i++){const p=document.createElement('p');p.style.cssText='height:50px;flex:none';el.appendChild(p);}
  const visible=document.createElement('p');visible.style.cssText='height:50px;flex:none';visible.dataset.incomingId='visible-last';el.appendChild(visible);el.scrollTop=el.scrollHeight;
  return workshopMessages.visibleIncoming(el);
 });
 assert(ids.includes('visible-last')&&!ids.includes('11111111-1111-1111-1111-111111111111'),'read helper returns only viewport-visible incoming messages');
 const hidden=await page.evaluate(()=>{Object.defineProperty(document,'visibilityState',{configurable:true,get:()=> 'hidden'});return workshopMessages.visibleIncoming(document.querySelector('.chat-thread'));});
 assert.equal(hidden.length,0,'hidden browser tab must never mark incoming messages read');console.log('PASS actual browser visibility and read helper');
 await browser.close();server.close();
})().catch(e=>{console.error(e);server.close();process.exitCode=1});
