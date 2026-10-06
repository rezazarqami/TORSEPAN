const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PROFILE_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),out=process.env.PROFILE_PREVIEW_DIR,bootstrap=process.env.PROFILE_BOOTSTRAP;
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
let browser;
const watchdog=setTimeout(()=>{console.error("Profile browser layout verification timed out");process.exit(1);},90000);
(async()=>{
 await new Promise(resolve=>server.listen(5193,'127.0.0.1',resolve));browser=await chromium.launch({headless:true,...(process.env.PROFILE_CHROMIUM?{executablePath:process.env.PROFILE_CHROMIUM,args:JSON.parse(process.env.PROFILE_CHROMIUM_ARGS||'[]')}:{})});const page=await browser.newPage();page.setDefaultTimeout(15000);page.setDefaultNavigationTimeout(15000);
 for(const width of [320,360,384,412,1024])for(const noReset of [false,true])for(const name of ['performance','payroll','security','security-error']){
  await page.setViewportSize({width,height:820});await page.goto(`http://127.0.0.1:5193/preview/${name}.html${noReset?'?no-reset=1':''}`);await page.evaluate(()=>document.fonts.ready);
  const bounds=await page.evaluate(()=>({width:document.documentElement.scrollWidth,viewport:innerWidth}));assert(bounds.width<=bounds.viewport,`${name} ${width}: ${JSON.stringify(bounds)}`);
  const hero=await page.locator('.account-hero').boundingBox(),tabs=await page.locator('.account-tabs').boundingBox();
  assert(hero.x>=0&&hero.x+hero.width<=width&&Math.abs(hero.width-tabs.width)<=1&&Math.abs(hero.x-tabs.x)<=1,'account hero must fit and align with tabs');
  assert(await page.locator('.account-hero img').evaluate(e=>e.complete&&e.naturalWidth>0),'brand logo must load');
  assert(await page.locator('.account-tabs button').count()===3,'account must have performance, payroll and security tabs');
  if(name.startsWith('security')){
   const picker=page.locator('.avatar-picker input[type=file]');
   assert(await picker.count()===1,'native photo input must exist');
   const pickerId=await picker.getAttribute('id');
   assert(await page.locator('.photo-upload').getAttribute('for')===pickerId,'photo label must be explicitly linked to the input');
   const chooser=page.waitForEvent('filechooser');await page.locator('.photo-upload').click();await chooser;
   console.log('PASS native photo picker opens',width,noReset);
   assert(await page.locator('.profile-photo img').evaluate(e=>e.complete&&e.naturalWidth>0),'profile image must load');
   assert(await page.locator('input[autocomplete="current-password"]').getAttribute('type')==='password','current password must be masked');
   assert(await page.locator('input[autocomplete="new-password"]').count()===2,'password confirmation field must be present');
   const card=await page.locator('.credentials-card').boundingBox();assert(card.x>=0&&card.x+card.width<=width,'security form must fit without horizontal scrolling');
  }else {
   assert(await page.locator('table').count()===0,'performance and payroll must use responsive cards');
   const popup=await page.evaluate(()=>{const el=document.createElement('div');el.className='position-absolute';el.style.cssText='width:300px;right:0';document.querySelector('.activity-filters .position-relative').appendChild(el);const r=el.getBoundingClientRect();el.remove();return{x:r.x,width:r.width};});
   assert(popup.x>=0&&popup.x+popup.width<=width,'date popup must stay inside the mobile viewport');
  }
  await page.screenshot({path:path.join(out,`${name}-${width}${noReset?'-no-reset':''}.png`),fullPage:true});console.log('PASS account layout',name,width,noReset);
 }
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{clearTimeout(watchdog);if(browser)await browser.close();server.close();});
