const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.WAREHOUSE_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),preview=process.env.WAREHOUSE_PREVIEW_DIR;
if(!preview)throw Error('WAREHOUSE_PREVIEW_DIR must contain WarehouseTierSmoke rendered fixtures');
const css=path.join(root,'TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css');
const server=http.createServer((req,res)=>{
 const pathname=new URL(req.url,'http://localhost').pathname;
 if(pathname.endsWith('.html')){
  res.setHeader('Content-Type','text/html; charset=utf-8');
  res.end(`<!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="/bootstrap.css"><link rel="stylesheet" href="/app.css"><link rel="stylesheet" href="/styles.css"><style>body{margin:0}.test-page{padding:10px;width:calc(100% - 24px);margin-left:auto}body{font-family:Vazirmatn,sans-serif}</style><div class="test-page">${fs.readFileSync(path.join(preview,path.basename(pathname)),'utf8')}</div></html>`);return;
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
  await page.setContent('<div class="topbar-title-block"><div>سامانه مدیریت تولید</div></div>');
  await page.addScriptTag({path:path.join(root,'android/app/src/main/assets/online-mode-controls.js')});
  assert.equal(await page.locator('.topbar-title-block .app-mode-controls').count(),1);
  assert.equal(await page.locator('.app-mode-controls a').getAttribute('href'),'torsepan-app://offline');
  await page.evaluate(()=>document.querySelector('.topbar-title-block').replaceChildren(document.createElement('div')));
  await page.waitForSelector('.topbar-title-block .app-mode-controls');
  assert.equal(await page.locator('.app-mode-controls').count(),1,'Blazor rerender must restore mode controls without duplicates');
  console.log('PASS Android mode controls remain inside the title and survive rerender');
  await page.close();
 }finally{await browser.close();server.close();}
})().catch(error=>{console.error(error);server.close();process.exitCode=1;});
