const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PAYROLL_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),out=process.env.PAYROLL_PREVIEW_DIR,config=process.env.PAYROLL_BUILD_CONFIG||'Release';
if(!out)throw Error('PAYROLL_PREVIEW_DIR is required');
const server=http.createServer((req,res)=>{const url=new URL(req.url,'http://localhost');let file;
 if(url.pathname==='/preview'){
  res.setHeader('Content-Type','text/html;charset=utf-8');res.end(`<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="/vendor/bootstrap/bootstrap.rtl.min.css"><link rel="stylesheet" href="/app.css"><link rel="stylesheet" href="/scoped.css"><link rel="stylesheet" href="/theme.css"><link rel="stylesheet" href="/theme-pages.css"><link rel="stylesheet" href="/navigation-shell.css"></head><body><div class="layout theme-shell"><div class="main"><main class="content">${fs.readFileSync(path.join(out,'payroll.html'),'utf8')}</main></div></div></body></html>`);return;
 }
 file=url.pathname==='/scoped.css'?path.join(root,`TORSEPAN.Panel/obj/${config}/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css`):path.join(root,'TORSEPAN.Panel/wwwroot',url.pathname);
 try{res.setHeader('Content-Type',({'.css':'text/css','.woff2':'font/woff2','.webp':'image/webp','.png':'image/png'})[path.extname(file)]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404);res.end();}
});
let browser;const watchdog=setTimeout(()=>{console.error('Payroll layout timed out');process.exit(1)},120000);
(async()=>{
 await new Promise(r=>server.listen(5196,'127.0.0.1',r));browser=await chromium.launch({headless:true,...(process.env.PAYROLL_CHROMIUM?{executablePath:process.env.PAYROLL_CHROMIUM,args:['--no-sandbox','--disable-dev-shm-usage','--disable-gpu']}:{})});
 const page=await browser.newPage();
 for(const width of [320,360,384,412,768,1280]){
  await page.setViewportSize({width,height:900});await page.goto('http://127.0.0.1:5196/preview');await page.evaluate(()=>document.fonts.ready);
  assert.equal(await page.locator('.payroll-compact-row').count(),7,'all normal, Custom and export wage lines remain present');
  assert.equal(await page.locator('.payroll-member-lines').count(),4,'member grouping remains separate in each production path');
  assert.equal((await page.locator('.payroll-member-total strong').first().innerText()).replace(/\D/g,''),'5200000','member total remains exact');
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'no horizontal page scroll');
  const rows=await page.locator('.payroll-compact-row>summary').all();
  for(const row of rows){const box=await row.boundingBox();assert(box.height<=100,'collapsed wage rows remain compact');}
  const first=page.locator('.payroll-compact-row').first();
  await first.locator('summary').focus();await page.keyboard.press('Enter');assert(await first.getAttribute('open')!==null,'keyboard opens real details');
  assert(await first.locator('.payroll-row-details').isVisible(),'rate and technical details are reachable');
  assert((await first.locator('.payroll-row-details').innerText()).includes('1,500,000'),'expanded row retains unit rate');
  const contrast=await page.locator('.payroll-row-details').first().evaluate(el=>{
   const rgb=s=>(s.match(/[\d.]+/g)||[]).slice(0,3).map(Number),lum=c=>c.map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4}).reduce((s,v,i)=>s+v*[.2126,.7152,.0722][i],0),bg=lum(rgb(getComputedStyle(el).backgroundColor));
   return [...el.querySelectorAll('dt,dd')].every(node=>{const fg=lum(rgb(getComputedStyle(node).color));return (Math.max(fg,bg)+.05)/(Math.min(fg,bg)+.05)>=4.5;});
  });assert(contrast,'expanded text has at least 4.5:1 contrast against navy');
  await first.locator('summary').press('Enter');assert.equal(await first.getAttribute('open'),null,'keyboard closes details');
  for(const row of await page.locator('.payroll-compact-row').all())await row.locator('summary').click();
  assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'long scales and large amounts fit when expanded');
  for(const row of await page.locator('.payroll-compact-row').all())await row.locator('summary').click();
  if(width===384||width===1280){await page.locator('.payroll-member-list').first().locator('..').screenshot({path:path.join(out,`payroll-compact-${width}.png`)});await first.locator('summary').click();await page.locator('.payroll-member-list').first().locator('..').screenshot({path:path.join(out,`payroll-expanded-${width}.png`)});}
  console.log('PASS compact payroll, contrast, exact totals, keyboard and overflow',width);
 }
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{clearTimeout(watchdog);if(browser)await browser.close();server.close()});
