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
  for(const file of ['new-order.html','simple-order.html','incomplete-order.html','edit-order.html','composition.html','drafts.html','order-list.html','order-list-expanded.html','overview-50.html','assign-code.html']){
   await page.goto(`http://127.0.0.1:${port}/${file}`);await page.evaluate(()=>document.fonts.ready);
   assert(await page.locator('.orders-hero').count(),`Actual page not rendered: ${file}`);
   assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,`page overflow ${width} ${file}`);
   const escaped=await page.locator('.composition-line,.order-summary,.instrument-slot,.instrument-overview,.overview-item,.order-tabs button,.code-dialog').evaluateAll(nodes=>nodes.filter(n=>{const r=n.getBoundingClientRect();return r.width>0&&(r.left < -1 || r.right>innerWidth+1 || n.scrollWidth>n.clientWidth+2);}).map(n=>n.className));
   assert.deepEqual(escaped,[],`control overflow ${width} ${file}: ${escaped}`);
   if(file==='simple-order.html'){
    assert.equal(await page.locator('.composition-line select').first().inputValue(),'');
    assert.equal((await page.locator('.composition-line select').first().locator('option:checked').innerText()).trim(),'دیزاین ساده');
    assert.equal(await page.locator('.composition-line select').first().locator('option').count(),1);
    const scaleSelect=page.locator('.composition-line select').nth(1);
    const scaleNames=await scaleSelect.locator('option').allTextContents();
    assert.deepEqual(scaleNames,['انتخاب اسکیل','Both instrument catalogs','D Kurd Custom 9','E Sabye standard','F Pygmy 12']);
    assert.equal(await scaleSelect.locator('optgroup').count(),0);
    assert.equal((await scaleSelect.locator('option:checked').innerText()).trim(),'E Sabye standard');
    assert.equal(await page.locator('.summary-total strong').innerText(),'1');
    assert.equal(await page.getByRole('button',{name:'ذخیرهٔ پیش‌سفارش',exact:true}).isEnabled(),true);
   }
   if(file==='incomplete-order.html'){
    assert.equal(await page.getByRole('button',{name:'ذخیرهٔ پیش‌سفارش',exact:true}).isEnabled(),false);
    assert.match(await page.locator('.order-summary [role=status]').innerText(),/ردیف 1.*اسیدکاری.*اسکیل ساز را انتخاب کنید/);
   }
   if(file==='edit-order.html'){
    assert.equal(await page.getByRole('button',{name:'ذخیرهٔ تغییرات سفارش',exact:false}).isEnabled(),true);
    assert.equal(await page.getByRole('button',{name:'ذخیرهٔ پیش‌سفارش',exact:true}).count(),0);
   }
   if(file==='composition.html'){assert.equal(await page.locator('.composition-line').count(),3);assert.equal((await page.locator('.summary-total strong').innerText()).trim(),'15');}
   if(file==='order-list-expanded.html'){assert(await page.locator('.specification-row').count()>=3);assert(await page.locator('.instrument-slot').count()>12);}
   if(['order-list.html','order-list-expanded.html','overview-50.html'].includes(file)){
    const card=page.locator('.order-card').first();
    assert.equal(await card.locator('.instrument-overview').count(),1);
    if(file==='order-list-expanded.html'){
    assert(await card.locator('.specification-scale').first().evaluate(n=>Number(getComputedStyle(n).fontWeight)>=700),'scale must be bold');
    assert(await card.evaluate(n=>n.querySelector('.instrument-overview').compareDocumentPosition(n.querySelector('.specification-list'))&Node.DOCUMENT_POSITION_FOLLOWING),'overview must precede code-entry sections');
    assert.equal(await card.locator('.code-section-toggle').getAttribute('aria-expanded'),'true');
    }else{
     assert.equal(await page.locator('.instrument-slot').count(),0);
     assert.equal(await card.locator('.code-section-toggle').getAttribute('aria-expanded'),'false');
     assert(await card.locator('.order-card-head').isVisible());
    }
    if(file==='overview-50.html'){
     const items=card.locator('.overview-item');
     assert.equal(await items.count(),50);
     assert.equal(await items.first().innerText(),'TP-0001\nدر انتظار دیمپل');
     assert.match(await items.nth(3).innerText(),/فاین/);
     assert.match(await items.nth(4).innerText(),/اتاق چسب/);
     const metrics=await card.locator('.overview-grid').evaluate(n=>({columns:getComputedStyle(n).gridTemplateColumns.split(' ').length,height:n.clientHeight,limit:innerHeight*.55,scroll:n.scrollHeight}));
     assert.equal(metrics.columns,2);
     assert(metrics.height<=metrics.limit+1&&metrics.scroll>metrics.height,'50-code list must scroll inside a bounded panel');
    }
   }
   if(file==='assign-code.html'){assert.equal(await page.locator('[role=dialog]').count(),1);assert.match(await page.locator('#order-code-title').innerText(),/1.*5/);}
   if([390,1280].includes(width)&&['composition.html','order-list.html','order-list-expanded.html','overview-50.html','assign-code.html'].includes(file))await page.screenshot({path:path.join(root,`${file.replace('.html','')}-${width}.png`),fullPage:true});
   console.log(`PASS ${file} at ${width}px; no page or control overflow`);
  }
 }
 await page.close();
})().catch(e=>{console.error(e);process.exitCode=1;}).finally(async()=>{if(browser)await browser.close();server.close();});
