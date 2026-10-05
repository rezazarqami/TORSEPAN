// Render the real scoped components at mobile widths, with local fixture data.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.INVENTORY_PLAYWRIGHT || 'playwright');
const root=path.resolve(__dirname,'..'),out=process.env.INVENTORY_PREVIEW_DIR;
const bootstrap=process.env.INVENTORY_BOOTSTRAP;
const server=http.createServer((req,res)=>{
 const url=new URL(req.url,'http://localhost');let file;
 if(url.pathname==='/bootstrap.css')file=bootstrap;
 else if(url.pathname.startsWith('/preview/')){
  const name=path.basename(url.pathname);const html=fs.readFileSync(path.join(out,name),'utf8');
  res.setHeader('Content-Type','text/html; charset=utf-8');res.end(`<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">${url.searchParams.has("no-reset") ? "" : '<link rel="stylesheet" href="/bootstrap.css">'}<link rel="stylesheet" href="/TORSEPAN.Panel/wwwroot/app.css"><link rel="stylesheet" href="/TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css"></head><body><div class="layout"><div class="main"><header class="topbar" style="color:white;display:flex;align-items:center;justify-content:center">سامانه مدیریت تولید · TORSEPAN</header><main class="content">${html}</main></div></div></body></html>`);return;
 }else if(url.pathname.startsWith('/images/'))file=path.join(root,'TORSEPAN.Panel/wwwroot',url.pathname);
 else file=path.join(root,url.pathname);
 try{const ext=path.extname(file);res.setHeader('Content-Type',({'.css':'text/css','.webp':'image/webp','.woff2':'font/woff2'})[ext]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404);res.end();}
});
(async()=>{await new Promise(resolve=>server.listen(5188,'127.0.0.1',resolve));const browser=await chromium.launch({headless:true});const page=await browser.newPage();
for(const width of [360,384,412])for(const noReset of [false,true])for(const name of ['overview','management','supplies','create','design']){
 await page.setViewportSize({width,height:782});await page.goto(`http://127.0.0.1:5188/preview/${name}.html${noReset?"?no-reset=1":""}`);await page.evaluate(()=>document.fonts.ready);
 assert.equal(await page.locator(name==='design'?'.design-hero img':'.inventory-hero img').evaluate(e=>e.complete&&e.naturalWidth>0),true,'brand logo must load');
 const bounds=await page.evaluate(()=>({width:document.documentElement.scrollWidth,height:document.documentElement.scrollHeight,viewport:innerWidth}));
 assert(bounds.width<=bounds.viewport,`${name} at ${width}px must not scroll horizontally: ${JSON.stringify(bounds)}`);
 if(!noReset) assert(bounds.height <= (name === 'overview' ? 782 : 782*1.5),`${name} at ${width}px must fit within one and a half screens: ${JSON.stringify(bounds)}`);
 const hero=await page.locator(name==='design'?'.design-hero':'.inventory-hero').boundingBox();
 const workspace=await page.locator(name==='design'?'.design-layout':'.inventory-workspace').boundingBox();
 assert(Math.abs(hero.x-workspace.x)<=1&&Math.abs(hero.width-workspace.width)<=1,`hero must align with page cards ${name} ${width} reset=${!noReset}: ${JSON.stringify({hero,workspace})}`);
 assert(hero.x>=0&&hero.x+hero.width<=width,`hero must be fully visible ${name}`);
 await page.screenshot({path:path.join(out,`${name}-${width}${noReset?'-no-reset':''}.png`),fullPage:true});console.log('PASS layout',name,width,{...bounds,noReset,hero:hero.width,workspace:workspace.width});
}await browser.close();server.close();})().catch(e=>{console.error(e);server.close();process.exitCode=1});
