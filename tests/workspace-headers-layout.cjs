const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.WORKSPACE_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),web=path.join(root,'TORSEPAN.Panel/wwwroot');
const out=process.env.WORKSPACE_HEADER_PREVIEW_DIR;
assert(out,'Set WORKSPACE_HEADER_PREVIEW_DIR to the Razor-rendered header directory');
const server=http.createServer((req,res)=>{
 const url=new URL(req.url,'http://localhost');
 if(/^\/(account|messages)$/.test(url.pathname)){
  const kind=url.pathname.slice(1),html=fs.readFileSync(path.join(out,kind+'.html'),'utf8');
  const css=['/vendor/bootstrap/bootstrap.rtl.min.css','/app.css','/scoped.css','/theme.css','/theme-pages.css','/navigation-shell.css'].map(h=>`<link rel="stylesheet" href="${h}">`).join('');
  res.setHeader('Content-Type','text/html; charset=utf-8');
  res.end(`<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">${css}</head><body><div class="layout theme-shell"><div class="main"><main class="content"><div class="navy-management-page navy-${kind}-page">${html}</div></main></div></div></body></html>`);return;
 }
 const file=url.pathname==='/scoped.css'?path.join(root,'TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css'):path.join(web,url.pathname);
 try{res.setHeader('Content-Type',({'.css':'text/css','.webp':'image/webp','.png':'image/png','.woff2':'font/woff2'})[path.extname(file)]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404);res.end();}
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const opts={headless:true};if(process.env.WORKSPACE_CHROMIUM)opts.executablePath=process.env.WORKSPACE_CHROMIUM;
 if(process.env.WORKSPACE_CHROMIUM_ARGS)opts.args=JSON.parse(process.env.WORKSPACE_CHROMIUM_ARGS);
 let browser;
 try{
  browser=await chromium.launch(opts);const page=await browser.newPage();
  for(const width of [320,360,384,412,768,1280])for(const kind of ['account','messages']){
   const failures=[];page.on('response',r=>{if(r.status()>=400)failures.push(r.url())});
   await page.setViewportSize({width,height:900});await page.goto(`http://127.0.0.1:${server.address().port}/${kind}`);await page.evaluate(()=>document.fonts.ready);
   await page.locator('.workspace-hero-mark').evaluate(e=>e.decode());
   const hero=page.locator('.workspace-photo-hero');
   const state=await hero.evaluate(async el=>{
    const box=el.getBoundingClientRect(),title=el.querySelector('h1'),desc=el.querySelector('p'),pill=el.querySelector('.workspace-hero-eyebrow');
    const bg=getComputedStyle(el).backgroundImage.match(/url\("?(.*?)"?\)/)[1];const image=new Image();image.src=bg;await image.decode();
    return {pageWidth:document.documentElement.scrollWidth,viewport:innerWidth,titleWidth:title.scrollWidth,titleBox:title.clientWidth,pillWidth:pill.scrollWidth,pillBox:pill.clientWidth,background:bg,backgroundWidth:image.naturalWidth,titleBottom:title.getBoundingClientRect().bottom,descriptionTop:desc.getBoundingClientRect().top,ratio:box.width/box.height,height:box.height,copyRight:el.querySelector('.workspace-hero-copy').getBoundingClientRect().right,heroRight:box.right};
   });
   assert.equal(failures.length,0,failures.join('\n'));assert(state.pageWidth<=width,JSON.stringify(state));
   assert(state.titleWidth<=state.titleBox+1,`Title clipped: ${kind} ${width}`);assert(state.pillWidth<=state.pillBox+1,`Eyebrow clipped: ${kind} ${width}`);
   assert(state.titleBottom<=state.descriptionTop,`Text overlaps: ${kind} ${width}`);assert(state.copyRight<=state.heroRight,`Copy overflows: ${kind} ${width}`);
   assert(state.backgroundWidth>0&&state.background.includes(kind+'-office.webp'));
   if(width===412)assert(Math.abs(state.ratio-1.6)<.04,`Reference proportions: ${state.ratio}`);
   await hero.screenshot({path:path.join(out,`${kind}-${width}.png`)});
   console.log('PASS actual Razor header',kind,width,Math.round(state.height)+'px');
  }
 }finally{if(browser)await browser.close();await new Promise(resolve=>server.close(resolve))}
})().catch(e=>{console.error(e);process.exitCode=1});
