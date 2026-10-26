const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PROFIT_PLAYWRIGHT||'playwright');const root=path.resolve(__dirname,'..'),out=process.env.PROFIT_PREVIEW_DIR;if(!out)throw Error('PROFIT_PREVIEW_DIR required');
const server=http.createServer((req,res)=>{const u=new URL(req.url,'http://localhost');let file;
 if(u.pathname.startsWith('/preview/')){const html=fs.readFileSync(path.join(out,path.basename(u.pathname)),'utf8');res.setHeader('Content-Type','text/html;charset=utf-8');res.end(`<!doctype html><html lang="fa" dir="rtl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><link rel="stylesheet" href="/vendor/bootstrap/bootstrap.rtl.min.css"><link rel="stylesheet" href="/app.css"><link rel="stylesheet" href="/scoped.css"><link rel="stylesheet" href="/theme.css"><link rel="stylesheet" href="/theme-pages.css"><link rel="stylesheet" href="/navigation-shell.css"></head><body><div class="layout theme-shell"><div class="main"><main class="content">${html}</main></div></div></body></html>`);return;}
 file=u.pathname==='/scoped.css'?path.join(root,'TORSEPAN.Panel/obj/Release/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css'):path.join(root,'TORSEPAN.Panel/wwwroot',u.pathname);try{res.setHeader('Content-Type',({'.css':'text/css','.webp':'image/webp','.woff2':'font/woff2'})[path.extname(file)]||'application/octet-stream');res.end(fs.readFileSync(file));}catch{res.writeHead(404);res.end();}});
let browser;const timer=setTimeout(()=>{console.error('Profit UI timed out');process.exit(1)},120000);
(async()=>{await new Promise(r=>server.listen(5196,'127.0.0.1',r));browser=await chromium.launch({headless:true,...(process.env.PROFIT_CHROMIUM?{executablePath:process.env.PROFIT_CHROMIUM,args:JSON.parse(process.env.PROFIT_CHROMIUM_ARGS||'[]')}:{})});const page=await browser.newPage();page.setDefaultTimeout(10000);
 for(const [width,height] of [[320,740],[360,740],[384,800],[412,850],[768,900],[1280,900]])for(const state of ['sale','sale-export','members','profile','hall','members-error','rules-error']){await page.setViewportSize({width,height});await page.goto(`http://127.0.0.1:5196/preview/${state}.html`);await page.evaluate(()=>document.fonts.ready);assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`${state} ${width} has no sideways scroll`);
 if(state.startsWith('sale')){const box=await page.locator('.sale-dialog').boundingBox();assert(box.x>=0&&box.y>=0&&box.x+box.width<=width&&box.y+box.height<=height,'entire dialog stays within viewport');
 assert(await page.locator('.sale-dialog').evaluate(e=>e.scrollHeight<=e.clientHeight+1),'default sale form fits one frame without internal scrolling');
 assert(await page.locator('#sale-profit').count()===1&&await page.locator('.sale-money-input select').count()===2,'independent currency controls exist');
 assert(await page.locator('.sale-dialog>header').evaluate(e=>getComputedStyle(e).backgroundColor!=='rgb(255, 255, 255)'),'header uses navy rather than the unreadable white surface');
 const footer=await page.locator('.sale-dialog>footer').boundingBox();assert(footer.y+footer.height<=height,'submit footer is visible without scrolling');
 }else{
 if(state==='hall'){const payment=page.getByLabel('مبلغ پرداخت خدمات سالن');await payment.fill('100');assert(await payment.evaluate(e=>e.checkValidity()),'integer toman hall payments satisfy browser step validation');}
 if(state!=='members-error')assert(await page.locator('.profit-member').count()===2,'member profile is selectable');
 if(['members','profile','hall','members-error'].includes(state)){
  assert(await page.locator('.profit-rules').getAttribute('open')!==null,'percentage editor is expanded without requiring a hidden toggle');
  assert(await page.locator('.profit-rule-grid input').count()===7,'all stage percentage inputs render');
  assert(await page.locator('#profit-rule-3').isVisible(),'shape percentage is visible');
 }
 if(state==='rules-error')assert(await page.getByText(/torsepan-api/).count()===1,'API version failure is actionable');
 }
 await page.screenshot({path:path.join(out,`${state}-${width}.png`),fullPage:true});console.log('PASS profit sharing layout',state,width);
 }
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{clearTimeout(timer);if(browser)await browser.close();server.close()});
