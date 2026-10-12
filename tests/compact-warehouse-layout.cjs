const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.WAREHOUSE_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),preview=process.env.WAREHOUSE_PREVIEW_DIR;
if(!preview)throw Error('WAREHOUSE_PREVIEW_DIR must contain WarehouseTierSmoke rendered fixtures');
const css=path.join(root,'TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css');
const server=http.createServer((req,res)=>{
 const pathname=new URL(req.url,'http://localhost').pathname;
 if(pathname.endsWith('.html')){
  res.setHeader('Content-Type','text/html; charset=utf-8');
  const isHeader=pathname==='/header.html';
  const markup=fs.readFileSync(path.join(preview,path.basename(pathname)),'utf8');
  const scope=fs.readFileSync(css,'utf8').match(/\.mobile-menu-toggle\[(b-[a-z0-9]+)\]/)?.[1]||'';
  const pageMarkup=isHeader?`<div class="layout"><div class="main"><header class="topbar" ${scope}>${markup}<button class="mobile-menu-toggle" ${scope}>☰</button></header><main class="content"></main></div></div>`:`<div class="test-page">${markup}</div>`;
  res.end(`<!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="/bootstrap.css"><link rel="stylesheet" href="/app.css"><link rel="stylesheet" href="/styles.css"><style>body{margin:0}.test-page{padding:10px;width:calc(100% - 24px);margin-left:auto}body{font-family:Vazirmatn,sans-serif}</style>${pageMarkup}</html>`);return;
 }
 const file=pathname==='/bootstrap.css'?path.join(preview,'bootstrap.css'):pathname==='/styles.css'?css:path.join(root,'TORSEPAN.Panel/wwwroot',pathname);
 if(fs.existsSync(file)){res.end(fs.readFileSync(file));return;}res.statusCode=404;res.end();
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const base=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch({headless:true,executablePath:process.env.WAREHOUSE_CHROMIUM,args:['--no-sandbox']});
 try{
  const page=await browser.newPage();
  for(const width of [320,360,384,412])for(const fixture of ['warehouse-economy','warehouse-details','qc-warehouse','qc-export']){
   await page.setViewportSize({width,height:780});await page.goto(`${base}/${fixture}.html`);await page.evaluate(()=>document.fonts.ready);
   assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`${fixture} at ${width}px must not scroll horizontally`);
   if(fixture==='warehouse-economy'){
    await page.evaluate(()=>{const list=document.querySelector('.stock-list'),row=list.querySelector('.stock-item');while(list.querySelectorAll('.stock-item').length<8)list.append(row.cloneNode(true));});
    const rows=await page.locator('.stock-item').evaluateAll(es=>es.map(e=>({y:e.offsetTop,h:e.getBoundingClientRect().height})));
    assert(rows.reduce((sum,r)=>sum+r.h,0)<=440,`eight collapsed rows must fit within 440px at ${width}px`);
    const controls=await page.locator('.stock-item').first().evaluate(e=>{const code=e.querySelector('.stock-code').getBoundingClientRect();return [...e.querySelectorAll('.stock-checkbox,.stock-expand')].every(x=>x.getBoundingClientRect().left>=code.right);});
    assert(controls,'selection and expansion must be to the right of the code');
   }
   if(fixture==='warehouse-details'){
    const operations=page.locator('.stock-operation');assert.equal(await operations.count(),3);
    assert(await operations.evaluateAll(es=>es.every(e=>e.scrollWidth<=e.clientWidth&&[...e.children].every(c=>c.scrollWidth<=c.clientWidth))),'performer names, stage labels and dates must stay within each history row');
   }
   if(fixture.startsWith('qc-')){
    const choices=await page.locator('.qc-warehouse-option').evaluateAll(es=>es.map(e=>{const box=e.getBoundingClientRect(),input=e.querySelector('input').getBoundingClientRect(),text=e.querySelector('strong').getBoundingClientRect();return {top:box.top,height:box.height,separate:input.bottom<=text.top};}));
    assert.equal(choices.length,4);assert(choices.every(x=>x.top===choices[0].top&&x.height<=75&&x.separate),'four QC choices must share one compact row without radio/text overlap');
   }
   console.log(`PASS ${fixture} ${width}px`);
   if(width===360)await page.screenshot({path:path.join(preview,`${fixture}.png`),fullPage:true});
  }
  for(const width of [320,360,384,412,768,1024])for(const legacy of [true,false]){
   await page.setViewportSize({width,height:780});await page.goto(`${base}/header.html`);await page.evaluate(()=>document.fonts.ready);
   if(legacy)await page.evaluate(()=>{
    const nav=document.createElement('nav');nav.className='app-mode-controls';nav.innerHTML='<span>● آنلاین</span><a href="torsepan-app://offline">ثبت آفلاین / صف عملیات</a>';
    document.querySelector('.topbar-title-block').append(nav);
    const style=document.createElement('style');style.textContent='.app-mode-controls{display:flex;align-items:center;gap:7px;padding-top:6px;margin-top:6px;border-top:1px solid #ffffff26;font-size:10px;line-height:1.6;direction:rtl}';document.head.append(style);
   });
   else await page.addScriptTag({path:path.join(root,'android/app/src/main/assets/online-mode-controls.js')});
   const box=await page.evaluate(()=>{
    const title=document.querySelector('.page-title').getBoundingClientRect(),mode=document.querySelector('.app-mode-controls').getBoundingClientRect(),header=document.querySelector('.topbar').getBoundingClientRect();
    return {title:{x:title.x,y:title.y,right:title.right,bottom:title.bottom},mode:{x:mode.x,y:mode.y,right:mode.right,bottom:mode.bottom},header:{x:header.x,y:header.y,right:header.right,bottom:header.bottom,height:header.height},width:document.documentElement.scrollWidth,viewport:innerWidth};
   });
   assert(box.mode.right<=box.title.x+1,'Android controls must be to the left of the title');
   assert(box.mode.y<box.title.bottom&&box.mode.bottom>box.title.y,'Android controls must align beside the title, never below it');
   assert(box.mode.x>=box.header.x&&box.mode.right<=box.header.right&&box.mode.bottom<=box.header.bottom,'controls must remain within the header');
   assert(box.width<=box.viewport,'header must not scroll horizontally');
   if(width<=768)assert.equal(box.header.height,106,'mode controls must not enlarge the existing mobile header');
   assert.equal(await page.locator('.app-mode-controls a').getAttribute('href'),'torsepan-app://offline');
   if(width===360)await page.screenshot({path:path.join(preview,`header-${legacy?'current-apk':'new-apk'}.png`),fullPage:true});
   if(!legacy){
    await page.evaluate(()=>{const host=document.querySelector('.topbar-left'),copy=host.cloneNode(true);copy.querySelector('.app-mode-controls').remove();host.replaceWith(copy);});
    await page.waitForSelector('.topbar-left>.app-mode-controls');
    assert.equal(await page.locator('.app-mode-controls').count(),1,'Blazor rerender restores exactly one control group');
   }
   console.log(`PASS header ${width}px ${legacy?'current APK':'new APK'}`);
  }
  await page.close();
 }finally{await browser.close();server.close();}
})().catch(error=>{console.error(error);server.close();process.exitCode=1;});
