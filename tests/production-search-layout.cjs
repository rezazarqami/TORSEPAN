const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.PROFIT_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),read=p=>fs.readFileSync(path.join(root,p),'utf8');
(async()=>{
 const browser=await chromium.launch({headless:true});
 try{
 const page=await browser.newPage();
 const razor=read('TORSEPAN.Panel/Components/Pages/Dimpling.razor');
 const block=razor.slice(razor.indexOf('<div class="input-group passport-search'),razor.indexOf('@if (!string.IsNullOrWhiteSpace(_successMessage))'));
 assert(block.indexOf('<button')<block.indexOf('<input'),'button precedes input in RTL grid');
 const css=['wwwroot/vendor/bootstrap/bootstrap.rtl.min.css','wwwroot/navigation-shell.css','Components/Pages/Dimpling.razor.css'].map(p=>read('TORSEPAN.Panel/'+p)).join('\n');
 for(const width of [320,384,412]){
 await page.setViewportSize({width,height:800});
 await page.setContent(`<html dir="rtl"><head><style>${css}</style></head><body><div class="layout theme-shell"><div class="main"><main class="content"><div style="height:650px"></div><div class="production-detail-surface"><div class="production-passport-page luxury-passport-page"><div class="input-group passport-search"><button>جست‌وجو</button><input class="form-control" id="passport-code" dir="ltr" value="S20156"></div></div></div><div style="height:600px"></div></main></div><nav class="bottom-navigation">منو</nav></div></body></html>`);
 await page.addScriptTag({content:read('TORSEPAN.Panel/wwwroot/mobile-input.js')});
 const input=page.locator('#passport-code'),button=page.locator('.passport-search button');
 const ib=await input.boundingBox(),bb=await button.boundingBox();assert(bb.x>=ib.x+ib.width-1,'search button is on the right');
 assert.equal(await input.evaluate(e=>getComputedStyle(e).textAlign),'left');
 await input.focus();await page.setViewportSize({width,height:400});
 await page.waitForFunction(()=>getComputedStyle(document.querySelector('.bottom-navigation')).display==='none');
 await page.waitForFunction(()=>{const b=document.querySelector('#passport-code').getBoundingClientRect();return b.top>=0&&b.bottom<=innerHeight});
 await input.evaluate(e=>{e.setSelectionRange(e.value.length,e.value.length)});await page.keyboard.press('Backspace');assert.equal(await input.inputValue(),'S2015');
 await input.blur();await page.waitForFunction(()=>getComputedStyle(document.querySelector('.bottom-navigation')).display!=='none');
 console.log('PASS search and keyboard viewport',width);
 }
 }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
