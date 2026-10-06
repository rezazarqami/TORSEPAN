const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const assert = require('node:assert/strict');
const {chromium} = require(process.env.ORDER_PLAYWRIGHT || 'playwright');
const root = process.env.TORSEPAN_ORDER_PREVIEW_DIR;
assert(root,'TORSEPAN_ORDER_PREVIEW_DIR is required');
const server = http.createServer((req,res)=>{
 const requestPath=decodeURIComponent(req.url.split('?')[0]);
 const file=path.join(root,requestPath);
 if(!file.startsWith(path.resolve(root)+path.sep)||!fs.existsSync(file)){res.writeHead(404);return res.end();}
 res.setHeader('Content-Type',file.endsWith('.html')?'text/html; charset=utf-8':file.endsWith('.webp')?'image/webp':'font/woff2');res.end(fs.readFileSync(file));
});
let browser;
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));
 const port=server.address().port;
 browser=await chromium.launch({headless:true,...(process.env.ORDER_CHROMIUM?{executablePath:process.env.ORDER_CHROMIUM,args:JSON.parse(process.env.ORDER_CHROMIUM_ARGS||'[]')}:{})});
 const page=await browser.newPage();
 for(const width of [360,390,768,1280]){
  await page.setViewportSize({width,height:900});
  for(const file of ['new-order.html','composition.html','drafts.html','order-list.html','assign-code.html']){
   await page.goto(`http://127.0.0.1:${port}/${file}`);await page.evaluate(()=>document.fonts.ready);
   assert(await page.locator('.orders-hero').count(),`Actual page not rendered: ${file}`);
   assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,`page overflow ${width} ${file}`);
   const escaped=await page.locator('.composition-line,.order-summary,.instrument-slot,.order-tabs button,.code-dialog').evaluateAll(nodes=>nodes.filter(n=>{const r=n.getBoundingClientRect();return r.width>0&&(r.left < -1 || r.right>innerWidth+1 || n.scrollWidth>n.clientWidth+2);}).map(n=>n.className));
   assert.deepEqual(escaped,[],`control overflow ${width} ${file}: ${escaped}`);
   if(file==='composition.html'){assert.equal(await page.locator('.composition-line').count(),3);assert.equal((await page.locator('.summary-total strong').innerText()).trim(),'15');}
   if(file==='order-list.html'){assert(await page.locator('.specification-row').count()>=3);assert(await page.locator('.instrument-slot').count()>12);}
   if(file==='assign-code.html'){assert.equal(await page.locator('[role=dialog]').count(),1);assert.match(await page.locator('#order-code-title').innerText(),/1.*5/);}
   if([390,1280].includes(width)&&['composition.html','order-list.html','assign-code.html'].includes(file))await page.screenshot({path:path.join(root,`${file.replace('.html','')}-${width}.png`),fullPage:true});
   console.log(`PASS ${file} at ${width}px; no page or control overflow`);
  }
 }
 await page.close();
})().catch(e=>{console.error(e);process.exitCode=1;}).finally(async()=>{if(browser)await browser.close();server.close();});
