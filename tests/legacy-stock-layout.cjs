const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.LEGACY_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),out=process.env.LEGACY_PREVIEW_DIR;
if(!out)throw Error('LEGACY_PREVIEW_DIR is required');
const server=http.createServer((req,res)=>{
 const url=new URL(req.url,'http://localhost');let file;
 if(url.pathname.startsWith('/preview/')){
  const html=fs.readFileSync(path.join(out,path.basename(url.pathname)),'utf8');
  res.setHeader('Content-Type','text/html;charset=utf-8');res.end(`<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="/vendor/bootstrap/bootstrap.rtl.min.css"><link rel="stylesheet" href="/app.css"><link rel="stylesheet" href="/scoped.css"><link rel="stylesheet" href="/theme.css"><link rel="stylesheet" href="/theme-pages.css"><link rel="stylesheet" href="/navigation-shell.css"></head><body><div class="layout theme-shell"><div class="main"><main class="content">${html}</main></div></div></body></html>`);return;
 }
 if(url.pathname==='/scoped.css')file=path.join(root,'TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css');
 else file=path.join(root,'TORSEPAN.Panel/wwwroot',url.pathname);
 try{res.setHeader('Content-Type',({'.css':'text/css','.js':'application/javascript','.webp':'image/webp','.woff2':'font/woff2'})[path.extname(file)]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404);res.end();}
});
let browser;const watchdog=setTimeout(()=>{console.error('Legacy stock browser check timed out');process.exit(1)},120000);
(async()=>{
 await new Promise(r=>server.listen(5195,'127.0.0.1',r));browser=await chromium.launch({headless:true,...(process.env.LEGACY_CHROMIUM?{executablePath:process.env.LEGACY_CHROMIUM,args:JSON.parse(process.env.LEGACY_CHROMIUM_ARGS||'[]')}:{})});
 const page=await browser.newPage();page.setDefaultTimeout(10000);page.setDefaultNavigationTimeout(15000);
 for(const width of [320,360,384,412,768,1280])for(const state of ['empty','export','retry','saved']){
  await page.setViewportSize({width,height:850});await page.goto(`http://127.0.0.1:5195/preview/${state}.html`);await page.evaluate(()=>document.fonts.ready);
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`${state} at ${width} must fit without sideways scroll`);
  const hero=await page.locator('.legacy-hero').boundingBox();assert(hero.x>=0&&hero.x+hero.width<=width,'hero fits the viewport');
  assert((await page.locator('h1').innerText()).replace(/\s+/g,' ').trim()==='ثبت سازهای قدیمی','new module has its own clear heading');
  if(state!=='saved'){
   assert(await page.locator('#legacy-code').count()===1&&await page.locator('#legacy-bottom-code').count()===0,'only visible bowl code is requested');
   assert(await page.locator('select[id^=legacy-]').count()>=9,'technical, warehouse and five maker selectors are present');
   if(state==='retry')assert(await page.locator('#legacy-code').isDisabled(),'uncertain request freezes its exact original payload for safe retry');
   else assert(await page.locator('#legacy-code').isEnabled(),'form is editable before submission');
   if(state==='export')assert(await page.locator('#legacy-country option').count()===5,'all four existing export countries can be selected');
  }else assert(await page.locator('.legacy-saved a[href*="production-edit"]').count()===1,'success links to the existing passport');
  await page.screenshot({path:path.join(out,`${state}-${width}.png`),fullPage:true});if(state!=='saved'){const submit=page.locator('button[type=submit]');await submit.scrollIntoViewIfNeeded();const r=await submit.boundingBox();assert(r&&r.y>=0&&r.y+r.height<=850,'submit remains reachable through the real page scroll container');}console.log('PASS legacy stock layout',state,width);
 }
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{clearTimeout(watchdog);if(browser)await browser.close();server.close()});
